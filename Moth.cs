using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MothSettings
{
    public float scale = 1f;
    public float unfold_time = 1f;
    public float spawn_time = 1f;          // cannonball immunity after spawning
    public float thrust = 10f;
    public float damping = 0.985f;
    public int constraint_iterations = 5;
    public float point_radius = 0.1f;
    public float body_radius = 0.15f;      // 0.5 would make the body very easy to hit
    public Vector2 wander_interval = new Vector2(0.3f, 1f);
    public float wander_max_turn = 35f;    // degrees changed per wander step
    public float drift = 0.15f;            // chance of a big sideways swerve
    public float max_heading_change = 60f; // max degrees away from straight up
    public float wall_margin = 2.5f;
    public float wall_avoidance_strength = 15f;
    public float center_bias_strength = 0.5f;
}

public class Moth
{
    public const int Body = 0;
    public const int antenna_left = 1;
    public const int antenna_right = 2;
    public const int wing_top_left = 3;
    public const int wing_bottom_left = 4;
    public const int wing_top_right = 5;
    public const int wing_bottom_right = 6;

    public static Vector2[] Layout = new Vector2[]
    {
        new Vector2(0, 0),        // Body
        new Vector2(-0.5f, 0.5f), // Antenna left
        new Vector2(0.5f, 0.5f),  // Antenna right
        new Vector2(-1f, 0),      // Wing top left
        new Vector2(-1f, -1f),    // Wing bottom left
        new Vector2(1f, 0),       // Wing top right
        new Vector2(1f, -1f)      // Wing bottom right
    };

    public static (int a, int b, float k)[] Links = new (int a, int b, float k)[]
    {
        (Body, antenna_left, 1f),
        (Body, antenna_right, 1f),
        (Body, wing_top_left, 1f),
        (Body, wing_bottom_left, 1f),
        (Body, wing_top_right, 1f),
        (Body, wing_bottom_right, 1f),
        (wing_top_left, wing_bottom_left, 1f),
        (wing_top_right, wing_bottom_right, 1f),
        (antenna_left, antenna_right, 1f),
        (wing_top_left, wing_top_right, 1f),
        (wing_bottom_left, wing_bottom_right, 1f)
        
    };

    public class Antenna
    {
        public float heading;   // degrees away from straight up
        public float timer;
    }

    public List<VerletPoint> points = new List<VerletPoint>();
    public List<DistanceConstraint> constraints = new List<DistanceConstraint>();
    public bool Alive = true;
    public float age;

    readonly MothSettings settings;
    readonly Antenna left_antenna = new Antenna();     // renamed: no clash with the const ints
    readonly Antenna right_antenna = new Antenna();

    public Moth(MothSettings settings, Vector2 position)
    {
        this.settings = settings;

        for (int i = 0; i < Layout.Length; i++)
        {
            Vector2 p = position + Random.insideUnitCircle * 0.1f;
            float r = (i == Body) ? settings.body_radius : settings.point_radius;
            points.Add(new VerletPoint(p, r));
        }

        foreach (var link in Links)
        {
            // !! FIX THIS -> done :DD
            // rest length comes from the LAYOUT!!, not the clustered spawn positions
            float rest_length = (Layout[link.a] - Layout[link.b]).magnitude * settings.scale;
            constraints.Add(new DistanceConstraint(points[link.a], points[link.b], rest_length, link.k));
        }
    }

    public void Update(float dt, Terrain terrain, List<Cannonball> balls)
    {
        age += dt;
        float unfold = Mathf.Clamp01(age / settings.unfold_time);
        float length_scale = Mathf.Lerp(0.1f, 1f, unfold);

        // forces + integration
        Vector2 env_acc = EnvAcc(terrain);
        for (int i = 0; i < points.Count; i++)
        {
            Vector2 accel = env_acc;
            if (i == antenna_left)
                accel += SteerAntenna(left_antenna, dt) * settings.thrust * unfold;
            if (i == antenna_right)
                accel += SteerAntenna(right_antenna, dt) * settings.thrust * unfold;

            points[i].Update(dt, accel, settings.damping);
        }

        // constraints + collisions
        bool spawn_protected = age < settings.spawn_time;
        for (int it = 0; it < settings.constraint_iterations; it++)
        {
            foreach (var c in constraints) c.Resolve(length_scale);
            foreach (var p in points) ResolveTerrain(p, terrain);
            if (!spawn_protected)
                foreach (var p in points) ResolveBalls(p, balls);
            if (!Alive) return;
        }

        // destruction: light or out of bounds
        foreach (var p in points)
        {
            if (terrain.TouchesLight(p.position, p.radius) || terrain.OutOfBounds(p.position))
            {
                Alive = false;
                return;
            }
        }
    }

    private Vector2 SteerAntenna(Antenna a, float dt)
    {
        a.timer -= dt;
        if (a.timer <= 0f)
        {
            a.timer = Random.Range(settings.wander_interval.x, settings.wander_interval.y);
            if (Random.value < settings.drift)
                a.heading = Random.Range(-settings.max_heading_change, settings.max_heading_change);
            else
                a.heading = Mathf.Lerp(a.heading, 0f, 0.25f)
                          + Random.Range(-settings.wander_max_turn, settings.wander_max_turn);
            a.heading = Mathf.Clamp(a.heading, -settings.max_heading_change, settings.max_heading_change);
        }
        // Rotate takes radians
        return MyPhysics.Rotate(Vector2.up, a.heading * Mathf.Deg2Rad);
    }

    private Vector2 EnvAcc(Terrain t)
    {
        Vector2 b = points[Body].position;
        if (b.y > t.cliff_height + 1f)
            return Vector2.zero;

        float dist_to_wall = t.valley_half_width - Mathf.Abs(b.x);
        float ax = -b.x * settings.center_bias_strength;

        if (dist_to_wall < settings.wall_margin)
        {
            ax += -Mathf.Sign(b.x) * settings.wall_avoidance_strength
                  * (1f - Mathf.Max(0f, dist_to_wall) / settings.wall_margin);
        }
        return new Vector2(ax, 0f);
    }

    private void ResolveTerrain(VerletPoint p, Terrain t)
    {
        foreach (Surface s in t.surfaces)
        {
            if (MyPhysics.CircleSegment(p.position, p.radius, s.A, s.B, s.normal,
                                           out Vector2 n, out float pen))
                p.position += n * pen;
        }
    }

    // body overlap kills the moth; every other vertex is pushed out of the ball
    private void ResolveBalls(VerletPoint p, List<Cannonball> balls)
    {
        foreach (Cannonball ball in balls)
        {
            if (MyPhysics.CircleCircle(p.position, p.radius, ball.Position, ball.Radius,
                                          out Vector2 n, out float pen))
            {
                if (p == points[Body]) { Alive = false; return; }
                p.position += n * pen;
            }
        }
    }
}   