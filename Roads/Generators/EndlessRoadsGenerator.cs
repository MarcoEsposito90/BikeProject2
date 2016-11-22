using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class EndlessRoadsGenerator : MonoBehaviour
{

    /* ----------------------------------------------------------------------------------------- */
    /* ---------------------------- ATTRIBUTES ------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    public const int DENSITY_ONE = 5;

    #region ATTRIBUTES

    [Range(1, DENSITY_ONE + 1)]
    public int controlPointsDensity;
    public float controlPointArea { get; private set; }
    public float scaledControlPointArea { get; private set; }

    [Range(2, 10)]
    public int NumberOfLods;

    [Range(3, 10)]
    public int radius;
    private float threshold;

    [Range(1, 3)]
    public int removeRadius;
    private float removeThreshold;

    public RoadsGenerator roadsGenerator;

    public GameObject controlPointPrefab;
    public GameObject roadPrefab;
    public GameObject controlPointsContainer;
    public GameObject roadsContainer;

    private int sectorSize;
    private int scale;
    private Transform viewer;
    private Vector3 latestViewerRecordedPosition;
    private float viewerDistanceUpdate;
    //private float[] LODThresholds;

    private Dictionary<Vector2, ControlPoint> controlPoints;
    private Dictionary<Graph<Vector2, ControlPoint>.Link, Road> roads;
    private PoolManager<Graph<Vector2, ControlPoint>.Link> roadsPoolManager;
    private PoolManager<Vector2> controlPointsPoolManager;
    public BlockingQueue<Road.RoadData> roadsResultsQueue { get; private set; }
    public BlockingQueue<ControlPoint.ControlPointData> cpsResultsQueue { get; private set; }

    #endregion


    /* ----------------------------------------------------------------------------------------- */
    /* ---------------------------- UNITY ------------------------------------------------------ */
    /* ----------------------------------------------------------------------------------------- */

    #region UNITY

    /* ----------------------------------------------------------------------------------------- */
    void Awake()
    {
        controlPoints = new Dictionary<Vector2, ControlPoint>();
        roads = new Dictionary<Graph<Vector2, ControlPoint>.Link, Road>();
        roadsResultsQueue = new BlockingQueue<Road.RoadData>();
        cpsResultsQueue = new BlockingQueue<ControlPoint.ControlPointData>();

        roadsPoolManager = new PoolManager<Graph<Vector2, ControlPoint>.Link>(10, true, roadPrefab, roadsContainer);
        controlPointsPoolManager = new PoolManager<Vector2>(400, true, controlPointPrefab, controlPointsContainer);
    }

    /* ----------------------------------------------------------------------------------------- */
    void Update()
    {

        while (!roadsResultsQueue.isEmpty())
        {
            Road.RoadData data = roadsResultsQueue.Dequeue();
            onRoadDataReceived(data);
        }

        while (!cpsResultsQueue.isEmpty())
        {
            ControlPoint.ControlPointData data = cpsResultsQueue.Dequeue();
            onCpDataReceived(data);
        }

        float distance = Vector3.Distance(latestViewerRecordedPosition, viewer.position);
        if (distance >= viewerDistanceUpdate)
        {
            createControlPoints();
            updateControlPoints();
            latestViewerRecordedPosition = viewer.position;
        }

    }



    #endregion


    /* ----------------------------------------------------------------------------------------- */
    /* ---------------------------- METHODS ---------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region METHODS

    /* ----------------------------------------------------------------------------------------- */
    public void initialize
        (int sectorSize,
        int scale,
        float viewerDistanceUpdate,
        Transform viewer,
        float[] LODThresholds)
    {
        this.sectorSize = sectorSize;
        float denom = (DENSITY_ONE + 1) - controlPointsDensity;
        if (denom <= 0) denom = 1.0f / Mathf.Pow(2, -denom + 1); 
        float multiplier = 1.0f / denom;
        controlPointArea = (float)sectorSize / multiplier;
        scaledControlPointArea = controlPointArea * scale;

        this.scale = scale;
        this.viewerDistanceUpdate = viewerDistanceUpdate;
        this.viewer = viewer;
        //this.LODThresholds = LODThresholds;

        threshold = ((float)radius + 0.5f) * scaledControlPointArea;
        removeThreshold = ((float)(radius + removeRadius) + 0.5f) * scaledControlPointArea;
    }


    /* ----------------------------------------------------------------------------- */
    /* ---------------------------- CONTROL POINTS --------------------------------- */
    /* ----------------------------------------------------------------------------- */

    #region CONTROL_POINTS

    /* ----------------------------------------------------------------------------------------- */
    private void createControlPoints()
    {
        Vector3 position = viewer.position;
        float startX = position.x - threshold;
        float endX = position.x + threshold;
        float startY = position.z - threshold;
        float endY = position.z + threshold;

        for (float x = startX; x <= endX; x += scaledControlPointArea)
            for (float y = startY; y <= endY; y += scaledControlPointArea)
            {
                int CPX = Mathf.RoundToInt(x / (float)scaledControlPointArea + 0.1f);
                int CPY = Mathf.RoundToInt(y / (float)scaledControlPointArea + 0.1f);

                Vector2 CPGridPos = new Vector2(CPX, CPY);
                if (controlPoints.ContainsKey(CPGridPos))
                    continue;

                float realCPX = CPX * scaledControlPointArea;
                float realCPY = CPY * scaledControlPointArea;

                Vector3 center = new Vector3(realCPX, 0, realCPY);
                Vector3 sizes = new Vector3(scaledControlPointArea, scaledControlPointArea, scaledControlPointArea);
                Bounds b = new Bounds(center, sizes);
                float dist = Mathf.Sqrt(b.SqrDistance(position));

                if (dist < threshold)
                    createControlPoint(CPGridPos);
            }
    }


    /* ----------------------------------------------------------------------------------------- */
    private void createControlPoint(Vector2 gridPos)
    {
        GameObject prefab = controlPointsPoolManager.acquireObject(gridPos);
        ControlPoint newCP = new ControlPoint(gridPos, prefab, controlPointArea, scale);
        controlPoints.Add(gridPos, newCP);
        roadsGenerator.sendNewControlPoint(newCP);
    }


    /* ----------------------------------------------------------------------------------------- */
    private void updateControlPoints()
    {
        List<ControlPoint> toBeRemoved = new List<ControlPoint>();
        foreach (ControlPoint cp in controlPoints.Values)
        {
            float d = cp.bounds.SqrDistance(viewer.transform.position);
            d = (float)Math.Sqrt(d);

            if (d > removeThreshold)
            {
                cp.resetPrefab();
                controlPointsPoolManager.releaseObject(cp.gridPosition);
                toBeRemoved.Add(cp);
            }
        }

        foreach (ControlPoint cp in toBeRemoved)
            controlPoints.Remove(cp.gridPosition);
    }


    #endregion

    /* ----------------------------------------------------------------------------- */
    /* ---------------------------- CONTROL POINTS --------------------------------- */
    /* ----------------------------------------------------------------------------- */

    #region CROSSROADS

    private void createCrossRoad(Graph<Vector2, ControlPoint>.GraphItem node)
    {
        bool canCreateCrossroad = true;
        List<Road> incomingRoads = new List<Road>();
        foreach (Graph<Vector2, ControlPoint>.Link l in node.links)
        {
            if (!roads.ContainsKey(l))
            {
                canCreateCrossroad = false;
                break;
            }

            incomingRoads.Add(roads[l]);
        }

        if (canCreateCrossroad)
            roadsGenerator.requestCrossroad(node.item, incomingRoads);
    }

    #endregion

    /* ------------------------------------------------------------------------------------ */
    /* ---------------------------- DATA RECEIVED ----------------------------------------- */
    /* ------------------------------------------------------------------------------------ */

    #region DATA_RECEIVED

    /* ----------------------------------------------------------------------------------------- */
    private void onRoadDataReceived(Road.RoadData roadData)
    {
        Road r = null;

        if (roads.ContainsKey(roadData.key))
        {
            r = roads[roadData.key];
        }
        else
        {
            GameObject newRoadObj = roadsPoolManager.acquireObject(roadData.key);
            r = new Road(
                roadData.curve,
                roadData.key,
                newRoadObj,
                scale,
                ((RoadMeshGenerator.RoadMeshData)roadData.meshData).numberOfSections,
                ((RoadMeshGenerator.RoadMeshData)roadData.meshData).heights);

            roads.Add(roadData.key, r);
        }

        r.setMesh(roadData.meshData.createMesh(), roadData.texture);
        createCrossRoad(roadData.key.from);
        createCrossRoad(roadData.key.to);
    }


    /* ----------------------------------------------------------------------------------------- */
    private void onCpDataReceived(ControlPoint.ControlPointData data)
    {
        //Debug.Log("data received for " + data.gridPosition);
        if (!controlPoints.ContainsKey(data.gridPosition))
        {
            //Debug.Log("request deceased for control point " + data.gridPosition);
            return;
        }

        ControlPoint cp = controlPoints[data.gridPosition];
        cp.setMesh(data.crossRoadMeshData.createMesh());
    }

    #endregion

    #endregion
}
