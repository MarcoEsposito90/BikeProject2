using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;

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

    private Graph<Vector2, ControlPoint> controlPointsGraph;
    private Queue<RoadsCallbackData> roadsQueue;
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
        roadsQueue = new Queue<RoadsCallbackData>();
        mapGenerator = this.GetComponent<MapGenerator>();
    }


    void Update()
    {
        lock (roadsQueue)
        {
            for (int i = 0; i < roadsQueue.Count; i++)
            {
                RoadsCallbackData callbackData = roadsQueue.Dequeue();
                callbackData.callback(callbackData.data);

                /*  IMPORTANT INFO -------------------------------------------------------------
                    the results are dequeued here in the update function, in order
                    to use them in the main thread. This is necessary, since it is not
                    possible to use them in secondary threads (for example, you cannot create
                    a mesh) 
                */
            }
        }
    }

    #endregion


    /* ///////////////////////////////////////////////////////////////////////////////////////////////// */
    /* ///-------------------------------------------------------------------------------------------/// */
    /* ///----------------------------- METHODS -----------------------------------------------------/// */
    /* ///-------------------------------------------------------------------------------------------/// */
    /* ///////////////////////////////////////////////////////////////////////////////////////////////// */

    #region METHODS

    public void requestRoadsData(MapSector sector, Action<Road.RoadData> callback)
    {
        ThreadStart ts = delegate
        {
            generateRoads(sector, callback);
        };

        Thread t = new Thread(ts);
        t.Start();
    }


    /* --------------------------------------------------------------------------------------------- */
    private void generateRoads(MapSector sector, Action<Road.RoadData> callback)
    {
        List<Road.RoadData> roadsDatas = new List<Road.RoadData>();

        // 1) expand the graph - ----------------------------------------------------------------------
        debug = sector.position.Equals(new Vector2(0, 0));
        sector.roadsComputed = true;
        List<Graph<Vector2, ControlPoint>.GraphItem> graphItems = getGraphRoadsNodes(sector);

        // 2) compute the curves ---------------------------------------------------------------------
        List<ICurve> curves = getCurves(graphItems);

        // 3) generate meshes ------------------------------------------------------------------------
        foreach (ICurve c in curves)
        {
            RoadMeshGenerator.RoadMeshData rmd = RoadMeshGenerator.generateRoadMeshData(c);
            Road.Key key = new Road.Key(c.startPoint(), c.endPoint());
            Road.RoadData data = new Road.RoadData(rmd, key, sector, c);
            roadsDatas.Add(data);
        }

        // 4) enqueue results ------------------------------------------------------------------------
        lock (roadsQueue)
        {
            foreach (Road.RoadData rd in roadsDatas)
            {
                roadsQueue.Enqueue(new RoadsCallbackData(rd, callback));
            }
        }

    }

    #endregion  // METHODS


    /* --------------------------------------------------------------------------------------------- */
    /* -------------------------------- ROADS CREATION --------------------------------------------- */
    /* --------------------------------------------------------------------------------------------- */

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
    public List<ICurve> getCurves(List<Graph<Vector2, ControlPoint>.GraphItem> graphItems)
    {
        List<ICurve> curves = new List<ICurve>();
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
                //Debug.Log("curve from " + gi.item.position + " to " + gj.item.position + " / " + c.startPoint() + " - " + c.endPoint());

                curves.Add(c);
            }
        }


        return curves;
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

    /* --------------------------------------------------------------------------------------------- */
    /* -------------------------------- MAP FILTERING ---------------------------------------------- */
    /* --------------------------------------------------------------------------------------------- */

    #region ROADS_MAP


    /* ------------------------------------------------------------------------------------------------- */
    public float[,] generateRoadsMap(int width, int height, MapSector sector, List<ICurve> curves)
    {
        float[,] roadsMap = new float[width, height];

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                roadsMap[x, y] = 0;

        foreach (ICurve curve in curves)
        {
            for (float t = 0.0f; t <= 1.0f; t += 0.01f)
            {
                Vector2 point = curve.pointOnCurve(t);

                int localX = (int)(point.x - (sector.position.x - 0.5f) * sector.size);
                int localY = (int)((sector.position.y + 0.5f) * sector.size - point.y);

                int startX = Mathf.Max(0, localX - roadsWidth);
                int endX = Mathf.Min(width - 1, localX + roadsWidth);
                int startY = Mathf.Max(0, localY - roadsWidth);
                int endY = Mathf.Min(height - 1, localY + roadsWidth);

                for (int j = startX; j <= endX; j++)
                {
                    for (int k = startY; k <= endY; k++)
                    {
                        if (j < 0 || j >= width) continue;
                        if (k < 0 || k >= height) continue;
                        roadsMap[j, k] = 1.0f;
                    }
                }
            }
        }

        //Debug.Log("finished roads mapping for " + sector.position);
        return roadsMap;
    }

    #endregion


    /* ///////////////////////////////////////////////////////////////////////////////////////////// */
    /* ///---------------------------------------------------------------------------------------/// */
    /* ///----------------------------- SUBCLASSES ----------------------------------------------/// */
    /* ///---------------------------------------------------------------------------------------/// */
    /* ///////////////////////////////////////////////////////////////////////////////////////////// */

    #region SUBCLASSES

    public struct RoadsCallbackData
    {
        public readonly Road.RoadData data;
        public readonly Action<Road.RoadData> callback;

        public RoadsCallbackData(Road.RoadData data, Action<Road.RoadData> callback)
        {
            this.data = data;
            this.callback = callback;
        }
    }

    #endregion
}
