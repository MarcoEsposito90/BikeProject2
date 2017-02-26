using UnityEngine;
using System.Collections;
using System;

public class BezierCurve : ICurve
{

    public Vector2 start;
    public Vector2 startTangent;
    public Vector2 end;
    public Vector2 endTangent;
    public float ax, bx, cx, ay, by, cy;
    public const int lengthSamplesNumber = 20;
    public float archLength;
    public float[] lengthSamples;

    public BezierCurve(Vector2 start, Vector2 startTangent, Vector2 end, Vector2 endTangent)
    {
        this.start = start;
        this.end = end;
        this.startTangent = startTangent;
        this.endTangent = endTangent;
        this.archLength = -1.0f;
        computeBezierCoefficients();
        computeLengthSamples();
    }


    /* ---------------------------------------------------------------- */
    private void computeBezierCoefficients()
    {
        cx = 3.0f * (startTangent.x - start.x);
        cy = 3.0f * (startTangent.y - start.y);

        bx = 3.0f * (endTangent.x - startTangent.x) - cx;
        by = 3.0f * (endTangent.y - startTangent.y) - cy;

        ax = end.x - start.x - cx - bx;
        ay = end.y - start.y - cy - by;
    }


    /* ---------------------------------------------------------------- */
    private void computeLengthSamples()
    {
        lengthSamples = new float[lengthSamplesNumber + 1];
        float increment = 1.0f / (float)lengthSamplesNumber;
        float currentLength = 0;
        Vector2 previous = pointOnCurve(0);
        lengthSamples[0] = 0;
        float t = 0;

        for (int i = 1; i <= lengthSamplesNumber; i++)
        {
            t += increment;
            Vector2 point = pointOnCurve(t);
            currentLength += Vector2.Distance(point, previous);
            lengthSamples[i] = currentLength;
            previous = point;
        }

        archLength = currentLength;
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


    /* ---------------------------------------------------------------- */
    public float parameterOnCurveArchLength(float normalizedLength)
    {
        if (normalizedLength <= 0)
            return 0;

        if (normalizedLength >= 1)
            return 1;

        float t = 0;
        float length = normalizedLength * archLength;
        float segment = 1.0f / (float)lengthSamplesNumber;

        for (int i = lengthSamples.Length - 1; i > 0; i--)
        {
            if (length <= lengthSamples[i] && length >= lengthSamples[i - 1])
            {
                float L1 = lengthSamples[i - 1];
                float L2 = lengthSamples[i];
                float r = (length - L1) / (L2 - L1) * segment;
                t = segment * (i - 1);
                t += r;
                return t;
            }
        }

        return t;
    }


    /* ---------------------------------------------------------------- */
    public Vector2 derivate1(float t, bool normalize)
    {
        float x = ax * 3 * Mathf.Pow(t, 2) + bx * 2 * t + cx;
        float y = ay * 3 * Mathf.Pow(t, 2) + by * 2 * t + cy;

        if (normalize)
        {
            float module = Mathf.Sqrt(x * x + y * y);
            x = x / module;
            y = y / module;
        }

        return new Vector2(x, y);
    }


    /* ---------------------------------------------------------------- */
    public Vector2 derivate2(float t, bool normalize)
    {
        float x = ax * 6 * t + bx * 2;
        float y = ay * 6 * t + by * 2;

        if (normalize)
        {
            float module = Mathf.Sqrt(x * x + y * y);
            x = x / module;
            y = y / module;
        }

        return new Vector2(x, y);
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
    public float length()
    {
        return archLength;
    }


    /* ---------------------------------------------------------------- */
    public Vector2 getRightVector(float t, bool normalize)
    {
        Vector2 p = pointOnCurve(t);
        Vector2 deriv = derivate1(t, normalize);
        Vector3 crossProd = GeometryUtilities.CrossProduct(
            Vector3.up,
            new Vector3(deriv.x, 0, deriv.y));
        Vector2 rightVect = new Vector2(crossProd.x, crossProd.z);

        return rightVect;
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
            float tp = parameterOnCurveArchLength(t);
            Vector2 point = pointOnCurve(tp);

            if( point.x >= left && point.x <= right &&
                point.y >= bottom && point.y <= top)
            {
                return true;
            }
        }

        return false;
    }
}
