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
    public int sinuosity;

    [Range(0, 10)]
    public int roadsWidth;

    [Range(1.0f, 2.0f)]
    public float segmentMaximumLength;

    private Graph<Vector2, ControlPoint> controlPointsGraph;

    #endregion

    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- UNITY CALLBACKS ------------------------------------------------ */
    /* ------------------------------------------------------------------------------------------------- */

    #region UNITY

    void Awake()
    {
        controlPointsGraph = new Graph<Vector2, ControlPoint>();
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
        bool debug = chunk.position.Equals(new Vector2(0, 0));

        if (debug)
            Debug.Log("generate roads started for " + chunk.position);

        chunk.roadsComputed = true;
        int maximumLinks = 3;

        List<Graph<Vector2, ControlPoint>.GraphItem> graphItems = new List<Graph<Vector2, ControlPoint>.GraphItem>();

        foreach (Quadrant q in chunk.quadrants.Values)
        {
            if (debug)
                Debug.Log("     quadrant " + q.position + " started");

            Dictionary<Vector2, ControlPoint> pointsToLink = new Dictionary<Vector2, ControlPoint>();
            List<ControlPoint> neighborPoints = q.getNeighbors();

            int links = 0;

            for (int i = 0; i < neighborPoints.Count; i++)
            {
                ControlPoint cp = neighborPoints[i];

                if (debug)
                    Debug.Log("         neighbor " + cp.position);

                pointsToLink.Add(cp.position, cp);
                links++;

                if (links == maximumLinks)
                    break;
            }

            if (debug)
                foreach (ControlPoint cp in pointsToLink.Values)
                    Debug.Log("must link to " + cp.position);


            Graph<Vector2, ControlPoint>.GraphItem item = controlPointsGraph.addItem(q.roadsControlPoint.position,
                                                                                        q.roadsControlPoint,
                                                                                        pointsToLink);

            graphItems.Add(item);
        }

        if (debug)
        {
            Debug.Log("links: " + graphItems.Count);
            for (int i = 0; i < graphItems.Count; i++)
            {
                Debug.Log("     " + graphItems[i].item.position);
                Debug.Log("linked to: ");
                foreach (Graph<Vector2, ControlPoint>.GraphItem gi in graphItems[i].links)
                    Debug.Log(gi.item.position);
            }
        }
            


        //// 1) calculate paths
        //List<Path> paths = Path.calculatePaths(chunk, segmentMaximumLength, sinuosity);

        //// 3) map filtering 
        //modifyHeightMap(map, chunk, paths);
    }

    #endregion  // METHODS




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
