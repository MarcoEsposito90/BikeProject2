using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class GeometryUtilities
{
    public enum QuadDirection { Right, Left, Up, Down};
    public const int LEFT_INDEX = 0;
    public const int UP_INDEX = 1;
    public const int RIGHT_INDEX = 2;
    public const int DOWN_INDEX = 3;

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


    public static int roundToInt(float num)
    {
        int sign = num < 0 ? -1 : 1;
        float absNum = num * (float)sign;

        int a = (int)absNum;
        int b = a + 1;
        float half = (float)a + 0.5f;

        if (absNum == half)
            return b * sign;
        if (absNum < half)
            return a * sign;

        return b * sign;
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

    #endregion

    /* ----------------------------------------------------------------------- */
    /* -------------------------- 2D MATHS ----------------------------------- */
    /* ----------------------------------------------------------------------- */

    #region DIRECTIONS

    /* ----------------------------------------------------------------------- */
    public static int getIndex(QuadDirection direction)
    {
        switch (direction)
        {
            case QuadDirection.Left:
                return LEFT_INDEX;
            case QuadDirection.Right:
                return RIGHT_INDEX;
            case QuadDirection.Up:
                return UP_INDEX;
            case QuadDirection.Down:
                return DOWN_INDEX;
            default:
                return LEFT_INDEX;
        }
    }


    /* ----------------------------------------------------------------------- */
    public static QuadDirection getDirection(int index)
    {
        switch (index)
        {
            case LEFT_INDEX:
                return QuadDirection.Left;
            case RIGHT_INDEX:
                return QuadDirection.Right;
            case UP_INDEX:
                return QuadDirection.Up;
            default:
                return QuadDirection.Down;
        }
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
    public static QuadDirection[] getQuadDirections(Vector2 position)
    {
        QuadDirection[] directions = new QuadDirection[4];
        QuadDirection first = getQuadDirection(position);
        directions[0] = first;

        int sign = (int)Mathf.Sign(position.x * position.y);
        Vector2 temp = new Vector2(position.y * sign, position.x * sign);
        QuadDirection second = getQuadDirection(temp);
        directions[1] = second;

        int mul = Mathf.Abs(temp.x) > Mathf.Abs(temp.y) ? -1 : 1;
        temp = new Vector2(temp.x * mul, temp.y * (-mul));
        QuadDirection third = getQuadDirection(temp);
        directions[2] = third;

        mul = -mul;
        temp = new Vector2(position.x * mul, position.y * (-mul));
        QuadDirection fourth = getQuadDirection(temp);
        directions[3] = fourth;

        return directions;
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
