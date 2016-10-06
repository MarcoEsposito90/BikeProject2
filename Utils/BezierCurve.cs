using UnityEngine;
using System.Collections;

public class BezierCurve {

    public ControlPoint start;
    public ControlPoint startTangent;
    public ControlPoint end;
    public ControlPoint endTangent;
    public float ax, bx, cx, ay, by, cy;

    public BezierCurve(ControlPoint start, ControlPoint startTangent, ControlPoint end, ControlPoint endTangent)
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
        cx = 3.0f * (startTangent.position.x - start.position.x);
        cy = 3.0f * (startTangent.position.y - start.position.y);

        bx = 3.0f * (endTangent.position.x - startTangent.position.x) - cx;
        by = 3.0f * (endTangent.position.y - startTangent.position.y) - cy;

        ax = end.position.x - start.position.x - cx - bx;
        ay = end.position.y - start.position.y - cy - by;
    }


    /* ---------------------------------------------------------------- */
    public Vector2 pointOnCurve(float t)
    {
        if (t < 0) t = 0;
        if (t > 1) t = 1;

        float x = ax * Mathf.Pow(t, 3) + bx * Mathf.Pow(t, 2) + cx * t + start.position.x;
        float y = ay * Mathf.Pow(t, 3) + by * Mathf.Pow(t, 2) + cy * t + start.position.y;
        return new Vector2(x, y);
    }
}
