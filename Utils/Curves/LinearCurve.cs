using UnityEngine;
using System.Collections;
using System;

public class LinearCurve : ICurve
{
    public Vector2 start { get; private set; }
    public Vector2 end { get; private set; }
    public float M;
    public float Q;

    public LinearCurve(Vector2 start, Vector2 end)
    {
        this.start = start;
        this.end = end;
        M = (end.y - start.y) / (end.x - start.x);
        Q = start.y - M * start.x;
    }


    /* ---------------------------------------------------------------- */
    public Vector2 derivate1(float t, bool normalize)
    {
        Vector2 derivate = new Vector2(1, M);
        if (normalize)
            derivate /= derivate.magnitude;
        return derivate;
    }


    /* ---------------------------------------------------------------- */
    public Vector2 derivate2(float t, bool normalize)
    {
        return Vector2.zero;
    }


    /* ---------------------------------------------------------------- */
    public Vector2 getRightVector(float t, bool normalize)
    {
        Vector2 right = new Vector2(1, -1.0f / M);
        if (normalize)
            right /= right.magnitude;
        return right;
    }


    /* ---------------------------------------------------------------- */
    public float length()
    {
        return Vector2.Distance(start, end);
    }


    /* ---------------------------------------------------------------- */
    public float parameterOnCurveArchLength(float normalizedLength)
    {
        return normalizedLength;
    }


    /* ---------------------------------------------------------------- */
    public Vector2 pointOnCurve(float t)
    {
        if (t < 0) t = 0;
        else if (t > 1) t = 1;

        float x = start.x + t * (end.x - start.x);
        return new Vector2(x, M * x + Q);
    }


    /* ---------------------------------------------------------------- */
    public Vector2 startPoint()
    {
        return start;
    }


    /* ---------------------------------------------------------------- */
    public Vector2 endPoint()
    {
        return end;
    }


    /* ---------------------------------------------------------------- */
    public bool overlapsSquare(Vector2 center, float side)
    {
        float left = center.x - side / 2.0f;
        float right = center.x + side / 2.0f;
        float top = center.y + side / 2.0f;
        float bottom = center.y - side / 2.0f;

        for (float t = 0.0f; t <= 1.0f; t += 0.1f)
        {
            Vector2 point = pointOnCurve(t);
            if (point.x >= left && point.x <= right &&
                point.y >= bottom && point.y <= top)
            {
                return true;
            }
        }

        return false;
    }
}
