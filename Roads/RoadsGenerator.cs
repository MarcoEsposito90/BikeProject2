using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RoadsGenerator : MonoBehaviour
{
    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- ATTRIBUTES ----------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    public bool displayRoads;

    [Range(0,100)]
    public int roadsCurveRay;

    [Range(1, 10)]
    public int roadsWidth;

    [Range(2, 8)]
    public int roadsDensity;

    private Dictionary<Vector2, RoadsData> chunkRoadsData;


    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- UNITY CALLBACKS ------------------------------------------------ */
    /* ------------------------------------------------------------------------------------------------- */

    void Start()
    {
        this.chunkRoadsData = new Dictionary<Vector2, RoadsData>();
    }

    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- METHODS -------------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    public void generateRoads(float[,] map, Vector2 chunkPosition)
    {

        // 1) create adjacent chunks roads data -----------------------------
        int chunkX = (int)chunkPosition.x;
        int chunkY = (int)chunkPosition.y;

        for(int i = chunkX - 1; i <= chunkX + 1; i++)
            for(int j = chunkY - 1; j <= chunkY + 1; j++)
                if(!chunkRoadsData.ContainsKey(new Vector2(i, j)))
                {
                    Vector2 pos = new Vector2(i, j);
                    RoadsData d = new RoadsData
                        (pos, 
                        roadsDensity, 
                        roadsCurveRay, 
                        map.GetLength(0), 
                        map.GetLength(1));

                    chunkRoadsData.Add(pos, d);
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
        for (int i = 0; i < data.curveSegments.Count; i++)
        {
            CurveSegment segment = data.curveSegments[i];
            for (float t = 0.0f; t <= 1.0f; t += 0.01f)
            {
                Vector2 point = segment.pointOnCurve(t);
                int x = Mathf.RoundToInt(point.x);
                int y = Mathf.RoundToInt(point.y);

                if (x < 0) x = 0;
                if (x >= map.GetLength(0)) x = map.GetLength(0) - 1;
                if (y < 0) y = 0;
                if (y >= map.GetLength(1)) y = map.GetLength(1) - 1;

                int startX = Mathf.Max(0, x - roadsWidth);
                int endX = Mathf.Min(map.GetLength(0) - 1, x + roadsWidth);
                int startY = Mathf.Max(0, y - roadsWidth);
                int endY = Mathf.Min(map.GetLength(1) - 1, y + roadsWidth);
                for (int j = startX; j <= endX; j++)
                    for (int k = startY; k <= endY; k++)
                        map[j, k] = 0.5f;
            }
        }
    }


    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- ROADS DATA ----------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    public class RoadsData
    {
        public Vector2 chunkPosition;
        public int density;
        public float curveRay;
        public int width;
        public int height;
        public ControlPoint[] controlPoints;
        public List<CurveSegment> curveSegments;

        /* -------- CONSTRUCTOR ------------------------------------------------ */
        public RoadsData(Vector2 chunkPosition, int density, float curveRay, int width, int height)
        {
            this.chunkPosition = chunkPosition;
            this.width = width;
            this.height = height;
            this.density = density;
            this.curveRay = curveRay;
            controlPoints = new ControlPoint[density * density];
            curveSegments = new List<CurveSegment>();

            calculateControlPoints();
            calculatePath();
        }

        /* -------- CONTROL POINTS CREATION ------------------------------------- */
        private void calculateControlPoints()
        {
            System.Random random = new System.Random();
            int quadrantWidth = (int)(width / (float)density);
            int quadrantHeight = (int)(height / (float)density);

            for (int i = 0; i < density; i++)
            {
                for (int j = 0; j < density; j++)
                {
                    int randomX = random.Next(quadrantWidth - 1);
                    int randomY = random.Next(quadrantHeight - 1);
                    int X = randomX + i * quadrantWidth;
                    int Y = randomY + j * quadrantHeight;

                    int randomAngle = random.Next(360);
                    int tangentX = X + Mathf.RoundToInt((Mathf.Cos(randomAngle) * curveRay));
                    int tangentY = Y + Mathf.RoundToInt((Mathf.Sin(randomAngle) * curveRay));

                    if (tangentX < 0) tangentX = 0;
                    if (tangentX >= width) tangentX = width;
                    if (tangentY < 0) tangentY = 0;
                    if (tangentY >= height) tangentY = height;

                    Vector2 center = new Vector2(X, Y);
                    Vector2 tangent = new Vector2(tangentX, tangentY);
                    ControlPoint point = new ControlPoint(center, tangent, i, j);
                    controlPoints[j * density + i] = point;
                }
            }
        }

        /* -------- PATH CREATION ------------------------------------------------- */
        private void calculatePath()
        {
            List<ControlPoint> points = new List<ControlPoint>(controlPoints);
            ControlPoint previous = null;
            System.Random random = new System.Random();

            for (int i = 0; i < controlPoints.Length; i++)
            {
                int index = random.Next(points.Count - 1);

                if (previous != null)
                {
                    CurveSegment segment = new CurveSegment(previous, points[index]);
                    curveSegments.Add(segment);
                }

                previous = points[index];
                points.RemoveAt(index);
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
