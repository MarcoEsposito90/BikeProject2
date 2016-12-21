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

    [Range(0, 10)]
    public int sinuosity;
    private float tangentRescale;
    private float tangentRotate;

    [Range(1.0f, 2.0f)]
    public float maximumSegmentLength;
    private float maxLength;

    //[Range(0.0f, 0.5f)]
    //public float minimumSegmentLength;
    //private float minLength;

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
    public CrossroadHandler crossroadPrefab;
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
        tangentRescale = sinuosity / 5.0f;
        tangentRotate = sinuosity * 10.0f;

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
            //minLength = minimumSegmentLength * cp.AreaSize;
            initialized = true;
        }

        bool debug = (bool)GlobalInformation.Instance.getData(CreateRoads.ROADS_DEBUG);

        // add point to graph --------------------------------------
        Graph<Vector2, ControlPoint>.GraphItem gi = controlPointsGraph.createItem(cp.position, cp);

        // check if the point is not on water or on mountains ------
        float noiseValue = NoiseGenerator.Instance.getNoiseValue(
            1,
            gi.item.position.x,
            gi.item.position.y);

        bool linkable = noiseValue < maximumRoadsHeight && noiseValue > minimumRoadsHeight;
        cp.linkable = linkable;
        if (!linkable)
            return;

        // create links with other control points -------------------

        int linkCount = 0;
        foreach (Graph<Vector2, ControlPoint>.GraphItem targetItem in controlPointsGraph.nodes.Values)
        {
            if (linkCount >= gi.item.maximumLinks)
                break;

            if (targetItem.Equals(gi) || targetItem.links.Count >= targetItem.item.maximumLinks)
                continue;

            ControlPoint toLink = targetItem.item;
            if (!toLink.linkable)
                continue;

            if (debug)
                Debug.Log("checking " + targetItem.item.position);

            float dist = cp.distance(toLink);
            if (dist < maxLength /*&& dist > minLength*/)
            {
                Graph<Vector2, ControlPoint>.Link l =
                    controlPointsGraph.createLink(cp.position, toLink.position);
                linkCount++;
            }
        }

        // create the crossroad ----------------
        foreach (Graph<Vector2, ControlPoint>.Link link in gi.links)
        {
            createCurve(link);
            Graph<Vector2, ControlPoint>.GraphItem other = null;
            if (link.from.Equals(gi))
                other = link.to;
            else
                other = link.from;

            createCrossroad(other);
        }

        if (gi.links.Count == 0)
            Debug.Log("couldn't link!");
        createCrossroad(gi);
    }


    /* -------------------------------------------------------------------------------------- */
    public void removeControlPoint(ControlPoint cp)
    {
        Graph<Vector2, ControlPoint>.GraphItem gi = controlPointsGraph[cp.position];

        foreach (Graph<Vector2, ControlPoint>.Link l in gi.links)
        {
            curves.Remove(l);
            parent.roadsRemoveQueue.Enqueue(l);
        }

        controlPointsGraph.removeItem(cp.position);
    }

    #endregion

    /* -------------------------------------------------------------------------------------- */
    /* -------------------------------- CURVES ---------------------------------------------- */
    /* -------------------------------------------------------------------------------------- */

    #region CURVES

    private void createCurve(Graph<Vector2, ControlPoint>.Link link)
    {
        Vector2 difference = link.to.item.position - link.from.item.position;
        difference = (difference / tangentRescale);
        System.Random r = new System.Random();

        float angle = r.Next((int)tangentRotate) / 180.0f * (float)Math.PI;
        Vector2 startTangent = GeometryUtilities.rotate(difference, angle);
        startTangent += link.from.item.position;

        angle = r.Next((int)tangentRotate) / 180.0f * (float)Math.PI;
        Vector2 endTangent = GeometryUtilities.rotate(-difference, angle);
        endTangent += link.to.item.position;

        BezierCurve c = new BezierCurve(
            link.from.item.position,
            startTangent,
            link.to.item.position,
            endTangent);

        curves.Add(link, c);
        RoadMeshGenerator.RoadMeshData rmd = RoadMeshGenerator.generateMeshData(
            link,
            c,
            distanceFromCrossroad,
            roadSegmentMeshData);
        Road.RoadData data = new Road.RoadData(rmd, link, c, roadSegmentTexture);
        parent.roadsResultsQueue.Enqueue(data);
    }


    #endregion


    /* -------------------------------------------------------------------------- */
    /* ---------------------- CROSSROADS ---------------------------------------- */
    /* -------------------------------------------------------------------------- */

    #region CROSSROADS

    public void createCrossroad(Graph<Vector2, ControlPoint>.GraphItem node)
    {
        Dictionary<Graph<Vector2, ControlPoint>.Link, ICurve> localCurves = new Dictionary<Graph<Vector2, ControlPoint>.Link, ICurve>();
        foreach (Graph<Vector2, ControlPoint>.Link link in node.links)
            localCurves.Add(link, curves[link]);

        CrossroadsMeshGenerator.CrossroadMeshData crmd = CrossroadsMeshGenerator.generateMeshData(
            node.item,
            localCurves,
            distanceFromCrossroad,
            roadSegmentMeshData,
            crossroadPrefab);

        ControlPoint.ControlPointData data = new ControlPoint.ControlPointData(
            node.item.gridPosition,
            crmd);

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
        Graph<Vector2, ControlPoint>.GraphItem nearest = controlPointsGraph.nodes[gridPos];
        float minDist = Vector2.Distance(nearest.item.position, position / (float)scale);

        for (int i = -1; i <= 1; i++)
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;

                Vector2 inc = new Vector2(i, j);
                Graph<Vector2, ControlPoint>.GraphItem c = controlPointsGraph.nodes[gridPos + inc];
                float d = Vector2.Distance(c.item.position, position / (float)scale);

                if (d < minDist)
                {
                    nearest = c;
                    minDist = d;
                }
            }

        Debug.Log("nearest is " + nearest.item.gridPosition);

        Graph<Vector2, ControlPoint>.Link toBeLinked = null;
        Vector2 connectionPoint = Vector2.zero;
        foreach (Graph<Vector2, ControlPoint>.Link l in nearest.links)
        {
            for (int i = 0; i <= 100; i++)
            {
                float t = curves[l].parameterOnCurveArchLength(i / 100.0f);
                Vector2 point = curves[l].pointOnCurve(t);
                float d = Vector2.Distance(point, position);

                if (d < minDist)
                {
                    toBeLinked = l;
                    minDist = d;
                    connectionPoint = point;
                }
            }
        }


    }

    #endregion
}


