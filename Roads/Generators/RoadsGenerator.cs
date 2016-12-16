using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;

public class RoadsGenerator : MonoBehaviour
{

    public static readonly string ROAD_ADHERENCE = "RoadsGenerator.Adherence";
    public static readonly string MAX_ROAD_ADHERENCE = "RoadsGenerator.MaxAdherence";

    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- ATTRIBUTES ----------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #region ATTRIBUTES

    public ControlPoint.Neighborhood neighborhood;

    [Range(0, 10)]
    public int crossRoadsDimension;
    private float distanceFromCrossroad;

    [Range(1, 6)]
    public int sinuosity;
    private float tangentRescale;

    [Range(1.0f, 2.0f)]
    public float maximumSegmentLength;
    private float maxLength;

    [Range(0.0f, 0.5f)]
    public float minimumSegmentLength;
    private float minLength;

    [Range(0.0f, 0.5f)]
    public float minimumRoadsHeight;

    [Range(0.5f, 1.0f)]
    public float maximumRoadsHeight;

    const int MAX_ADHERENCE = 20;
    [Range(1, MAX_ADHERENCE)]
    public int adherence;

    private int scale;
    private float controlPointArea;

    public GameObject roadSegment;
    public Texture2D roadSegmentTexture;
    public GameObject crossroadPrefab;
    public Texture2D crossroadTexture;

    private MeshData roadSegmentMeshData;
    public EndlessRoadsGenerator parent;

    private Graph<Vector2, ControlPoint> controlPointsGraph;
    private Dictionary<Graph<Vector2, ControlPoint>.Link, ICurve> curves;
    private bool initialized;
    private object synchVariable;
    #endregion

    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- UNITY CALLBACKS ------------------------------------------------ */
    /* ------------------------------------------------------------------------------------------------- */

    #region UNITY

    void Awake()
    {
        initialized = false;
        synchVariable = new object();
        controlPointsGraph = new Graph<Vector2, ControlPoint>();
        curves = new Dictionary<Graph<Vector2, ControlPoint>.Link, ICurve>();

        Mesh m = roadSegment.GetComponent<MeshFilter>().sharedMesh;
        roadSegmentMeshData = new MeshData(m.vertices, m.triangles, m.uv, m.normals, 0);

        distanceFromCrossroad = crossRoadsDimension;
        tangentRescale = 0.25f * sinuosity - 0.25f;

        roadSegmentTexture.filterMode = FilterMode.Bilinear;
        roadSegmentTexture.wrapMode = TextureWrapMode.Clamp;

        GlobalInformation.Instance.addData(ROAD_ADHERENCE, adherence);
        GlobalInformation.Instance.addData(MAX_ROAD_ADHERENCE, MAX_ADHERENCE);
    }

    #endregion

    /* -------------------------------------------------------------------------------------- */
    /* -------------------------------- CONTROL POINTS -------------------------------------- */
    /* -------------------------------------------------------------------------------------- */

    #region CONTROL_POINTS

    public void sendNewControlPoint(ControlPoint cp)
    {
        ThreadStart ts = delegate
        {
            lock (synchVariable)
            {
                addControlPoint(cp);
            }
        };

        Thread t = new Thread(ts);
        t.Start();
    }

    /* -------------------------------------------------------------------------------------- */
    private void addControlPoint(ControlPoint cp)
    {
        if (!initialized)
        {
            scale = cp.scale;
            controlPointArea = cp.AreaSize;
            maxLength = maximumSegmentLength * cp.AreaSize;
            minLength = minimumSegmentLength * cp.AreaSize;
            initialized = true;
        }

        // add point to graph --------------------------------------
        Graph<Vector2, ControlPoint>.GraphItem gi = controlPointsGraph.createItem(cp.gridPosition, cp);

        // check if the point is not on water or on mountains ------
        float noiseValue = NoiseGenerator.Instance.getNoiseValue(
            1,
            gi.item.position.x,
            gi.item.position.y);

        bool linkable = noiseValue < maximumRoadsHeight && noiseValue > minimumRoadsHeight;
        cp.linkable = linkable;
        if (!linkable)
            return;

        // if the point is linkable to others, we will get here ----

        // create links (if possible) ------------------------------
        foreach (Vector2 target in cp.getNeighborsGridPositions(neighborhood))
        {
            if (controlPointsGraph.containsItem(target))
            {
                ControlPoint toLink = controlPointsGraph[target].item;
                if (!toLink.linkable)
                    continue;

                float dist = cp.distance(toLink);
                if (dist < maxLength && dist > minLength)
                {
                    if (!checkLinkFeasibility(cp, toLink))
                        continue;

                    Graph<Vector2, ControlPoint>.Link l =
                        controlPointsGraph.createLink(cp.gridPosition, toLink.gridPosition);
                }
            }
        }


        // create new curves, where it is possible ------------------
        foreach (Graph<Vector2, ControlPoint>.Link l in controlPointsGraph.links)
        {
            lock (curves)
            {
                if (curves.ContainsKey(l))
                    continue;
            }

            ControlPoint from = l.from.item;
            ControlPoint to = l.to.item;

            bool canCreateCurve = true;
            foreach (Vector2 target in from.getNeighborsGridPositions(neighborhood))
                if (!controlPointsGraph.containsItem(target))
                {
                    canCreateCurve = false;
                    break;
                }

            if (!canCreateCurve)
                continue;

            foreach (Vector2 target in to.getNeighborsGridPositions(neighborhood))
                if (!controlPointsGraph.containsItem(target))
                {
                    canCreateCurve = false;
                    break;
                }

            if (canCreateCurve)
                createCurve(l);

        }
    }


