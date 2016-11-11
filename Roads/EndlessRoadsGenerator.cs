using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class EndlessRoadsGenerator : MonoBehaviour {

    /* ----------------------------------------------------------------------------------------- */
    /* ---------------------------- ATTRIBUTES ------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region ATTRIBUTES

    [Range(-5, 5)]
    public int controlPointsDensity;
    public float controlPointArea { get; private set; }
    public float scaledControlPointArea { get; private set; }

    [Range(2, 10)]
    public int NumberOfLods;

    [Range(3,10)]
    public int radius;
    private float threshold;

    [Range(1,3)]
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
    private Dictionary<Graph<Vector2,ControlPoint>.Link, Road> roads;
    private PoolManager<Graph<Vector2, ControlPoint>.Link> roadsPoolManager;
    private PoolManager<Vector2> controlPointsPoolManager;
    public BlockingQueue<Road.RoadData> roadsResultsQueue { get; private set; }
    //public BlockingQueue<Road.RoadData> roadsResultsQueue { get; private set; }

    #endregion


    /* ----------------------------------------------------------------------------------------- */
    /* ---------------------------- UNITY ------------------------------------------------------ */
    /* ----------------------------------------------------------------------------------------- */
    
    #region UNITY

    /* ----------------------------------------------------------------------------------------- */
    void Awake ()
    {
        controlPoints = new Dictionary<Vector2, ControlPoint>();
        roads = new Dictionary<Graph<Vector2, ControlPoint>.Link, Road>();
        roadsResultsQueue = new BlockingQueue<Road.RoadData>();

        roadsPoolManager = new PoolManager<Graph<Vector2, ControlPoint>.Link>(10, true, roadPrefab, roadsContainer);
        controlPointsPoolManager = new PoolManager<Vector2>(400, true, controlPointPrefab, controlPointsContainer);
    }
	
    /* ----------------------------------------------------------------------------------------- */
	void Update () {

        while (!roadsResultsQueue.isEmpty())
        {
            Road.RoadData data = roadsResultsQueue.Dequeue();
            onRoadDataReceived(data);
        }

        float distance = Vector3.Distance(latestViewerRecordedPosition, viewer.position);
        if (distance >= viewerDistanceUpdate)
        {
            // TODO
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

        if (controlPointsDensity == 0) controlPointsDensity = 1;
        float multiplier =  controlPointsDensity > 0 ? controlPointsDensity : 
                            - 1.0f / (float)controlPointsDensity;

        controlPointArea = (float)sectorSize / multiplier;
        scaledControlPointArea = controlPointArea * scale;

        this.scale = scale;
        this.viewerDistanceUpdate = viewerDistanceUpdate;
        this.viewer = viewer;
        //this.LODThresholds = LODThresholds;

        threshold = ((float)radius + 0.5f) * scaledControlPointArea;
        removeThreshold = ((float)(radius + removeRadius) + 0.5f) * scaledControlPointArea;
    }


    /* ------------------------------------------------------------------------------------ */
    /* ---------------------------- CREATE CONTROL POINTS --------------------------------- */
    /* ------------------------------------------------------------------------------------ */

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


    #endregion

    /* ------------------------------------------------------------------------------------ */
    /* ---------------------------- UPDATE CONTROL POINTS --------------------------------- */
    /* ------------------------------------------------------------------------------------ */

    #region UPDATE_CONTROL_POINTS

    private void updateControlPoints()
    {
        List<ControlPoint> toBeRemoved = new List<ControlPoint>(); 
        foreach(ControlPoint cp in controlPoints.Values)
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

        foreach(ControlPoint cp in toBeRemoved)
            controlPoints.Remove(cp.gridPosition);
    }


    #endregion

    /* ------------------------------------------------------------------------------------ */
    /* ---------------------------- DATA RECEIVED ----------------------------------------- */
    /* ------------------------------------------------------------------------------------ */

    #region DATA_RECEIVED

    /* ----------------------------------------------------------------------------------------- */
    public void onRoadDataReceived(Road.RoadData roadData)
    {
        Road r = null;

        if (roads.ContainsKey(roadData.key))
        {
            r = roads[roadData.key];
        }
        else
        {
            GameObject newRoadObj = roadsPoolManager.acquireObject(roadData.key);
            r = new Road(roadData.curve, roadData.key, newRoadObj, scale);
            roads.Add(roadData.key, r);
        }

        r.setMesh(roadData.meshData.createMesh());
    }

    #endregion

    #endregion
}
