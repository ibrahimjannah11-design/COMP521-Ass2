public class Cannon
{
    public GameObject cannonball;       // !! is this still needed?
    public float angle;
    public Vector2 position;
    public bool Active;
    public float timer;
    //public float radius;

    public int side;    // indicates whether cannon is on left or right cliff
                        // -1 for left, 1 for right
                        // i assumed that side can tale no other balue

    public Cannon (Vector2 position, float angle, int side)
    {
        this.Active = true;
        this.position = position;
        //this.radius = radius;
        this.angle = angle;
        this.side = side;
    }

}