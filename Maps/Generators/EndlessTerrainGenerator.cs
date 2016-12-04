using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EndlessTerrainGenerator : MonoBehaviour
{

    public static readonly string VIEWER_DIST_UPDATE = "EndlessTerrainGenerator.ViewerDistanceUpdate";
    public static readonly string VIEWER = "EndlessTerrainGenerator.Viewer";
    public static readonly string SCALE = "EndlessTerrainGenerator.Scale";
    public static readonly string SECTOR_SIZE = "EndlessTerrainGenerator.SectorSize";


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- ATTRIBUTES --------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region ATTRIBUTES

    [Range(1, 3)]
    public int sectorDimension;
    private int sectorSize;
    private int scaledChunkSize;

    [Range(1, 20)]
    public int scale;

    [Range(1, 8)]
    public int subdivisions;

    [Range(2, 10)]
    public int NumberOfLods;

    [Range(2, 6)]
    public int accuracy;
    private float[] LODThresholds;

    [Range(1, 3)]
    public int keepUnvisible;
    private float removeThreshold;

    [Range(0, 2)]
    public int colliderAccuracy;

    public Transform viewer;
    private Vector2 latestViewerRecordedPosition;

    [Range(1, 20)]
    public int viewerDistanceUpdateFrequency;
    private float viewerDistanceUpdate;

    public Material terrainMaterial;
    public GameObject mapSectorPrefab;
    //public GameObject roadPrefab;
    //public GameObject roadsContainer;

    private Dictionary<Vector2, MapSector> mapSectors;
    private PoolManager<Vector2> sectorsPoolManager;
    public BlockingQueue<MapSector.SectorData> sectorResultsQueue;

    //private Dictionary<Road.Key, Road> roads;
    //private PoolManager<Road.Key> roadsPoolManager;

    public GameObject sectorsContainer;
    public MapGenerator mapGenerator;
    public EndlessRoadsGenerator roadsGenerator;
    //public RoadsGenerator roadsGenerator;
    private float seedX, seedY;


    #endregion

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- UNITY CALLBACKS ---------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region UNITY

    void Awake()
    {
        initialize();
    }

    /* ----------------------------------------------------------------------------------------- */
    public void initialize()
    {
        LODThresholds = new float[NumberOfLods];
        sectorSize = ((int)Mathf.Pow(2, sectorDimension) * 8);
        scaledChunkSize = sectorSize * scale;
        viewerDistanceUpdate = scaledChunkSize / (float)(viewerDistanceUpdateFrequency + 3);

        int multiply = accuracy;
        for (int i = 0; i < LODThresholds.Length; i++)
        {
            if (i > 3) multiply *= 2;
            LODThresholds[i] = (1.5f * scaledChunkSize + i * scaledChunkSize * multiply) / 2.0f;
        }
        removeThreshold = LODThresholds[LODThresholds.Length - 1] * keepUnvisible;

        mapSectors = new Dictionary<Vector2, MapSector>();
        sectorsPoolManager = new PoolManager<Vector2>(50, true, mapSectorPrefab, sectorsContainer);
        sectorResultsQueue = new BlockingQueue<MapSector.SectorData>();

        GlobalInformation.Instance.addData(SECTOR_SIZE, sectorSize);
        GlobalInformation.Instance.addData(VIEWER, viewer);
        GlobalInformation.Instance.addData(SCALE, scale);
        GlobalInformation.Instance.addData(VIEWER_DIST_UPDATE, viewerDistanceUpdate);
    }


    /* ----------------------------------------------------------------------------------------- */
    void Start()
    {
        viewer.position = new Vector3(0, viewer.position.y, 0);
        createNewSectors();
        updateSectors();
    }


    /* ----------------------------------------------------------------------------------------- */
    void Update()
    {
        while (!sectorResultsQueue.isEmpty())
        {
            MapSector.SectorData data = sectorResultsQueue.Dequeue();
            onSectorDataReceived(data);
        }

        Vector2 pos = new Vector2(viewer.position.x, viewer.position.z);
        float distance = Vector2.Distance(latestViewerRecordedPosition, pos);
        if (distance >= viewerDistanceUpdate)
        {
            createNewSectors();
            updateSectors();
            latestViewerRecordedPosition = pos;
        }
    }

    #endregion

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- CHUNK UPDATING ----------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region CHUNK_UPDATING

    /* ----------------------------------------------------------------------------------------- */
    public void createSector(int x, int y)
    {
        Vector2 sectorCenter = new Vector2(x, y);
        GameObject newSectorObj = sectorsPoolManager.acquireObject(sectorCenter);
        MapSector newSector = new MapSector(
                                x,
                                y,
                                sectorSize,
                                scale,
                                subdivisions,
                                NumberOfLods,
                                newSectorObj);

        mapSectors.Add(sectorCenter, newSector);
        newSector.currentLOD = LODThresholds.Length - 1;

        mapGenerator.requestSectorData
            (newSector,
            LODThresholds.Length - 1,
            false,
            -1);
    }


    // checks the list of chunks for updates ------------------------------------------------------
    public void updateSectors()
    {
        List<Vector2> toBeRemoved = new List<Vector2>();

        foreach (MapSector sector in mapSectors.Values)
        {
            float dist = sector.bounds.SqrDistance(new Vector3(viewer.position.x, 0, viewer.position.z));
            dist = Mathf.Sqrt(dist);
            bool visible = false;

            // check which is the correct load ---------------
            for (int i = 0; i < LODThresholds.Length; i++)
            {
                if (dist < LODThresholds[i])
                {
                    updateSector(sector, i);
                    visible = true;
                    break;
                }
            }

            // check if the object should be removed ---------
            if (!visible && dist >= removeThreshold)
            {
                //Debug.Log("must destroy chunk " + chunk.position);
                sector.resetPrefabObject();
                sectorsPoolManager.releaseObject(sector.position);
                toBeRemoved.Add(sector.position);
            }
            else
                sector.setVisible(visible);
        }

        // delete chunk references from map --------------
        foreach (Vector2 pos in toBeRemoved)
        {
            mapSectors.Remove(pos);
            //Debug.Log("TerrainChunks = " + TerrainChunks.Count);
        }
    }


    // updates a single chunk to the specified LOD --------------------------------------------------
    private void updateSector(MapSector sector, int LOD)
    {
        if (sector.latestLODRequest == LOD)
            return;

        sector.latestLODRequest = LOD;

        if (sector.meshes[LOD] != null)
        {
            //Debug.Log("chunk " + chunk.position + " with mesh " + LOD + "available");
            Mesh collider = null;
            if (LOD == 0 && sector.meshes[colliderAccuracy] != null)
                collider = sector.meshes[colliderAccuracy];

            sector.updateMeshes(collider, sector.meshes[LOD]);
            sector.currentLOD = LOD;
            return;
        }

        bool colliderRequested = LOD == 0 ? true : false;
        mapGenerator.requestSectorData
            (sector,
            LOD,
            colliderRequested,
            colliderAccuracy);
    }


    // creates chunks --------------------------------------------------------------
    public void createNewSectors()
    {
        Vector3 position = viewer.position;
        float startX = position.x - LODThresholds[LODThresholds.Length - 1];
        float endX = position.x + LODThresholds[LODThresholds.Length - 1];
        float startY = position.z - LODThresholds[LODThresholds.Length - 1];
        float endY = position.z + LODThresholds[LODThresholds.Length - 1];

        for (float x = startX; x <= endX; x += scaledChunkSize)
            for (float y = startY; y <= endY; y += scaledChunkSize)
            {
                int chunkX = Mathf.RoundToInt(x / (float)scaledChunkSize + 0.1f);
                int chunkY = Mathf.RoundToInt(y / (float)scaledChunkSize + 0.1f);

                Vector2 chunkCenter = new Vector2(chunkX, chunkY);
                if (mapSectors.ContainsKey(chunkCenter))
                    continue;

                float realChunkX = chunkX * scaledChunkSize;
                float realChunkY = chunkY * scaledChunkSize;

                Vector3 center = new Vector3(realChunkX, 0, realChunkY);
                Vector3 sizes = new Vector3(scaledChunkSize, scaledChunkSize, scaledChunkSize);
                Bounds b = new Bounds(center, sizes);
                float dist = Mathf.Sqrt(b.SqrDistance(position));

                if (dist < LODThresholds[LODThresholds.Length - 1])
                    createSector(chunkX, chunkY);
            }
    }

    #endregion


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- MAP DRAWING -------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    public void onSectorDataReceived(MapSector.SectorData sectorData)
    {
        //Debug.Log(sectorData.sectorPosition + " data received");
        MapSector sector = null;
        mapSectors.TryGetValue(sectorData.sectorPosition, out sector);

        if (sector == null)
        {
            Debug.Log("ATTENTION! trying to set data on null chunk");
            return;
        }

        // store the mesh inside the object --------------------------------
        Mesh mesh = sectorData.meshData.createMesh();
        mesh.name = "mesh" + sectorData.sectorPosition.ToString();
        sector.meshes[sectorData.meshData.LOD] = mesh;

        Mesh colliderMesh = null;
        if (sectorData.colliderMeshData != null)
        {
            colliderMesh = sectorData.colliderMeshData.createMesh();
            if (sector.meshes[sectorData.colliderMeshData.LOD] == null)
                sector.meshes[sectorData.colliderMeshData.LOD] = colliderMesh;
        }


        /* ----------------------------------------------------------------- 
            means we came back to a previous LOD before the response of this
            LOD was provided. We have already set the mesh for the current LOD,
            so we just store the new mesh for future use and do not change
            any setting
        */

        if (sector.latestLODRequest != sectorData.meshData.LOD)
        {
            //Debug.Log("chunk " + chunk.position + " not updated due to deceased request");
            return;
        }

        List<Color[]> alphaMaps = new List<Color[]>();
        alphaMaps.Add(sectorData.alphaMap);
        alphaMaps.Add(sectorData.alphaMap);
        sector.setPrefabObject(colliderMesh, mesh, sectorData.colorMap, alphaMaps);

        // scaling -----------------------------------------------------------
        sector.prefabObject.transform.localScale = new Vector3(scale, scale, scale);
        sector.currentLOD = sectorData.meshData.LOD;
    }


    /* ----------------------------------------------------------------------------------------- */
    //public void onRoadDataReceived(Road.RoadData roadData)
    //{
    //    Road r = null;
    //    //Debug.Log("roadData received for " + roadData.sector.position);
    //    Road.Key inverseKey = new Road.Key(roadData.key.end, roadData.key.start);
    //    //Debug.Log("key = " + roadData.key.start + " - " + roadData.key.end + " | inverse = " + inverseKey.start + " - " + inverseKey.end);

    //    if (roads.ContainsKey(roadData.key) || roads.ContainsKey(inverseKey))
    //    {
    //        Debug.Log("road exists");
    //        if (!roads.TryGetValue(roadData.key, out r))
    //        {
    //            Debug.Log("road inv exists");
    //            roads.TryGetValue(inverseKey, out r);
    //        }
    //    }
    //    else
    //    {
    //        Debug.Log("road is new");
    //        GameObject newRoadObj = roadsPoolManager.acquireObject(roadData.key);
    //        r = new Road(roadData.curve, newRoadObj, scale);
    //        roads.Add(roadData.key, r);
    //    }

    //    r.setMesh(roadData.meshData.createMesh());
    //}

}
