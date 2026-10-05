using System.Collections.Generic;
using UnityEngine;

// !! this is temporary, used to check if physics work
// i will later on switch to sprites
public class LineDrawer : MonoBehaviour
{
    public float default_width = 0.06f;
    Material mat;
    readonly List<LineRenderer> pool = new List<LineRenderer>();
    int used;

    void Awake()
    {
        mat = new Material(Shader.Find("Sprites/Default"));
    }

    public void Begin() { used = 0; }

    LineRenderer Next()
    {
        if (used == pool.Count)
        {
            GameObject go = new GameObject("Line");
            go.transform.SetParent(transform);
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.material = mat;
            lr.useWorldSpace = true;
            pool.Add(lr);
        }
        LineRenderer l = pool[used++];
        l.gameObject.SetActive(true);
        return l;
    }

    public void Line(Vector2 a, Vector2 b, Color c, float width = -1f)
    {
        LineRenderer l = Next();
        if (width < 0f) width = default_width;
        l.loop = false;
        l.positionCount = 2;
        l.startWidth = l.endWidth = width;
        l.startColor = l.endColor = c;
        l.SetPosition(0, a);
        l.SetPosition(1, b);
    }

    public void Circle(Vector2 center, float radius, Color c, int segments = 24, float width = -1f)
    {
        LineRenderer l = Next();
        if (width < 0f) width = default_width;
        l.loop = true;
        l.positionCount = segments;
        l.startWidth = l.endWidth = width;
        l.startColor = l.endColor = c;
        for (int i = 0; i < segments; i++)
        {
            float t = i * Mathf.PI * 2f / segments;
            l.SetPosition(i, center + new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * radius);
        }
    }

    public void End()
    {
        for (int i = used; i < pool.Count; i++)
            pool[i].gameObject.SetActive(false);
    }
}