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

    [Range(10, 50)]
    public int roadsFlattening;

    [Range(1.0f, 2.0f)]
    public float maximumSegmentLength;

    [Range(0.0f, 0.5f)]
    public float minimumRoadsHeight;

    [Range(0.5f, 1.0f)]
    public float maximumRoadsHeight;

    public Color roadsColor;
    public Texture2D roadsTexture;

    private Graph<Vector2, ControlPoint> controlPointsGraph;
    private MapGenerator mapGenerator;
    private bool debug;
    #endregion

    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- UNITY CALLBACKS ------------------------------------------------ */
    /* ------------------------------------------------------------------------------------------------- */

    #region UNITY

    void Awake()
    {
        controlPointsGraph = new Graph<Vector2, ControlPoint>();
        mapGenerator = this.GetComponent<MapGenerator>();
    }


    #endregion


    /* ///////////////////////////////////////////////////////////////////////////////////////////////// */
    /* ///-------------------------------------------------------------------------------------------/// */
    /* ///----------------------------- METHODS -----------------------------------------------------/// */
    /* ///-------------------------------------------------------------------------------------------/// */
    /* ///////////////////////////////////////////////////////////////////////////////////////////////// */

    #region METHODS

    public void generateRoads(float[,] map, MapSector chunk)
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

            float noiseValue = Noise.getNoiseValue(mapGenerator.noiseScale,
                                    gi.item.position.x + mapGenerator.offsetX,
                                    gi.item.position.y + mapGenerator.offsetY,
                                    mapGenerator.numberOfFrequencies,
                                    mapGenerator.frequencyMultiplier,
                                    mapGenerator.amplitudeDemultiplier);

            if (noiseValue >= maximumRoadsHeight || noiseValue <= minimumRoadsHeight)
                continue;

            for (int j = 0; j < gi.links.Count; j++)
            {
                Graph<Vector2, ControlPoint>.GraphItem gj = gi.links[j];

                noiseValue = Noise.getNoiseValue(mapGenerator.noiseScale,
                                gj.item.position.x + mapGenerator.offsetX,
                                gj.item.position.y + mapGenerator.offsetY,
                                mapGenerator.numberOfFrequencies,
                                mapGenerator.frequencyMultiplier,
                                mapGenerator.amplitudeDemultiplier);

                if (noiseValue <= minimumRoadsHeight || noiseValue >= maximumRoadsHeight)
                    continue;

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

    private List<Graph<Vector2, ControlPoint>.GraphItem> getGraphRoadsNodes(MapSector chunk)
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
        float angle = Mathf.Atan2(averageY, averageX);

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

    private void modifyHeightMap(float[,] map, MapSector chunk, List<BezierCurve> curves)
    {

        foreach (BezierCurve c in curves)
            MapProcessing.medianFilter(map, chunk, c, roadsWidth, roadsFlattening, mapGenerator);
        
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
