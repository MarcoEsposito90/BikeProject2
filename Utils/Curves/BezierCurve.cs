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
        lengthSamples = new float[lengthSamplesNumber];
        float increment = 1.0f / (float)lengthSamplesNumber;
        float currentLength = 0;
        Vector2 previous = Vector2.zero;

        int counter = 0;
        for (float i = 0; i <= 1.0f; i += increment)
        {
            Vector2 point = pointOnCurve(i);
            float addLength = 0;
            if (i != 0)
                addLength = Vector2.Distance(point, previous);

            lengthSamples[counter] = currentLength + addLength;
            currentLength = lengthSamples[counter];
            counter++;
            previous = point;
        }

        archLength = lengthSamples[lengthSamplesNumber - 1];
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
    public float parameterOnCurveArchLength(float normalizedLength, bool fromStart)
    {
        float t = -1.0f;

        if (normalizedLength <= 0)
        {
            t = fromStart ? 0.0f : 1.0f;
            return t;
        }

        if (normalizedLength >= 1)
        {
            t = fromStart ? 1.0f : 0.0f;
            return t;
        }

        float length = normalizedLength * archLength;

        float interval = (1.0f / (float)lengthSamplesNumber);
        for (int i = 0; i < lengthSamplesNumber - 1; i++)
        {
            int index = fromStart ? i : lengthSamplesNumber - (i + 1);
            float L1 = fromStart ? lengthSamples[index] : archLength - lengthSamples[index];
            float L2 = fromStart ? lengthSamples[index + 1] : archLength - lengthSamples[index - 1];

            if (length >= L1 && length <= L2)
            {
                t = interval * (float)index;
                float rate = (length - L1) / (L2 - L1) * interval;
                if (!fromStart) rate *= -1;

                t += rate;
                break;
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
    public Vector2 getRightVector(float t)
    {
        Vector2 p = pointOnCurve(t);
        Vector2 deriv = derivate1(t, true);
        Vector3 crossProd = GeometryUtilities.CrossProduct(
            Vector3.up,
            new Vector3(deriv.x, 0, deriv.y));
        Vector2 rightVect = new Vector2(crossProd.x, crossProd.z);

        return rightVect;
    }
}
