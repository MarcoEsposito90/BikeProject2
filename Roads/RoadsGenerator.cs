using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RoadsGenerator : MonoBehaviour
{
    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- ATTRIBUTES ----------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    public bool displayRoads;

    [Range(0, 100)]
    public int roadsCurveRay;

    [Range(1, 10)]
    public int roadsWidth;

    [Range(2, 8)]
    public int roadsDensity;

    private Dictionary<Vector2, ControlPoint> controlPoints;
    private Dictionary<CurveSegmentId, CurveSegment> curveSegments;
    private int chunkSize;
    private int quadrantWidth, quadrantHeight;
    private float seedX, seedY;
    private System.Random random;

    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- UNITY CALLBACKS ------------------------------------------------ */
    /* ------------------------------------------------------------------------------------------------- */

    void Start()
    {
        //this.chunkRoadsData = new Dictionary<Vector2, RoadsData>();
        this.controlPoints = new Dictionary<Vector2, ControlPoint>();
        this.curveSegments = new Dictionary<CurveSegmentId, CurveSegment>();

        chunkSize = this.GetComponent<EndlessTerrainGenerator>().chunkSize;
        quadrantWidth = (int)(chunkSize / (float)roadsDensity);
        quadrantHeight = (int)(chunkSize / (float)roadsDensity);

        random = new System.Random();
        seedX = ((float)random.NextDouble()) * random.Next(100);
        seedY = ((float)random.NextDouble()) * random.Next(100);
    }


    /* ///////////////////////////////////////////////////////////////////////////////////////////////// */
    /* ///-------------------------------------------------------------------------------------------/// */
    /* ///----------------------------- METHODS -----------------------------------------------------/// */
    /* ///-------------------------------------------------------------------------------------------/// */
    /* ///////////////////////////////////////////////////////////////////////////////////////////////// */

    public void generateRoads(float[,] map, Vector2 chunkPosition)
    {
        lock (controlPoints)
        {
            Debug.Log("chunk " + chunkPosition + " acquired lock");
            // 1) create adjacent chunks roads data
            calculateControlPoints(chunkPosition);

            // 2) calculate paths
            List<CurveSegment> curves = calculatePaths(map, chunkPosition);

            // 3) map filtering 
            modifyHeightMap(map, chunkPosition, curves);
        }

        
    }


    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- CONTROL POINTS ------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    private void calculateControlPoints(Vector2 chunkPosition)
    {
        for (int j = -1; j <= roadsDensity; j++)
        {
            for (int i = -1; i <= roadsDensity; i++)
            {
                // 1) compute normalized coordinates of quadrant -------------
                float quadrantX = (chunkPosition.x - 0.5f + (1.0f / (float)roadsDensity) * i);
                float quadrantY = (chunkPosition.y - 0.5f + (1.0f / (float)roadsDensity) * j);
                Vector2 quadrantCoordinates = new Vector2(quadrantX, quadrantY);

                if (controlPointExists(quadrantCoordinates))
                    continue;

                // 2) get perlin value relative to coordinates ----------------
                float randomX = Mathf.PerlinNoise(quadrantX + seedX, quadrantY + seedX);
                float randomY = Mathf.PerlinNoise(quadrantX + seedY, quadrantY + seedY);


                // 3) compute absolute coordinates of point in space ----------
                int X = Mathf.RoundToInt(randomX * (float)quadrantWidth + quadrantX * chunkSize);
                int Y = Mathf.RoundToInt(randomY * (float)quadrantHeight + quadrantY * chunkSize);

                /* ------ to be improved ---------------------- */
                float randomAngle = (float)random.NextDouble() * Mathf.PI * 2;
                int tangentX = X + Mathf.RoundToInt((Mathf.Cos(randomAngle) * roadsCurveRay));
                int tangentY = Y + Mathf.RoundToInt((Mathf.Sin(randomAngle) * roadsCurveRay));
                /* ------ to be improved ---------------------- */

                // 4) create point and add it to map --------------------------
                Vector2 center = new Vector2(X, Y);
                Vector2 tangent = new Vector2(tangentX, tangentY);
                ControlPoint point = new ControlPoint(center, tangent);
                addControlPoint(quadrantCoordinates, point);
            }
        }
    }


    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- CURVE SEGMENTS ------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    private List<CurveSegment> calculatePaths(float[,] map, Vector2 chunkPosition)
    {
        List<CurveSegment> localCurves = new List<CurveSegment>();

        for (int j = -1; j < roadsDensity; j++)
        {
            for (int i = -1; i < roadsDensity; i++)
            {
                // 1) compute normalized coordinates of quadrant -------------
                float quadrantX = (chunkPosition.x - 0.5f + (1.0f / (float)roadsDensity) * i);
                float quadrantY = (chunkPosition.y - 0.5f + (1.0f / (float)roadsDensity) * j);
                Vector2 quadrantCoordinates = new Vector2(quadrantX, quadrantY);

                if (!controlPointExists(quadrantCoordinates))
                {
                    Debug.Log("ERROR: requested control point wasn't created!");
                    continue;
                }

                // 2) create links --------------------------------------------
                List<CurveSegment> quadrantCurves = linkControlPoint(quadrantCoordinates);
                foreach (CurveSegment c in quadrantCurves)
                    if (!localCurves.Contains(c))
                        localCurves.Add(c);
            }
        }

        return localCurves;
    }


    /* ------------------------------------------------------------------------------------------------- */
    private List<CurveSegment> linkControlPoint(Vector2 quadrantCoordinates)
    {
        // 1) initialize list -------------------------------------------------------
        List<CurveSegment> pointCurves = new List<CurveSegment>();

        // 2) get control point of this quadrant ------------------------------------
        float quadrantX = quadrantCoordinates.x;
        float quadrantY = quadrantCoordinates.y;

        ControlPoint point = getControlPoint(quadrantCoordinates);

        // 3) create curve from this to right control point -------------------------
        float rightQuadrantX = quadrantX + (1.0f / roadsDensity);
        Vector2 rightQuadrant = new Vector2(rightQuadrantX, quadrantY);
        ControlPoint right = getControlPoint(rightQuadrant);

        if (right != null)
            pointCurves.Add(createLink(point, right));
        else
            Debug.Log("right missing for quadrant " + quadrantX + "," + quadrantY);

        // 4) create curve from this to top control point ---------------------------
        float topQuadrantY = quadrantY + (1.0f / roadsDensity);
        Vector2 topQuadrant = new Vector2(quadrantX, topQuadrantY);
        ControlPoint top = getControlPoint(topQuadrant);

        if (top != null)
            pointCurves.Add(createLink(point, top));
        else
            Debug.Log("top missing for quadrant " + quadrantX + "," + quadrantY);

        return pointCurves;
    }


    /* ------------------------------------------------------------------------------------------------- */
    public CurveSegment createLink(ControlPoint p1, ControlPoint p2)
    {
        CurveSegment segment = null;

        CurveSegmentId id = new CurveSegmentId(p1, p2);
        if (!curveSegmentExists(id))
        {
            segment = new CurveSegment(p1, p2);
            addCurveSegment(id, segment);
        }

        if (segment == null)
            segment = getCurveSegment(id);

        return segment;
    }



    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- MAP FILTERING -------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    private void modifyHeightMap(float[,] map, Vector2 chunkPosition, List<CurveSegment> curves)
    {

        foreach (CurveSegment c in curves)
        {
            for (float t = 0.0f; t <= 1.0f; t += 0.01f)
            {
                Vector2 point = c.pointOnCurve(t);

                int localY = (int)(point.x - (chunkPosition.x - 0.5f) * chunkSize);
                int localX = (int)((chunkPosition.y + 0.5f) * chunkSize - point.y);
                //localY = map.GetLength(1) - localY;
                localX = map.GetLength(0) - localX;

                int startX = Mathf.Max(0, localX - roadsWidth);
                int endX = Mathf.Min(map.GetLength(0) - 1, localX + roadsWidth);
                int startY = Mathf.Max(0, localY - roadsWidth);
                int endY = Mathf.Min(map.GetLength(1) - 1, localY + roadsWidth);

                for (int j = startX; j <= endX; j++)
                {
                    if (j < 0 || j >= map.GetLength(0)) continue;

                    for (int k = startY; k <= endY; k++)
                    {
                        if (k < 0 || k >= map.GetLength(1)) continue;
                        map[j, k] = 0.5f;

                    }
                }
                
            }

        }
    }


    /* ///////////////////////////////////////////////////////////////////////////////////////////////// */
    /* ///-------------------------------------------------------------------------------------------/// */
    /* ///----------------------------- THREAD-SAFE METHODS -----------------------------------------/// */
    /* ///-------------------------------------------------------------------------------------------/// */
    /* ///////////////////////////////////////////////////////////////////////////////////////////////// */

    private void addControlPoint(Vector2 quadrantPosition, ControlPoint p)
    {
        //lock (controlPoints)
            if (!controlPoints.ContainsKey(quadrantPosition))
                controlPoints.Add(quadrantPosition, p);

    }

    private ControlPoint getControlPoint(Vector2 quadrantPosition)
    {
        ControlPoint result = null;
        //lock (controlPoints)
            controlPoints.TryGetValue(quadrantPosition, out result);

        return result;
    }

    private bool controlPointExists(Vector2 quadrantPosition)
    {
        bool result = false;
        //lock (controlPoints)
            result = controlPoints.ContainsKey(quadrantPosition);

        return result;
    }

    private void addCurveSegment(CurveSegmentId id, CurveSegment segment)
    {
        //lock (curveSegments)
            if (!curveSegments.ContainsKey(id))
                curveSegments.Add(id, segment);
    }

    private CurveSegment getCurveSegment(CurveSegmentId id)
    {
        CurveSegment segment = null;
        //lock (curveSegments)
            curveSegments.TryGetValue(id, out segment);

        return segment;
    }


    private bool curveSegmentExists(CurveSegmentId id)
    {
        bool result = false;
        //lock (curveSegments)
            result = curveSegments.ContainsKey(id);

        return result;
    }


    /* ///////////////////////////////////////////////////////////////////////////////////////////////// */
    /* ///-------------------------------------------------------------------------------------------/// */
    /* ///----------------------------- DEBUG METHODS -----------------------------------------------/// */
    /* ///-------------------------------------------------------------------------------------------/// */
    /* ///////////////////////////////////////////////////////////////////////////////////////////////// */

    public void printControlPoints()
    {
        foreach(Vector2 quadrant in controlPoints.Keys)
        {
            ControlPoint point = getControlPoint(quadrant);
            Debug.Log("quadrant " + quadrant + "; cp = " + point.center + "; tangent = " + point.tangent);
        }
    }


    /* ///////////////////////////////////////////////////////////////////////////////////////////////// */
    /* ///-------------------------------------------------------------------------------------------/// */
    /* ///----------------------------- SUBCLASSES --------------------------------------------------/// */
    /* ///-------------------------------------------------------------------------------------------/// */
    /* ///////////////////////////////////////////////////////////////////////////////////////////////// */


    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- CONTROL POINT -------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */
    public class ControlPoint
    {
        public Vector2 center;
        public Vector2 tangent;

        public ControlPoint(Vector2 center, Vector2 tangent)
        {
            this.center = center;
            this.tangent = tangent;
        }
    }



    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- CURVE SEGMENT -------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */
    public class CurveSegment
    {
        public ControlPoint start;
        public ControlPoint end;
        public float ax, bx, cx, ay, by, cy;

        public CurveSegment(ControlPoint start, ControlPoint end)
        {
            this.start = start;
            this.end = end;
            computeBezierCoefficients();
        }

        /* ---------------------------------------------------------------- */
        public void computeBezierCoefficients()
        {
            cx = 3.0f * (start.tangent.x - start.center.x);
            cy = 3.0f * (start.tangent.y - start.center.y);

            bx = 3.0f * (end.tangent.x - start.tangent.x) - cx;
            by = 3.0f * (end.tangent.y - start.tangent.y) - cy;

            ax = end.center.x - start.center.x - cx - bx;
            ay = end.center.y - start.center.y - cy - by;
        }

        /* ---------------------------------------------------------------- */
        public Vector2 pointOnCurve(float t)
        {
            if (t < 0) t = 0;
            if (t > 1) t = 1;

            float x = ax * Mathf.Pow(t, 3) + bx * Mathf.Pow(t, 2) + cx * t + start.center.x;
            float y = ay * Mathf.Pow(t, 3) + by * Mathf.Pow(t, 2) + cy * t + start.center.y;
            return new Vector2(x, y);
        }
    }

    public struct CurveSegmentId
    {
        ControlPoint start;
        ControlPoint end;

        public CurveSegmentId(ControlPoint start, ControlPoint end)
        {
            this.start = start;
            this.end = end;
        }
    }

    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- BEZIER COMPUTATION --------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

}
