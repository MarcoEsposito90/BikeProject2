using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RoadsGenerator : MonoBehaviour
{
    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- ATTRIBUTES ----------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #region ATTRIBUTES

    [Range(0, 200)]
    public int sinuosity;

    public Quadrant.Neighborhood neighborhood;

    [Range(0, 10)]
    public int roadsWidth;

    [Range(2, 5)]
    public int maxLinks;

    [Range(1.0f, 2.0f)]
    public float maximumSegmentLength;

    private Graph<Vector2, ControlPoint> controlPointsGraph;
    private bool debug;
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

        // 1) expand the graph -----------------------------------------------------------------------
        debug = chunk.position.Equals(new Vector2(0, 0));
        chunk.roadsComputed = true;
        List<Graph<Vector2, ControlPoint>.GraphItem> graphItems = getGraphRoadsNodes(chunk);

        // 2) compute the curves ---------------------------------------------------------------------
        List<BezierCurve> curves = new List<BezierCurve>();
        for (int i = 0; i < graphItems.Count; i++)
        {
            Graph<Vector2, ControlPoint>.GraphItem gi = graphItems[i];

            for (int j = 0; j < gi.links.Count; j++)
            {
                Graph<Vector2, ControlPoint>.GraphItem gj = gi.links[j];
                ControlPoint startTangent = computeTangent(gi, gj);
                ControlPoint endTangent = computeTangent(gj, gi);
                BezierCurve c = new BezierCurve(gi.item, startTangent, gj.item, endTangent);
                curves.Add(c);
            }
        }

        // 3) map filtering 
        modifyHeightMap(map, chunk, curves);
    }

    #endregion  // METHODS


    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- ROADS CREATION ------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #region ROADS

    private List<Graph<Vector2, ControlPoint>.GraphItem> getGraphRoadsNodes(MapChunk chunk)
    {
        List<Graph<Vector2, ControlPoint>.GraphItem> graphItems = new List<Graph<Vector2, ControlPoint>.GraphItem>();
        float maxLength = maximumSegmentLength * (chunk.size / (float)chunk.subdivisions);

        foreach (Quadrant q in chunk.quadrants.Values)
        {
            foreach (Quadrant neighbor in q.getNeighbors(neighborhood))
            {
                Dictionary<Vector2, ControlPoint> pointsToLink = new Dictionary<Vector2, ControlPoint>();
                List<ControlPoint> neighborPoints = neighbor.getNeighborsPoints(neighborhood);

                for (int i = 0; i < neighborPoints.Count; i++)
                {
                    ControlPoint cp = neighborPoints[i];

                    if (cp.distance(neighbor.roadsControlPoint) < maxLength)
                        pointsToLink.Add(cp.position, cp);
                }

                Graph<Vector2, ControlPoint>.GraphItem item = controlPointsGraph.addItem(neighbor.roadsControlPoint.position,
                                                                                        neighbor.roadsControlPoint,
                                                                                        pointsToLink);
                graphItems.Add(item);
            }

        }

        return graphItems;
    }


    /* ------------------------------------------------------------------------------------------------- */
    public ControlPoint computeTangent(Graph<Vector2, ControlPoint>.GraphItem graphItem, Graph<Vector2, ControlPoint>.GraphItem exclude)
    {
        float averageX = 0;
        float averageY = 0;
        int denom = graphItem.links.Count - 1;
        float xDist = 0;
        float yDist = 0;

        for (int k = 0; k < graphItem.links.Count; k++)
        {
            if (graphItem.links[k].Equals(exclude))
                continue;

            Graph<Vector2, ControlPoint>.GraphItem gk = graphItem.links[k];
            xDist = gk.item.position.x - graphItem.item.position.x;
            yDist = gk.item.position.y - graphItem.item.position.y;
            averageX += xDist / (float)denom;
            averageY += yDist / (float)denom;
        }

        Vector2 meanTangent = new Vector2(averageX, averageY);
        Vector2 tangentPosition = meanTangent + graphItem.item.position;
        float angle = Mathf.Atan2(averageY,averageX);
        
        float distanceFromTangent = Vector2.Distance(exclude.item.position, tangentPosition);
        float distanceFromPoint = Vector2.Distance(exclude.item.position, graphItem.item.position);

        if (distanceFromTangent > distanceFromPoint)
            angle = Mathf.PI + angle;

        tangentPosition.x = graphItem.item.position.x + Mathf.Cos(angle) * sinuosity;
        tangentPosition.y = graphItem.item.position.y + Mathf.Sin(angle) * sinuosity;

        ControlPoint tangent = new ControlPoint(tangentPosition, ControlPoint.Type.Tangent);
        return tangent;
    }

    #endregion

    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- MAP FILTERING -------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #region MAP_FILTERING

    private void modifyHeightMap(float[,] map, MapChunk chunk, List<BezierCurve> curves)
    {

        foreach (BezierCurve c in curves)
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
