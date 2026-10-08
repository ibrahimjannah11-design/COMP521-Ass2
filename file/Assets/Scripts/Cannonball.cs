using UnityEngine;
using System.Collections.Generic;

public class Cannonball
{
    public Vector2 Position;
    public Vector2 Velocity;

    public float Radius;

    public bool Active = true;

    public Cannon cannon;

    public Cannonball( Vector2 pos, float radius, Vector2 vel, Cannon cannon)
    {
        Position = pos;
        Radius = radius;
        Velocity = vel;
        this.cannon = cannon;
    }

    public void Update( float dt, Vector2 gravity)
    {
        Velocity += gravity * dt;
        Position += Velocity * dt;
    }
}