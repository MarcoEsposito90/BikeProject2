using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RoadsGenerator : MonoBehaviour
{
    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- ATTRIBUTES ----------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #region ATTRIBUTES

    [Range(0, 100)]
    public int roadsCurveRay;

    [Range(1, 10)]
    public int roadsWidth;

    private Dictionary<CurveSegmentId, BezierCurve> curveSegments;
    private System.Random random;

    #endregion

    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- UNITY CALLBACKS ------------------------------------------------ */
    /* ------------------------------------------------------------------------------------------------- */

    #region UNITY

    void Start()
    {
        this.curveSegments = new Dictionary<CurveSegmentId, BezierCurve>();
        random = new System.Random();
    }


    #endregion


    /* ///////////////////////////////////////////////////////////////////////////////////////////////// */
    /* ///-------------------------------------------------------------------------------------------/// */
    /* ///----------------------------- METHODS -----------------------------------------------------/// */
    /* ///-------------------------------------------------------------------------------------------/// */
    /* ///////////////////////////////////////////////////////////////////////////////////////////////// */

    #region METHODS

    public void generateRoads(float[,] map, MapChunk chunk)
    {
        chunk.roadsComputed = true;

        // 1) calculate paths
        List<Path> paths = Path.calculatePaths(chunk);

        // 3) map filtering 
        modifyHeightMap(map, chunk, paths);
    }


    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- CONTROL POINTS ------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #endregion  // METHODS




    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- CURVE SEGMENTS ------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    //private List<BezierCurve> calculatePaths(float[,] map, Vector2 chunkPosition)
    //{
    //    List<BezierCurve> localCurves = new List<BezierCurve>();

    //    for (int j = -1; j < roadsDensity; j++)
    //    {
    //        for (int i = -1; i < roadsDensity; i++)
    //        {
    //            // 1) compute normalized coordinates of quadrant -------------
    //            float quadrantX = (chunkPosition.x - 0.5f + (1.0f / (float)roadsDensity) * i);
    //            float quadrantY = (chunkPosition.y - 0.5f + (1.0f / (float)roadsDensity) * j);
    //            Vector2 quadrantCoordinates = new Vector2(quadrantX, quadrantY);

    //            if (!controlPointExists(quadrantCoordinates))
    //            {
    //                Debug.Log("ERROR: requested control point wasn't created!");
    //                continue;
    //            }

    //            // 2) create links --------------------------------------------
    //            List<BezierCurve> quadrantCurves = linkControlPoint(quadrantCoordinates);
    //            foreach (BezierCurve c in quadrantCurves)
    //                if (!localCurves.Contains(c))
    //                    localCurves.Add(c);
    //        }
    //    }

    //    return localCurves;
    //}


    ///* ------------------------------------------------------------------------------------------------- */
    //private List<BezierCurve> linkControlPoint(Vector2 quadrantCoordinates)
    //{
    //    // 1) initialize list -------------------------------------------------------
    //    List<BezierCurve> pointCurves = new List<BezierCurve>();

    //    // 2) get control point of this quadrant ------------------------------------
    //    float quadrantX = quadrantCoordinates.x;
    //    float quadrantY = quadrantCoordinates.y;

    //    ControlPoint point = getControlPoint(quadrantCoordinates);

    //    // 3) create curve from this to right control point -------------------------
    //    float rightQuadrantX = quadrantX + (1.0f / roadsDensity);
    //    Vector2 rightQuadrant = new Vector2(rightQuadrantX, quadrantY);
    //    ControlPoint right = getControlPoint(rightQuadrant);

    //    if (right != null)
    //        pointCurves.Add(createLink(point, right));
    //    else
    //        Debug.Log("right missing for quadrant " + quadrantX + "," + quadrantY);

    //    // 4) create curve from this to top control point ---------------------------
    //    float topQuadrantY = quadrantY + (1.0f / roadsDensity);
    //    Vector2 topQuadrant = new Vector2(quadrantX, topQuadrantY);
    //    ControlPoint top = getControlPoint(topQuadrant);

    //    if (top != null)
    //        pointCurves.Add(createLink(point, top));
    //    else
    //        Debug.Log("top missing for quadrant " + quadrantX + "," + quadrantY);

    //    return pointCurves;
    //}


    ///* ------------------------------------------------------------------------------------------------- */
    //public BezierCurve createLink(ControlPoint p1, ControlPoint p2)
    //{
    //    BezierCurve segment = null;

    //    CurveSegmentId id = new CurveSegmentId(p1, p2);
    //    if (!curveSegmentExists(id))
    //    {
    //        segment = new BezierCurve(p1, p1, p2, p2);
    //        addCurveSegment(id, segment);
    //    }

    //    if (segment == null)
    //        segment = getCurveSegment(id);

    //    return segment;
    //}



    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- MAP FILTERING -------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    private void modifyHeightMap(float[,] map, MapChunk chunk, List<Path> paths)
    {

        foreach (Path p in paths)
        {
            foreach (BezierCurve c in p.curves)
            {
                for (float t = 0.0f; t <= 1.0f; t += 0.01f)
                {
                    Vector2 point = c.pointOnCurve(t);

                    int localX = (int)(point.x - (chunk.position.x - 0.5f) * chunk.size);
                    int localY = (int)((chunk.position.y + 0.5f) * chunk.size - point.y);
                    //localY = map.GetLength(1) - localY;
                    //localX = map.GetLength(0) - localX;

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

    }


    /* ///////////////////////////////////////////////////////////////////////////////////////////////// */
    /* ///-------------------------------------------------------------------------------------------/// */
    /* ///----------------------------- UTILITY METHODS ---------------------------------------------/// */
    /* ///-------------------------------------------------------------------------------------------/// */
    /* ///////////////////////////////////////////////////////////////////////////////////////////////// */

    #region UTILITY

    //private void addQuadrant(Vector2 quadrantPosition, Quadrant q)
    //{
    //    if (!quadrants.ContainsKey(quadrantPosition))
    //        quadrants.Add(quadrantPosition, q);

    //}

    //private Quadrant getQuadrant(Vector2 quadrantPosition)
    //{
    //    Quadrant result = null;
    //    quadrants.TryGetValue(quadrantPosition, out result);

    //    return result;
    //}

    //private bool quadrantExists(Vector2 quadrantPosition)
    //{
    //    bool result = false;
    //    result = quadrants.ContainsKey(quadrantPosition);

    //    return result;
    //}


    /* ------------------------------------------------------------------------------------------------- */
    private void addCurveSegment(CurveSegmentId id, BezierCurve segment)
    {
        if (!curveSegments.ContainsKey(id))
            curveSegments.Add(id, segment);
    }

    private BezierCurve getCurveSegment(CurveSegmentId id)
    {
        BezierCurve segment = null;
        curveSegments.TryGetValue(id, out segment);

        return segment;
    }


    private bool curveSegmentExists(CurveSegmentId id)
    {
        bool result = false;
        result = curveSegments.ContainsKey(id);

        return result;
    }

    #endregion



    /* ///////////////////////////////////////////////////////////////////////////////////////////////// */
    /* ///-------------------------------------------------------------------------------------------/// */
    /* ///----------------------------- DEBUG METHODS -----------------------------------------------/// */
    /* ///-------------------------------------------------------------------------------------------/// */
    /* ///////////////////////////////////////////////////////////////////////////////////////////////// */

    #region DEBUG

    //public void printControlPoints()
    //{
    //    foreach (Quadrant quadrant in quadrants.Values)
    //        Debug.Log("quadrant " + quadrant + "; cp = " + quadrant.position);
    //}


    #endregion



    /* ///////////////////////////////////////////////////////////////////////////////////////////////// */
    /* ///-------------------------------------------------------------------------------------------/// */
    /* ///----------------------------- SUBCLASSES --------------------------------------------------/// */
    /* ///-------------------------------------------------------------------------------------------/// */
    /* ///////////////////////////////////////////////////////////////////////////////////////////////// */

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

}
