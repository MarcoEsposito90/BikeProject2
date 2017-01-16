using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

public class EndlessTerrainGenerator : MonoBehaviour
{

    public static readonly string VIEWER_DIST_UPDATE = "EndlessTerrainGenerator.ViewerDistanceUpdate";
    public static readonly string VIEWER = "EndlessTerrainGenerator.Viewer";
    public static readonly string SCALE = "EndlessTerrainGenerator.Scale";
    public static readonly string SECTOR_SIZE = "EndlessTerrainGenerator.SectorSize";
    public static readonly string WATER_LEVEL = "EndlessTerrainGenerator.WaterLevel";


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- INSTANCE ----------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region INSTANCE

    private static EndlessTerrainGenerator _Instance;
    public static EndlessTerrainGenerator Instance
    {
        get
        {
            return _Instance;
        }
    }

    #endregion

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- ATTRIBUTES --------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region ATTRIBUTES

    [Range(1, 3)]
    public int sectorDimension;
    private int sectorSize;
    private int scaledSectorSize;

    [Range(1, 20)]
    public int scale;

    [Range(1, 8)]
    public int subdivisions;

    [Range(2, 10)]
    public int NumberOfLods;

    [Range(2, 6)]
    public int accuracy;
    private float[] LODThresholds;

    [Range(0, 2)]
    public int colliderAccuracy;

    public Transform viewer;
    private Vector2 latestViewerRecordedPosition;

    [Range(1, 20)]
    public int viewerDistanceUpdateFrequency;
    private float viewerDistanceUpdate;

    [Range(0, 1)]
    public float waterLevel;

    public Material terrainMaterial;
    public GameObject mapSectorPrefab;

    private Dictionary<Vector2, MapSector> mapSectors;
    private PoolManager<Vector2> sectorsPoolManager;
    public BlockingQueue<MapSector.SectorData> sectorResultsQueue;
    //public BlockingQueue<RedrawRequest> sectorRedrawRequests;
    public BlockingQueue<DrawRequest> drawRequests;

    public GameObject sectorsContainer;
    public MapGenerator mapGenerator;
    private bool start = true;

    #endregion

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- OPERATORS ---------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region OPERATORS

    public MapSector this[Vector2 key]
    {
        get
        {
            return mapSectors[key];
        }
        private set
        {
            mapSectors[key] = value;
        }
    }

    #endregion

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- UNITY CALLBACKS ---------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region UNITY

    void Awake()
    {
        _Instance = this;
        LODThresholds = new float[NumberOfLods];
        sectorSize = ((int)Mathf.Pow(2, sectorDimension) * 8);
        scaledSectorSize = sectorSize * scale;
        viewerDistanceUpdate = scaledSectorSize / (float)(viewerDistanceUpdateFrequency + 3);

        int multiply = accuracy;
        for (int i = 0; i < LODThresholds.Length; i++)
        {
            if (i > 3) multiply *= 2;
            LODThresholds[i] = (2.5f * scaledSectorSize + i * scaledSectorSize * multiply) / 2.0f;
        }

        mapSectors = new Dictionary<Vector2, MapSector>();
        int startSize = (int)LODThresholds[LODThresholds.Length - 1] * 2 / scaledSectorSize;
        sectorsPoolManager = new PoolManager<Vector2>(startSize, true, mapSectorPrefab, sectorsContainer);
        sectorResultsQueue = new BlockingQueue<MapSector.SectorData>();
        drawRequests = new BlockingQueue<DrawRequest>();
        //sectorRedrawRequests = new BlockingQueue<RedrawRequest>();

        GlobalInformation.Instance.addData(SECTOR_SIZE, sectorSize);
        GlobalInformation.Instance.addData(VIEWER, viewer);
        GlobalInformation.Instance.addData(SCALE, scale);
        GlobalInformation.Instance.addData(VIEWER_DIST_UPDATE, viewerDistanceUpdate);
        GlobalInformation.Instance.addData(WATER_LEVEL, waterLevel);
    }

    /* ----------------------------------------------------------------------------------------- */
    void Start()
    {
        Vector3 startPos = new Vector3(0, viewer.position.y, 0);
        viewer.position = startPos;

        ThreadStart ts = delegate
        {
            createNewSectors(startPos);
            updateSectors(startPos);
        };
        Thread t = new Thread(ts);
        t.Start();
    }


    /* ----------------------------------------------------------------------------------------- */
    void Update()
    {
        Vector3 position = viewer.position;
        Vector2 pos = new Vector2(viewer.position.x, viewer.position.z);
        float distance = Vector2.Distance(latestViewerRecordedPosition, pos);

        while (!sectorResultsQueue.isEmpty())
        {
            MapSector.SectorData data = sectorResultsQueue.Dequeue();
            onSectorDataReceived(data);
        }

        //while (!drawRequests.isEmpty())
        //{
        //    DrawRequest r = drawRequests.Dequeue();
        //    if (this[r.gridPosition] != null)
        //        updateSector(this[r.gridPosition]);
        //}

        
        if (distance >= viewerDistanceUpdate)
        {
            ThreadStart ts = delegate
            {
                updateSectors(position);
                createNewSectors(position);
            };
            Thread t = new Thread(ts);
            t.Start();

            latestViewerRecordedPosition = pos;
        }

        start = false;
    }


    #endregion

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- UPDATING ----------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region CHUNK_UPDATING

    // checks the list of chunks for updates ------------------------------------------------------
    public void updateSectors(Vector3 position)
    {
        List<MapSector> currentSectors = new List<MapSector>(mapSectors.Values);

        for (int index = 0; index < currentSectors.Count; index++)
        {
            MapSector sector = currentSectors[index];
            float dist = updateSector(sector, position);

            // check if the object should be removed ---------
            if (dist >= LODThresholds[LODThresholds.Length - 1] * 1.1f)
            {
                sector.resetPrefabObject();
                sectorsPoolManager.releaseObject(sector.position);
                mapSectors.Remove(sector.position);
            }
        }
    }


