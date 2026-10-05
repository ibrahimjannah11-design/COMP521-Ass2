using System.Collections.Generic;
using UnityEngine;

// NOTE: if you see a comment beginning or containing !!
// Thats what i would write when i wanted to go back and check on something
// I should have gotten rid of them all, but if you see one lingering
// Just know its me talking to myself lol

public class GameManager : MonoBehaviour
{
    
    public MothSettings moth_settings;
    

   
    public float gravity = -15f;
    public float cannonball_radius = 0.2f;
    public float fire_interval = 1.2f;
    public float restitution = 0.9f;

    private Terrain terrain;
    private LineDrawer draw;

    private List<Moth> moths = new List<Moth>();
    private List<Cannon> cannons = new List<Cannon>();
    private List<Cannonball> cannonballs = new List<Cannonball>();

    // void Start()
    // {
    //     terrain = new Terrain();
    //     terrain.Build();

    //     BuildCannons();
    // }
    // !! remebert to comment out when u switch to sprites

    void Start()
    {
        terrain = new Terrain();
        terrain.Build();

        BuildCannons();

        Camera cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = terrain.world_half_height;
        cam.transform.position = new Vector3(0f, 0f, -10f);

        draw = gameObject.AddComponent<LineDrawer>();
    }

    void Update()
    {
        float dt = Time.deltaTime;

        SpawnMoths();

        UpdateCannons(dt);

        UpdateCannonballs(dt);

        UpdateMoths(dt);

        Cleanup();

        // !! remove later
        Render();
    }

    // !! remove later
    private void Render()
    {
        draw.Begin();

        // scene bounds 
        float W = terrain.world_half_width, H = terrain.world_half_height;
        Color bounds = new Color(1f, 1f, 1f, 0.2f);
        draw.Line(new Vector2(-W, -H), new Vector2(W, -H), bounds);
        draw.Line(new Vector2(W, -H), new Vector2(W, H), bounds);
        draw.Line(new Vector2(W, H), new Vector2(-W, H), bounds);
        draw.Line(new Vector2(-W, H), new Vector2(-W, -H), bounds);

        // terrain
        foreach (Surface s in terrain.Surfaces)
            draw.Line(s.A, s.B, s.type == SurfaceType.Ground ? Color.green : Color.gray, 0.12f);

        // light
        draw.Circle(terrain.light_source, terrain.light_radius, Color.yellow, 32, 0.15f);

        // cannons
        foreach (Cannon c in cannons)
        {
            Vector2 dir = new Vector2(Mathf.Cos(c.angle * Mathf.Deg2Rad), Mathf.Sin(c.angle * Mathf.Deg2Rad));
            draw.Circle(c.position, 0.3f, Color.white);
            draw.Line(c.position, c.position + dir * 0.8f, Color.white, 0.12f);
        }

        // cannonballs
        foreach (Cannonball b in cannonballs)
            draw.Circle(b.Position, b.Radius, Color.red, 16);

        // moths
        foreach (Moth m in moths)
        {
            foreach (var link in Moth.Links)
                draw.Line(m.points[link.a].position, m.points[link.b].position, Color.cyan, 0.05f);

            foreach (VerletPoint p in m.points)
                draw.Circle(p.position, p.radius, Color.magenta, 8, 0.04f);

            VerletPoint body = m.points[Moth.Body];
            draw.Circle(body.position, body.radius, Color.white, 12, 0.06f);
        }

        draw.End();
    }

    private void BuildCannons()
    {
        float leftX = -terrain.valley_half_width;
        float rightX = terrain.valley_half_width;

        float spacing = terrain.cliff_height / 4f;

        for (int i = 0; i < 3; i++)
        {
            float y = terrain.ground_y + spacing * (i + 1);

            Cannon left =
                new Cannon(
                    new Vector2(leftX, y),
                    35f,
                    -1);

            left.timer = Random.Range(0f, fire_interval);

            cannons.Add(left);

            Cannon right =
                new Cannon(
                    new Vector2(rightX, y),
                    145f,
                    1);

            right.timer = Random.Range(0f, fire_interval);

            cannons.Add(right);
        }
    }

