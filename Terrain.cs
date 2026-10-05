using System.Collections.Generic;
using UnityEngine;

public enum SurfaceType
{
    Cliff,
    Ground,
}

public class Surface
{
    public SurfaceType type;
    public Vector2 A;
    public Vector2 B;
    public Vector2 normal;

    public Surface(SurfaceType type, Vector2 a, Vector2 b, Vector2 normal)
    {
        this.type = type;
        this.A = a;
        this.B = b;
        this.normal = normal;
    }
}

public class Terrain
{
    
    public float valley_half_width = 7f;
    public float ground_y = -5f;
    public float cliff_height = 5f;
    public float world_half_width = 10f;
    public float world_half_height = 10f;
    public Vector2 light_source = new Vector2(0, 10f);
    public float light_radius = 1.2f;

    public List<Surface> Surfaces
    {
        get;
        private set;
    }

    public void Build()
    {
        float w = valley_half_width;
        float W = world_half_width;
        Surfaces = new List<Surface>
        {
            // might have to switch w and W here if doesnt work propelru i n unity
            new Surface(SurfaceType.Cliff, new Vector2(-W, ground_y), new Vector2(-w, ground_y), Vector2.up),
            new Surface(SurfaceType.Cliff, new Vector2(w, ground_y), new Vector2(W, ground_y), Vector2.up),
            new Surface(SurfaceType.Ground, new Vector2(-w, ground_y), new Vector2(w, ground_y), Vector2.up),
            new Surface(SurfaceType.Cliff, new Vector2(-W, ground_y), new Vector2(-W, ground_y + cliff_height), Vector2.right),
            new Surface(SurfaceType.Cliff, new Vector2(W, ground_y), new Vector2(W, ground_y + cliff_height), Vector2.left)
        };
    }

    public bool OutOfBounds(Vector2 position)
    {
        return position.x < -world_half_width || position.x > world_half_width || position.y < -world_half_height || position.y > world_half_height;
    }

    public bool TouchesLight(Vector2 position, float rad)
    {
        float r = light_radius + rad;
        return (position - light_source).sqrMagnitude <= r * r;
    }
}