    // updates a single sector to the right LOD --------------------------------------------------
    private float updateSector(MapSector sector, Vector3 position)
    {
        float distance = Vector2.Distance(new Vector2(position.x, position.z), sector.position * scaledSectorSize);

        for (int i = 0; i < LODThresholds.Length; i++)
        {
            if (distance < LODThresholds[i])
            {
                float[,] heightMap = NoiseGenerator.Instance[sector.position];
                MapDisplay.Instance.getSectorData(sector.position, heightMap, i, i == 0, colliderAccuracy);
                break;
            }
        }

        return distance;
    }


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- CREATE ------------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    public void createNewSectors(Vector3 position)
    {
        float startX = position.x - LODThresholds[LODThresholds.Length - 1];
        float endX = position.x + LODThresholds[LODThresholds.Length - 1];
        float startY = position.z - LODThresholds[LODThresholds.Length - 1];
        float endY = position.z + LODThresholds[LODThresholds.Length - 1];
        int startGridX = Mathf.RoundToInt(startX / scaledSectorSize + 0.1f);
        int endGridX = Mathf.RoundToInt(endX / scaledSectorSize + 0.1f);
        int startGridY = Mathf.RoundToInt(startY / scaledSectorSize + 0.1f);
        int endGridY = Mathf.RoundToInt(endY / scaledSectorSize + 0.1f);
        Vector2 viewerPos = new Vector2(position.x, position.z);

        for (int x = startGridX; x <= endGridX; x++)
            for (int y = startGridY; y <= endGridY; y++)
            {
                Vector2 sectorPosition = new Vector2(x, y);
                if (mapSectors.ContainsKey(sectorPosition))
                    continue;

                Vector3 center = sectorPosition * scaledSectorSize;
                float dist = Vector2.Distance(viewerPos, center);

                if(dist < LODThresholds[LODThresholds.Length - 1])
                {
                    MapSector s = new MapSector(sectorPosition, sectorSize, scale);
                    this[sectorPosition] = s;
                    mapGenerator.GenerateMap(sectorPosition);
                }
            }
    }

    #endregion


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- MAP DRAWING -------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    public void onSectorDataReceived(MapSector.SectorData sectorData)
    {
        MapSector sector = this[sectorData.sectorPosition];
        if (sector == null)
            return;

        // store the mesh inside the object --------------------------------
        Mesh mesh = null;
        mesh = sectorData.meshData.createMesh();
        mesh.name = "mesh" + sectorData.sectorPosition.ToString();

        // get the collider mesh --------------------------------------------
        Mesh colliderMesh = null;
        if (sectorData.colliderMeshData != null)
            colliderMesh = sectorData.colliderMeshData.createMesh();

        if(sector.prefabObject == null)
        {
            GameObject newObj = sectorsPoolManager.acquireObject(sectorData.sectorPosition);
            sector.initializePrefabObject(newObj);
        }

        sector.setPrefabObject(colliderMesh, mesh, sectorData.colorMap);
        //sector.prefabObject.transform.localScale = new Vector3(scale, scale, scale);
        //sector.currentLOD = sectorData.meshData.LOD;
    }


    /* ----------------------------------------------------------------------------------------- */
    //private void OnRedrawRequestReceived(RedrawRequest request)
    //{
    //    Dictionary<Vector2, int> toRedraw = new Dictionary<Vector2, int>();

    //    float X = request.worldPosition.x / (float)scale;
    //    float Y = request.worldPosition.y / (float)scale;
    //    int radius = Mathf.RoundToInt(request.radius / (float)scale);
    //    float n = NoiseGenerator.Instance.getNoiseValue(1, X, Y);

    //    for (int i = -1; i <= 1; i++)
    //        for (int j = -1; j <= 1; j++)
    //        {
    //            float a = request.worldPosition.x + i * request.radius * 2.0f;
    //            float b = request.worldPosition.y + j * request.radius * 2.0f;
    //            a /= scaledSectorSize;
    //            b /= scaledSectorSize;
    //            int gridX = Mathf.RoundToInt(a);
    //            int gridY = Mathf.RoundToInt(b);

    //            Vector2 gridPos = new Vector2(gridX, gridY);
    //            if (!toRedraw.ContainsKey(gridPos))
    //                toRedraw.Add(gridPos, 0);

    //        }

    //    foreach (Vector2 v in toRedraw.Keys)
    //    {
    //        MapSector sector = mapSectors[v];
    //        ThreadStart ts = delegate
    //        {
    //            int centerX = (int)((X - (sector.position.x - 0.5f) * sectorSize));
    //            int centerY = (int)(((sector.position.y + 0.5f) * sectorSize - Y));

    //            lock (sector)
    //            {
    //                sector.heightMap = ImageProcessing.radialFlattening(
    //                sector.heightMap,
    //                radius,
    //                centerX + 1,
    //                centerY + 1,
    //                n);

    //                sector.needRedraw = true;
    //            }
    //        };

    //        Thread t = new Thread(ts);
    //        t.Start();
    //    }
    //}


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- SUBCLASSES --------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region REDRAW_REQUEST

    public class DrawRequest
    {
        public readonly Vector2 gridPosition;
        public readonly float[,] heightMap;

        public DrawRequest(Vector2 worldPosition, float[,] heightMap)
        {
            this.gridPosition = worldPosition;
            this.heightMap = heightMap;
        }
    }

    #endregion

}