    private void SpawnMoths()
    {
        if (!Input.GetKeyDown(KeyCode.Space))
            return;

        float x =
            Random.Range(
                -terrain.valley_half_width + 2f,
                 terrain.valley_half_width - 2f);

        Vector2 spawn =
            new Vector2(
                x,
                terrain.ground_y + 0.5f);

        moths.Add(
            new Moth(
                moth_settings,
                spawn));
    }

    private void UpdateCannons(float dt)
    {
        foreach (Cannon cannon in cannons)
        {
            cannon.timer -= dt;

            if (cannon.timer > 0f)
                continue;

            cannon.timer = fire_interval;

            FireCannon(cannon);
        }
    }

    private void FireCannon(Cannon cannon)
    {
        float angle =
            cannon.angle +
            Random.Range(-10f, 10f);

        float speed =
            Random.Range(12f, 17f);

        Vector2 dir =
            new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad));

        cannonballs.Add(
            new Cannonball(
                cannon.position + dir * 0.3f,
                cannonball_radius,
                dir * speed,
                cannon));
    }

    private void UpdateCannonballs(float dt)
    {
        Vector2 gravity_accel =
            new Vector2(0f, gravity);

        foreach (Cannonball ball in cannonballs)
        {
            if (!ball.Active)
                continue;

            ball.Update(dt, gravity_accel);

            ResolveWallCollision(ball);

            if (ball.Position.y <= terrain.ground_y)
            {
                ball.Active = false;
                continue;
            }

            if (terrain.TouchesLight(
                ball.Position,
                ball.Radius))
            {
                ball.Active = false;
                continue;
            }

            if (terrain.OutOfBounds(
                ball.Position))
            {
                ball.Active = false;
            }
        }

        ResolveBallBallCollisions();
    }

    private void ResolveWallCollision(Cannonball ball)
    {
        foreach (Surface s in terrain.Surfaces)
        {
            if (s.type != SurfaceType.Cliff)
                continue;

            if (!MyPhysics.CircleSegment(
                    ball.Position,
                    ball.Radius,
                    s.A,
                    s.B,
                    s.normal,
                    out Vector2 n,
                    out float penetration))
                continue;

            // !! if this doesnt work do (hoepfully it does tho):
            // ball.Position += n * (penetration + 0.001f);
            ball.Position += n * penetration;

            ball.Velocity =
                Vector2.Reflect(
                    ball.Velocity,
                    n) * restitution;
        }
    }

    private void ResolveBallBallCollisions()
    {
        for (int i = 0; i < cannonballs.Count; i++)
        {
            Cannonball a = cannonballs[i];

            if (!a.Active)
                continue;

            for (int j = i + 1; j < cannonballs.Count; j++)
            {
                Cannonball b = cannonballs[j];

                if (!b.Active)
                    continue;

                if (!MyPhysics.CircleCircle(
                        a.Position,
                        a.Radius,
                        b.Position,
                        b.Radius,
                        out Vector2 n,
                        out float penetration))
                    continue;

                a.Position -= n * penetration * 0.5f;
                b.Position += n * penetration * 0.5f;

                Vector2 relative =
                    b.Velocity - a.Velocity;

                float separating =
                    Vector2.Dot(relative, n);

                if (separating > 0f)
                    continue;

                float impulse =
                    -(1f + restitution)
                    * separating
                    * 0.5f;

                Vector2 impulseVector =
                    impulse * n;

                a.Velocity -= impulseVector;
                b.Velocity += impulseVector;
            }
        }
    }

    private void UpdateMoths(float dt)
    {
        foreach (Moth moth in moths)
        {
            if (!moth.Alive)
                continue;

            moth.Update(
                dt,
                terrain,
                cannonballs);
        }
    }

    private void Cleanup()
    {
        moths.RemoveAll(
            m => !m.Alive);

        cannonballs.RemoveAll(
            b => !b.Active);
    }
}