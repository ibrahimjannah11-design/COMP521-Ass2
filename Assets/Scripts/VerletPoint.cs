using UnityEngine;
using System.Collections.Generic;

// This will be used to define the moth points in the game

public class VerletPoint
{
    public Vector2 position;
    public Vector2 prev_position;
    public float radius;

    public VerletPoint(Vector2 position, float radius)
    {
        this.position = position;
        this.prev_position = position;
        this.radius = radius;
    }

    public void Update(float dt, Vector2 accel, float damping)
    {
        Vector2 temp = position;

        Vector2 velocity = (position - prev_position) * damping;

        position += velocity;
        position += accel * dt * dt;

        prev_position = temp;
    }
}
