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

    private Dictionary<Vector2, RoadsData> chunkRoadsData;
    private static float seedX, seedY;

    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- UNITY CALLBACKS ------------------------------------------------ */
    /* ------------------------------------------------------------------------------------------------- */

    void Start()
    {
        this.chunkRoadsData = new Dictionary<Vector2, RoadsData>();
        System.Random r = new System.Random();
        seedX = ((float)r.NextDouble()) * r.Next(100);
        seedY = ((float)r.NextDouble()) * r.Next(100);

        //Debug.Log("seeds = " + seedX + "," + seedY);
    }

    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- METHODS -------------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    public void generateRoads(float[,] map, Vector2 chunkPosition)
    {

        // 1) create adjacent chunks roads data -----------------------------
        int chunkX = (int)chunkPosition.x;
        int chunkY = (int)chunkPosition.y;

        lock (chunkRoadsData)
        {
            for (int i = chunkX - 1; i <= chunkX + 1; i++)
                for (int j = chunkY - 1; j <= chunkY + 1; j++)
                    if (!chunkRoadsData.ContainsKey(new Vector2(i, j)))
                    {
                        Vector2 pos = new Vector2(i, j);
                        RoadsData d = new RoadsData
                            (pos,
                            map.GetLength(0) - 1,
                            roadsDensity,
                            roadsCurveRay);

                        chunkRoadsData.Add(pos, d);
                    }
        }
        

        if (!displayRoads)
            return;

        // 1) modify height map ---------------------------------------
        RoadsData data = null;
        chunkRoadsData.TryGetValue(chunkPosition, out data);
        modifyHeightMap(map, data);
    }


    /* ------------------------------------------------------------------------------------------------- */
    private void modifyHeightMap(float[,] map, RoadsData data)
    {
        lock (chunkRoadsData)
        {

            for (int i = 0; i < data.curveSegments.Count; i++)
            {
                CurveSegment segment = data.curveSegments[i];

                for (float t = 0.0f; t <= 1.0f; t += 0.01f)
                {
                    Vector2 point = segment.pointOnCurve(t);

                    int localX = (int)(point.x - (data.chunkPosition.x - 0.5f) * data.chunkSize);
                    int localY = (int)(-point.y - (data.chunkPosition.y + 0.5f) * data.chunkSize);

                    if (localX < 0 || localX >= map.GetLength(0)) continue;
                    if (localY < 0 || localY >= map.GetLength(1)) continue;

                    int startX = Mathf.Max(0, localX - roadsWidth);
                    int endX = Mathf.Min(map.GetLength(0) - 1, localX + roadsWidth);
                    int startY = Mathf.Max(0, localY - roadsWidth);
                    int endY = Mathf.Min(map.GetLength(1) - 1, localY + roadsWidth);
                    for (int j = startX; j <= endX; j++)
                        for (int k = startY; k <= endY; k++)
                            map[j, k] = 0.5f;
                }
            }
        }
        
    }


    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- ROADS DATA ----------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    public class RoadsData
    {
        public Vector2 chunkPosition;
        public int chunkSize;
        public int density;
        public float curveRay;
        public Dictionary<Vector2, ControlPoint> controlPoints;
        public List<CurveSegment> curveSegments;

        /* -------- CONSTRUCTOR ------------------------------------------------ */
        public RoadsData
            (Vector2 chunkPosition,
            int chunkSize,
            int density,
            float curveRay)
        {

            this.chunkPosition = chunkPosition;
            this.chunkSize = chunkSize;
            this.density = density;
            this.curveRay = curveRay;
            controlPoints = new Dictionary<Vector2, ControlPoint>();
            curveSegments = new List<CurveSegment>();

            calculateControlPoints();
            calculatePaths();
        }

        /* -------- CONTROL POINTS CREATION ------------------------------------- */
        private void calculateControlPoints()
        {
            int quadrantWidth = (int)((chunkSize + 1) / (float)density);
            int quadrantHeight = (int)((chunkSize + 1) / (float)density);

            //Debug.Log("---------------------------- " + chunkPosition.x + "," + chunkPosition.y + " --------------------------------------");

            for (int i = -1; i <= density; i++)
            {
                for (int j = -1; j <= density; j++)
                {
                    float perlinX = (chunkPosition.x - 0.5f + (1.0f / (float)density) * i);
                    float perlinY = (chunkPosition.y - 0.5f + (1.0f / (float)density) * j);
                    float X1 = perlinX + seedX;
                    float Y1 = perlinY + seedX;
                    float X2 = perlinX + seedY;
                    float Y2 = perlinY + seedY;
                    float randomX = Mathf.PerlinNoise(perlinX + seedX, perlinY + seedX);
                    float randomY = Mathf.PerlinNoise(perlinX + seedY, perlinY + seedY);

                    int X = Mathf.RoundToInt(randomX * (float)quadrantWidth + perlinX * chunkSize);
                    int Y = Mathf.RoundToInt(randomY * (float)quadrantHeight + perlinY * chunkSize);

                    //Debug.Log("QUADRANT " + i + "," + j + ": point = " + X + "," + Y + " || perl(" + perlinX + "," + perlinY + ") || p1(" + X1 + "," + Y1 + ") || p2(" + X2 + "," + Y2 + ")");
                    //Debug.Log("Perlin = " + perlinX + "," + perlinY);
                    //Debug.Log("random = " + randomX + "," + randomY);


                    int randomAngle = new System.Random().Next(360);
                    int tangentX = X + Mathf.RoundToInt((Mathf.Cos(randomAngle) * curveRay));
                    int tangentY = Y + Mathf.RoundToInt((Mathf.Sin(randomAngle) * curveRay));

                    Vector2 center = new Vector2(X, Y);
                    Vector2 tangent = new Vector2(tangentX, tangentY);
                    ControlPoint point = new ControlPoint(center, tangent, i, j);
                    controlPoints.Add(new Vector2(i, j), point);
                }
            }
        }

        /* -------- PATH CREATION ------------------------------------------------- */
        private void calculatePaths()
        {
            for (int i = -1; i < density; i++)
            {
                for (int j = -1; j < density; j++)
                {
                    ControlPoint point = null;
                    controlPoints.TryGetValue(new Vector2(i, j), out point);

                    if (point == null)
                        continue;

                    ControlPoint right = null;
                    if (controlPoints.TryGetValue(new Vector2(i + 1.0f, j), out right))
                        curveSegments.Add(new CurveSegment(point, right));

                    ControlPoint top = null;
                    if (controlPoints.TryGetValue(new Vector2(i, j + 1.0f), out top))
                        curveSegments.Add(new CurveSegment(point, top));

                }
            }
        }
    }



    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- CONTROL POINT -------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */
    public class ControlPoint
    {
        public Vector2 center;
        public Vector2 tangent;
        public int quadrantX, quadrantY;

        public ControlPoint(Vector2 center, Vector2 tangent, int quadrantX, int quadrantY)
        {
            this.center = center;
            this.tangent = tangent;
            this.quadrantX = quadrantX;
            this.quadrantY = quadrantY;
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



    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- BEZIER COMPUTATION --------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

}
