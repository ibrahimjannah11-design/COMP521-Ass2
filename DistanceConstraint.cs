using UnityEngine;

// This will be used to define the contarints between the points in the simulation
public class DistanceConstraint
{
    public VerletPoint p1;
    public VerletPoint p2;
    public float rest_length;
    public float stiffness;

    public DistanceConstraint(
        VerletPoint p1,
        VerletPoint p2,
        float rest_length,
        float stiff)
    {
        this.p1 = p1;
        this.p2 = p2;
        this.rest_length = rest_length;
        this.stiffness = stiff;
    }

    public void Resolve(float length_scale)
    {
        Vector2 delta = p2.position - p1.position;

        float current_length = delta.magnitude;

        if (current_length < 1e-6f)
            return;

        float difference =
            (current_length - rest_length * length_scale)
            / current_length;

        // Move points to satisfy the constraint
        Vector2 correction = delta * 0.5f * difference * stiffness;

        p1.position += correction;
        p2.position -= correction;
    }
}