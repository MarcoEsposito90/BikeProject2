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
    public static readonly string NUMBER_OF_LODS = "EndlessTerrainGenerator.NumberOfLods";

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

    [Range(2, 10)]
    public int radius;
    private float[] LODThresholds;

    public Transform viewer;
    private Vector2 latestViewerRecordedPosition;

    [Range(1, 20)]
    public int viewerDistanceUpdateFrequency;
    private float viewerDistanceUpdate;

    [Range(0, 1)]
    public float waterLevel;
    public GameObject mapSectorPrefab;

    private Dictionary<Vector2, MapSector> mapSectors;
    private PoolManager<Vector2> sectorsPoolManager;
    public BlockingQueue<MapSector.SectorData> sectorResultsQueue;
    public Queue<Vector2> removingSectors;
    private bool needUpdate;
    private bool start;

    public GameObject sectorsContainer;
    public MapGenerator mapGenerator;

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

        int multiply = radius;
        for (int i = 0; i < LODThresholds.Length; i++)
        {
            if (i > 3) multiply *= 2;
            LODThresholds[i] = (2.5f * scaledSectorSize + i * scaledSectorSize * multiply) / 2.0f;
        }

        mapSectors = new Dictionary<Vector2, MapSector>();
        int startSize = (int)LODThresholds[LODThresholds.Length - 1] * 2 / scaledSectorSize;
        sectorsPoolManager = new PoolManager<Vector2>(startSize, true, mapSectorPrefab, sectorsContainer);
        sectorResultsQueue = new BlockingQueue<MapSector.SectorData>();
        removingSectors = new Queue<Vector2>();
        needUpdate = false;
        start = true;

        GlobalInformation.Instance.addData(SECTOR_SIZE, sectorSize);
        GlobalInformation.Instance.addData(VIEWER, viewer);
        GlobalInformation.Instance.addData(SCALE, scale);
        GlobalInformation.Instance.addData(VIEWER_DIST_UPDATE, viewerDistanceUpdate);
        GlobalInformation.Instance.addData(WATER_LEVEL, waterLevel);
        GlobalInformation.Instance.addData(NUMBER_OF_LODS, NumberOfLods);
    }

    /* ----------------------------------------------------------------------------------------- */
    void Start()
    {
        // subscribe to event
        NoiseGenerator.Instance.OnSectorChanged += OnSectorChange;
        NoiseGenerator.Instance.OnSectorCreated += OnSectorCreate;

        float h = GlobalInformation.Instance.getHeight(new Vector2(0, 0));
        viewer.position = new Vector3(0, 1000, 0);
        latestViewerRecordedPosition = Vector2.zero;
        createNewSectors(Vector2.zero);
        //updateSectors(Vector2.zero);
    }


    /* ----------------------------------------------------------------------------------------- */
    void Update()
    {
        Vector2 pos = new Vector2(viewer.position.x, viewer.position.z);
        float distance = Vector2.Distance(latestViewerRecordedPosition, pos);

        while (!(removingSectors.Count == 0))
            removeSector(removingSectors.Dequeue());

        int count = 0;
        while (!sectorResultsQueue.isEmpty())
        {
            if (count > 10 && !start)
                break;
            MapSector.SectorData data = sectorResultsQueue.Dequeue();
            onSectorDisplayDataReceived(data);
            count++;
        }

        if (distance >= viewerDistanceUpdate || needUpdate)
        {
            latestViewerRecordedPosition = pos;
            updateMapAsynch(pos);
        }

        start = false;
    }


    /* ----------------------------------------------------------------------------------------- */
    private void updateMapAsynch(Vector2 viewerPosition)
    {
        ThreadStart ts = delegate
        {
            lock (mapSectors)
            {
                updateMap(viewerPosition);
            }
        };
        Thread t = new Thread(ts);
        t.Start();
    }


    private void updateMap(Vector2 viewerPosition)
    {
        updateSectors(viewerPosition);
        createNewSectors(viewerPosition);
        needUpdate = false;
    }

    #endregion


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- CREATE ------------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region CREATE

    public void createNewSectors(Vector2 viewerPosition)
    {

        float startX = viewerPosition.x - LODThresholds[LODThresholds.Length - 1];
        float endX = viewerPosition.x + LODThresholds[LODThresholds.Length - 1];
        float startY = viewerPosition.y - LODThresholds[LODThresholds.Length - 1];
        float endY = viewerPosition.y + LODThresholds[LODThresholds.Length - 1];
        int startGridX = Mathf.RoundToInt(startX / scaledSectorSize + 0.1f);
        int endGridX = Mathf.RoundToInt(endX / scaledSectorSize + 0.1f);
        int startGridY = Mathf.RoundToInt(startY / scaledSectorSize + 0.1f);
        int endGridY = Mathf.RoundToInt(endY / scaledSectorSize + 0.1f);
        Vector2 viewerPos = new Vector2(viewerPosition.x, viewerPosition.y);

        for (int x = startGridX; x <= endGridX; x++)
            for (int y = startGridY; y <= endGridY; y++)
            {
                Vector2 sectorPosition = new Vector2(x, y);
                if (mapSectors.ContainsKey(sectorPosition))
                    continue;

                Vector3 center = sectorPosition * scaledSectorSize;
                float dist = Vector2.Distance(viewerPos, center);

                if (dist < LODThresholds[LODThresholds.Length - 1])
                {
                    MapSector s = new MapSector(sectorPosition, sectorSize, scale);
                    mapSectors.Add(sectorPosition, s);
                    mapGenerator.GenerateMap(sectorPosition);
                }
            }
    }


    #endregion


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- UPDATING ----------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region CHUNK_UPDATING

    /* ----------------------------------------------------------------------------------------- */
    public void updateSectors(Vector2 viewerPosition)
    {
        List<MapSector> currentSectors = new List<MapSector>(mapSectors.Values);

        for (int index = 0; index < currentSectors.Count; index++)
        {
            MapSector sector = currentSectors[index];
            float distance = Vector2.Distance(
                new Vector2(viewerPosition.x, viewerPosition.y),
                sector.position * scaledSectorSize);

            if (distance >= LODThresholds[LODThresholds.Length - 1] * 1.1f)
                removingSectors.Enqueue(sector.position);
            else
                updateSector(sector, distance);
        }
    }


    /* ----------------------------------------------------------------------------------------- */
    private void updateSector(MapSector sector, float distance)
    {
        for (int i = 0; i < LODThresholds.Length; i++)
        {
            if (distance <= LODThresholds[i])
            {
                if (i != sector.currentLOD || sector.needRedraw)
                {
                    sector.latestLODRequest = i;
                    sector.needRedraw = false;
                    MapDisplay.Instance.createSectorDisplayData(sector.position, i);
                }
                return;
            }
        }
    }


    /* ----------------------------------------------------------------------------------------- */
    private void removeSector(Vector2 position)
    {
        if (!mapSectors.ContainsKey(position))
            return;

        MapSector sector = mapSectors[position];
        sector.resetPrefabObject();
        sectorsPoolManager.releaseObject(sector.position);
        mapSectors.Remove(sector.position);
        NoiseGenerator.Instance.removeNoiseMap(sector.position);
    }

    #endregion




    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- MAP DRAWING -------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region MAP_DRAW

    private void OnSectorChange(Vector2 position)
    {
        MapSector s;
        lock (mapSectors)
        {
            s = mapSectors[position];
            if (s == null)
                return;
        }

        s.needRedraw = true;
        s.resetMeshes();
        needUpdate = true;
    }


    /* ----------------------------------------------------------------------------------------- */
    private void OnSectorCreate(Vector2 position)
    {
        lock (mapSectors)
        {
            if (!mapSectors.ContainsKey(position))
                return;

            Vector2 center = position * scaledSectorSize;
            float distance = Vector2.Distance(center, latestViewerRecordedPosition);
            MapSector s = mapSectors[position];
            updateSector(s, distance);
        }
    }


    /* ----------------------------------------------------------------------------------------- */
    private void onSectorDisplayDataReceived(MapSector.SectorData sectorData)
    {
        MapSector sector = this[sectorData.sectorPosition];
        if (sector == null)
            return;

        if (sectorData.LOD != sector.latestLODRequest)
            return;

        // meshes -----------------------------------------
        Mesh mesh = sector.getMesh(sectorData.LOD, sectorData.meshData);
        mesh.name = "mesh" + sectorData.sectorPosition.ToString();

        Mesh colliderMesh = null;
        if (sectorData.colliderMeshData != null)
            colliderMesh = sector.getMesh(1, sectorData.colliderMeshData);

        // prefab -----------------------------------------
        if (sector.prefabObject == null)
        {
            GameObject newObj = sectorsPoolManager.acquireObject(sectorData.sectorPosition);
            sector.initializePrefabObject(newObj);
        }

        sector.setPrefabObject(colliderMesh, mesh, sectorData.colorMap);
        sector.currentLOD = sectorData.LOD;
    }

    #endregion

}
