using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class EndlessRoadsGenerator : MonoBehaviour
{
    public static readonly string MAP_SEEDX = "EndlessRoadsGenerator.SeedX";
    public static readonly string MAP_SEEDY = "EndlessRoadsGenerator.SeedY";
    public static readonly string CP_AREA = "EndlessRoadsGenerator.ControlPointArea";

    /* ----------------------------------------------------------------------------------------- */
    /* ---------------------------- ATTRIBUTES ------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    public const int DENSITY_ONE = 5;

    #region ATTRIBUTES

    [Range(1, DENSITY_ONE + 1)]
    public int controlPointsDensity;
    public float controlPointArea { get; private set; }
    public float scaledControlPointArea { get; private set; }

    [Range(0, 1000)]
    public int seed;

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
    private Vector2 latestViewerRecordedPosition;
    private float viewerDistanceUpdate;
    private float seedX, seedY;
    //private float[] LODThresholds;

    private Dictionary<Vector2, ControlPoint> controlPoints;
    private Dictionary<Graph<Vector2, ControlPoint>.Link, Road> roads;
    private PoolManager<Graph<Vector2, ControlPoint>.Link> roadsPoolManager;
    private PoolManager<Vector2> controlPointsPoolManager;
    public BlockingQueue<Road.RoadData> roadsResultsQueue { get; private set; }
    public BlockingQueue<ControlPoint.ControlPointData> cpsResultsQueue { get; private set; }
    public BlockingQueue<Graph<Vector2, ControlPoint>.Link> roadsRemoveQueue { get; private set; }
    //public BlockingQueue<Vector2> roadsSplitRequests { get; private set; }

    #endregion


    /* ----------------------------------------------------------------------------------------- */
    /* ---------------------------- UNITY ------------------------------------------------------ */
    /* ----------------------------------------------------------------------------------------- */

    #region UNITY

    /* ----------------------------------------------------------------------------------------- */
    void Awake()
    {
        System.Random random = new System.Random(seed);
        seedX = ((float)random.NextDouble()) * random.Next(100);
        seedY = ((float)random.NextDouble()) * random.Next(100);
        GlobalInformation.Instance.addData(MAP_SEEDX, seedX);
        GlobalInformation.Instance.addData(MAP_SEEDY, seedY);

        controlPoints = new Dictionary<Vector2, ControlPoint>();
        roads = new Dictionary<Graph<Vector2, ControlPoint>.Link, Road>();
        roadsResultsQueue = new BlockingQueue<Road.RoadData>();
        cpsResultsQueue = new BlockingQueue<ControlPoint.ControlPointData>();
        roadsRemoveQueue = new BlockingQueue<Graph<Vector2, ControlPoint>.Link>();
        //roadsSplitRequests = new BlockingQueue<Vector2>();

        roadsPoolManager = new PoolManager<Graph<Vector2, ControlPoint>.Link>(10, true, roadPrefab, roadsContainer);
        controlPointsPoolManager = new PoolManager<Vector2>(400, true, controlPointPrefab, controlPointsContainer);
    }


    /* ----------------------------------------------------------------------------------------- */
    void Start()
    {
        sectorSize = (int)GlobalInformation.Instance.getData(EndlessTerrainGenerator.SECTOR_SIZE);
        scale = (int)GlobalInformation.Instance.getData(EndlessTerrainGenerator.SCALE);
        viewerDistanceUpdate = (float)GlobalInformation.Instance.getData(EndlessTerrainGenerator.VIEWER_DIST_UPDATE);
        viewer = (Transform)GlobalInformation.Instance.getData(EndlessTerrainGenerator.VIEWER);

        float denom = (DENSITY_ONE + 1) - controlPointsDensity;
        if (denom <= 0) denom = 1.0f / Mathf.Pow(2, -denom + 1);
        float multiplier = 1.0f / denom;
        controlPointArea = (float)sectorSize / multiplier;
        scaledControlPointArea = controlPointArea * scale;
        threshold = ((float)radius + 0.5f) * scaledControlPointArea;
        removeThreshold = ((float)(radius + removeRadius) + 0.5f) * scaledControlPointArea;

        GlobalInformation.Instance.addData(CP_AREA, controlPointArea);
        createControlPoints();
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

        while (!roadsRemoveQueue.isEmpty())
        {
            Graph<Vector2, ControlPoint>.Link l = roadsRemoveQueue.Dequeue();
            onRoadRemove(l);
        }


        Vector2 pos = new Vector2(viewer.position.x, viewer.position.z);
        float distance = Vector3.Distance(latestViewerRecordedPosition, pos);
        if (distance >= viewerDistanceUpdate)
        {
            createControlPoints();
            latestViewerRecordedPosition = pos;
        }

    }



    #endregion


    /* ----------------------------------------------------------------------------------------- */
    /* ---------------------------- METHODS ---------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region METHODS


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
        {
            controlPoints.Remove(cp.gridPosition);
            roadsGenerator.removeControlPoint(cp);
        }
    }


    #endregion

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
            Debug.Log("updating road");
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

        Mesh mesh = roadData.meshData.createMesh();
        mesh.name = "road " + roadData.key.from.item.position + " - " + roadData.key.to.item.position;
        r.setMesh(mesh, roadData.texture);
    }


    /* ----------------------------------------------------------------------------------------- */
    private void onCpDataReceived(ControlPoint.ControlPointData data)
    {
        if (!controlPoints.ContainsKey(data.gridPosition))
            return;

        ControlPoint cp = controlPoints[data.gridPosition];
        cp.setData(data);
    }

    
    /* ----------------------------------------------------------------------------------------- */
    private void onRoadRemove(Graph<Vector2, ControlPoint>.Link link)
    {
        if (!roads.ContainsKey(link))
            return;

        Road r = roads[link];
        r.resetPrefab();
        roadsPoolManager.releaseObject(link);
        roads.Remove(link);
        r = null;
    }


    /* ----------------------------------------------------------------------------------------- */
    public void splitRequest(Vector2 point)
    {
        Vector2 gridPos = point / scaledControlPointArea;
        GameObject prefab = controlPointsPoolManager.acquireObject(gridPos);
        ControlPoint cp = new ControlPoint(gridPos, prefab, controlPointArea, scale);
        //roadsGenerator.requestSplit(point);
        controlPoints.Add(gridPos, cp);
        roadsGenerator.sendNewControlPoint(cp);
    }

    #endregion

}