    /* -------------------------------------------------------------------------------------- */
    private bool checkLinkFeasibility(ControlPoint a, ControlPoint b)
    {
        int numberOfChecks = 10;
        for (int i = 1; i < numberOfChecks; i++)
        {
            Vector2 checkPos = a.position + (b.position - a.position) / (float)numberOfChecks;
            float n = NoiseGenerator.Instance.getNoiseValue(1, checkPos.x, checkPos.y);
            if (n > maximumRoadsHeight || n < minimumRoadsHeight)
                return false;
        }

        return true;
    }



    /* -------------------------------------------------------------------------------------- */
    public void removeControlPoint(ControlPoint cp)
    {
        Graph<Vector2, ControlPoint>.GraphItem gi = controlPointsGraph[cp.gridPosition];

        foreach (Graph<Vector2, ControlPoint>.Link l in gi.links)
        {
            curves.Remove(l);
            parent.roadsRemoveQueue.Enqueue(l);
        }

        controlPointsGraph.removeItem(cp.gridPosition);
    }

    #endregion

    /* -------------------------------------------------------------------------------------- */
    /* -------------------------------- CURVES ---------------------------------------------- */
    /* -------------------------------------------------------------------------------------- */

    #region CURVES

    private void createCurve(Graph<Vector2, ControlPoint>.Link link)
    {

        //Debug.Log("creating curve " + link.from.item.gridPosition + " - " + link.to.item.position);
        Vector2 startTangent = getTangent(link.from, link);
        Vector2 endTangent = getTangent(link.to, link);

        BezierCurve c = new BezierCurve(
            link.from.item.position,
            startTangent,
            link.to.item.position,
            endTangent);

        lock (curves)
        {
            curves.Add(link, c);
        }

        RoadMeshGenerator.RoadMeshData rmd = RoadMeshGenerator.generateMeshData(
            link,
            c,
            distanceFromCrossroad,
            roadSegmentMeshData);
        Road.RoadData data = new Road.RoadData(rmd, link, c, roadSegmentTexture);
        parent.roadsResultsQueue.Enqueue(data);
    }


    /* -------------------------------------------------------------------------------------- */
    private Vector2 getTangent(
        Graph<Vector2, ControlPoint>.GraphItem point, 
        Graph<Vector2, ControlPoint>.Link exclude)
    {
        List<Vector2> linksPositions = new List<Vector2>();

        foreach (Graph<Vector2, ControlPoint>.Link l in point.links)
        {
            if (l.Equals(exclude))
                continue;

            ControlPoint cp = null;
            if (l.from.Equals(point))
                cp = l.to.item;
            else
                cp = l.from.item;

            linksPositions.Add(cp.position);
        }

        Graph<Vector2, ControlPoint>.GraphItem toward = null;
        if (exclude.from.Equals(point))
            toward = exclude.to;
        else
            toward = exclude.from;

        Vector2 tangent = point.item.getAverageVector(
            linksPositions,
            false,
            false,
            true,
            toward.item.position);

        return tangent * tangentRescale + point.item.position;
    }

    #endregion


    /* -------------------------------------------------------------------------- */
    /* ---------------------- CROSSROADS ---------------------------------------- */
    /* -------------------------------------------------------------------------- */

    #region CROSSROADS

    public void requestCrossroad(ControlPoint center, List<Road> roads)
    {
        CrossroadsMeshGenerator.CrossroadMeshData crmd;
        crmd = CrossroadsMeshGenerator.generateMeshData(
            center,
            roads,
            distanceFromCrossroad,
            roadSegmentMeshData,
            crossroadPrefab);

        ControlPoint.ControlPointData data = new ControlPoint.ControlPointData(
            center.gridPosition,
            crmd,
            crossroadTexture,
            roadSegmentTexture);

        parent.cpsResultsQueue.Enqueue(data);
    }

    #endregion



    /* -------------------------------------------------------------------------- */
    /* ---------------------- SPLIT --------------------------------------------- */
    /* -------------------------------------------------------------------------- */

    #region SPLIT

    public void requestSplit(Vector2 position)
    {
        int gridX = Mathf.RoundToInt(position.x / (controlPointArea * scale));
        int gridY = Mathf.RoundToInt(position.y / (controlPointArea * scale));

        Vector2 gridPos = new Vector2(gridX, gridY);
        ControlPoint nearest = controlPointsGraph.nodes[gridPos].item;
        float minDist = Vector2.Distance(nearest.position, position/(float)scale);

        for(int i = -1; i <= 1; i++)
            for(int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;

                Vector2 inc = new Vector2(i, j);
                ControlPoint c = controlPointsGraph.nodes[gridPos + inc].item;
                float d = Vector2.Distance(c.position, position/(float)scale);

                if(d < minDist)
                {
                    nearest = c;
                    minDist = d;
                }
            }

        Debug.Log("nearest is " + nearest.gridPosition);
    }

    #endregion
}


