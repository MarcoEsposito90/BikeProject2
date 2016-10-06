using UnityEngine;
using System.Collections;

public class BezierCurve {

    public Vector2 start;
    public Vector2 startTangent;
    public Vector2 end;
    public Vector2 endTangent;
    public float ax, bx, cx, ay, by, cy;

    public BezierCurve(Vector2 start, Vector2 startTangent, Vector2 end, Vector2 endTangent)
    {
        this.start = start;
        this.end = end;
        this.startTangent = startTangent;
        this.endTangent = endTangent;
        computeBezierCoefficients();
    }

    /* ---------------------------------------------------------------- */
    public void computeBezierCoefficients()
    {
        cx = 3.0f * (startTangent.x - start.x);
        cy = 3.0f * (startTangent.y - start.y);

        bx = 3.0f * (endTangent.x - startTangent.x) - cx;
        by = 3.0f * (endTangent.y - startTangent.y) - cy;

        ax = end.x - start.x - cx - bx;
        ay = end.y - start.y - cy - by;
    }


    /* ---------------------------------------------------------------- */
    public Vector2 pointOnCurve(float t)
    {
        if (t < 0) t = 0;
        if (t > 1) t = 1;

        float x = ax * Mathf.Pow(t, 3) + bx * Mathf.Pow(t, 2) + cx * t + start.x;
        float y = ay * Mathf.Pow(t, 3) + by * Mathf.Pow(t, 2) + cy * t + start.y;
        return new Vector2(x, y);
    }
}
