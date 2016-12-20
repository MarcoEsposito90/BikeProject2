using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class GeometryUtilities
{
    public enum QuadDirection { Right, Left, Up, Down};


    /* ----------------------------------------------------------------------- */
    /* ----------------------------- MATHS ----------------------------------- */
    /* ----------------------------------------------------------------------- */

    public static bool isInsideRange(float num, float A, float B)
    {
        float min = Mathf.Min(A, B);
        float max = min == A ? B : A;

        if (num < min || num > max)
            return false;

        return true;
    }


    /* ----------------------------------------------------------------------- */
    /* -------------------------- 2D MATHS ----------------------------------- */
    /* ----------------------------------------------------------------------- */

    #region 2DMATH

    /* ----------------------------------------------------------------------- */
    public static Vector2 rotate(Vector2 point, float angle)
    {
        return new Vector2(
            Mathf.Cos(angle) * point.x - Mathf.Sin(angle) * point.y,
            Mathf.Sin(angle) * point.x + Mathf.Cos(angle) * point.y);
    }


    /* ----------------------------------------------------------------------- */
    public static bool SegmentsIntersection2D(
        Vector2 segment1A,
        Vector2 segment1B,
        Vector2 segment2A,
        Vector2 segment2B,
        out Vector2 intersection)
    {
        intersection = Vector2.zero;

        float m1 = (segment1B.y - segment1A.y) / (segment1B.x - segment1A.x);
        float m2 = (segment2B.y - segment2A.y) / (segment2B.x - segment2A.x);

        if (m1 == m2) // segments are parallel
            return false;

        float q1 = segment1A.y - m1 * segment1A.y;
        float q2 = segment2A.y - m2 * segment2A.y;

        float X = (q1 - q2) / (m2 - m1);
        float Y = m1 * X + q1;

        // must check if intersection is inside both segments
        if (!isInsideRange(X, segment1A.x, segment1B.x))
            return false;

        if (!isInsideRange(X, segment2A.x, segment2B.x))
            return false;

        if (!isInsideRange(Y, segment1A.y, segment1B.y))
            return false;

        if (!isInsideRange(Y, segment2A.y, segment2B.y))
            return false;

        intersection = new Vector2(X, Y);
        return true;
    }


    /* ----------------------------------------------------------------------- */
    public static QuadDirection getQuadDirection(Vector2 position)
    {
        if (Mathf.Abs(position.x) > Mathf.Abs(position.y))
        {
            if (position.x > 0)
                return QuadDirection.Right;
            else
                return QuadDirection.Left;
        }
        else
        {
            if (position.y > 0)
                return QuadDirection.Up;
            else
                return QuadDirection.Down;
        }
    }


    /* ----------------------------------------------------------------------- */
    public static Vector2 getVector2D(QuadDirection direction)
    {
        switch (direction)
        {
            case QuadDirection.Right:
                return Vector2.right;

            case QuadDirection.Left:
                return Vector2.left;

            case QuadDirection.Up:
                return Vector2.up;

            default:
                return Vector2.down;
        }
    }

    #endregion

    /* ----------------------------------------------------------------------- */
    /* -------------------------- 3D MATHS ----------------------------------- */
    /* ----------------------------------------------------------------------- */

    #region 3DMATH

    public static Vector3 calculateDimensions(Vector3[] vertices)
    {
        float minZ = vertices[0].z;
        float maxZ = minZ;
        float minY = vertices[0].y;
        float maxY = minY;
        float minX = vertices[0].x;
        float maxX = minX;

        for (int i = 1; i < vertices.Length; i++)
        {
            if (vertices[i].z > maxZ)
                maxZ = vertices[i].z;
            if (vertices[i].z < minZ)
                minZ = vertices[i].z;

            if (vertices[i].y > maxY)
                maxY = vertices[i].y;
            if (vertices[i].y < minY)
                minY = vertices[i].y;

            if (vertices[i].x > maxX)
                maxX = vertices[i].x;
            if (vertices[i].x < minX)
                minX = vertices[i].x;
        }

        return new Vector3(maxX - minX, maxY - minY, maxZ - minZ);
    }


    /* ----------------------------------------------------------------------- */
    public static Vector3 CrossProduct(Vector3 v1, Vector3 v2)
    {
        float x = v1.y * v2.z - v1.z * v2.y;
        float y = v1.x * v2.z - v1.z * v2.x;
        float z = v1.x * v2.y - v1.y * v2.x;
        return new Vector3(x, y, z);
    }


    public static float getAngle(Vector2 position)
    {
        float a = Vector2.Angle(new Vector2(1, 0), position);
        float a2 = position.y >= 0 ? a : 360 - a;
        return a2;
    }


    #endregion


    /* ----------------------------------------------------------------------- */
    /* -------------------------- FILTERING ---------------------------------- */
    /* ----------------------------------------------------------------------- */

    #region FILTERING


    public static float[] averageFilterKernel(int kernelDimension)
    {
        float[] kernel = new float[kernelDimension + 1];

        for (int i = 0; i < kernel.Length; i++)
            kernel[i] = 1.0f / (float)(kernel.Length * 2);

        return kernel;
    }


    /* ----------------------------------------------------------------------- */
    private static void normalizeKernel(float[] kernel)
    {
        float sum = 0;

        for (int i = 0; i < kernel.Length; i++)
            sum += i == 0 ? kernel[i] : kernel[i] * 2;

        for (int i = 0; i < kernel.Length + 1; i++)
            kernel[i] /= sum;
    }


    /* ----------------------------------------------------------------------- */
    public static void filter(float[] src, float[] kernel)
    {
        // kernel is assumed to be simmetric and with odd dimension, so this 
        // method wants a half to be mirrored
        float[] clone = (float[])src.Clone();
        int arrayLength = src.Length;

        for (int i = 0; i < arrayLength; i++)
        {
            float newValue = 0;

            for (int j = 0; j < kernel.Length; j++)
            {
                int index1 = Mathf.Max(0, i - j);
                int index2 = Mathf.Min(arrayLength - 1, i + j);
                newValue += clone[index1] * kernel[j];

                if (j != 0)
                    newValue += clone[index2] * kernel[j];
            }

            src[i] = newValue;
        }
    }


    #endregion

    /* ----------------------------------------------------------------------- */
    /* -------------------------- CLOCKWISE COMPARER ------------------------- */
    /* ----------------------------------------------------------------------- */

    #region CLOCKWISE_COMPARER

    public class ClockWiseComparer : IComparer<Vector2>
    {
        public Vector2 center;
        public bool clockwise;
        public float startAngle;

        // -----------------------------------------------------------
        public ClockWiseComparer(Vector2 center, bool clockwise)
        {
            this.center = center;
            this.clockwise = clockwise;
        }


        // -----------------------------------------------------------
        public int Compare(Vector2 x, Vector2 y)
        {
            float angle1 = getAngle(x);
            float angle2 = getAngle(y);

            if (angle1 < angle2) return -1;
            else if (angle1 == angle2) return 0;
            return 1;
        }


        // -----------------------------------------------------------
        public float getAngle(Vector2 other)
        {
            Vector2 d = other - center;
            float a = GeometryUtilities.getAngle(d);
            return a - startAngle;
        }

    }

    #endregion
}
