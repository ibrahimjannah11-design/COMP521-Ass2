
public static class MyPhysics
{
    // First, we have a circle and segment intersection helper class

    public static Vector2 ClosestPoint(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab  = b-a;
        float len = ab.sqrMagnitude;

        // First check if a and b are distinct (aka if the line segment is just a point)
        if (len < 1e-8f)
        {
            return a;       
        }

        // t shows where the closest point to p is on the ab vector
        float t = Mathf.Clamp01(Vector2.Dot(p-a,ab)/len);
        Vector2 point = a + ab*t;
        return point;


           
    }


    // First, lets handle circle and segment intersection
    // For this, we use the above defined closest point function
    // We export the collisiton data as well to be used later on (normal and penetration)
    public static bool CircleSegment( Vector2 c, float r, Vector2 a, Vector2 b, Vector2 fallbackNormal,
                                        out Vector2 normal, out float penetration)
    {


        Vector2 closest = ClosestPoint(c,a,b);
        Vector2 d = c - closest;
        float dist = d.magnitude;

        if (dist >= r)
        {
            normal = Vector2.zero;
            penetration = 0;
            return false;
            // aka, they do NOT intersect
        }

        // Now if they do intersect, there are two cases:
        // Either the center of the circle is on the line segment, or it is not.

        if (dist < 1e-8f)   //This deals with the edgecase
        {
            // we then use the fallback normal
            normal = fallbackNormal.normalized;     // cannot divide by dist bc cant divide by 0
            penetration = r;
        }
        else                // This handles standard intersection
        {
            normal = d /dist;
            penetration = r - dist;
        }

        return true;
    }




    // Then, we handle circle on circle collision
    public static bool CircleCircle(Vector2 a, float a_rad, Vector2 b, float b_rad,
                                        out Vector2 normal, out float penetration)
    {
        // This case is simpler than line on circle collision. 
        // We check if the distance bwteen the two circle centers is less than the sum of their radii (funny word)

        Vector2 d = b - a;
        float dist = d.magnitude;

        float r = a_rad + b_rad;

        if (dist >= r)
        {
            normal = Vector2.zero;
            penetration = 0;
            return false;
        }

        // Again, lets check the edge case

        if (dist < 1e-8f)
        {
            normal =  Vector2.up;       // they perfectly overlap, so we pick any direction arbitrarily (i pick erm just cuz)
            penetration = r;
            return true;
        }

        normal = d/dist;
        penetration = r - dist;
        return true;

    }


    // Finally, lets handle point on circle collision

    public static bool CirclePoint(Vector2 c, float r, Vector2 p)
    {
        // This is probably the simplest case. 
        // Just check is the distance between circle center and the point is less than the radius


        Vector2 d = c - p;
        float dist = d.magnitude;


        return dist < r ;
        
    }


    // iknow the assignemtn doesnt ask for rotation, but i wanted to use just one physics class,
    // rather than jumping between mine and default unity physics
    // (its also pretty simepl to implemet, so why not)
    public static Vector2 Rotate(Vector2 v, float radians)
    {
        float c = Mathf.Cos(radians);
        float s = Mathf.Sin(radians);

        return new Vector2(
            v.x * c - v.y * s,
            v.x * s + v.y * c);
            // !! 8
    }
    

}