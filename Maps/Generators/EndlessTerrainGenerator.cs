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

    public Material terrainMaterial;
    public GameObject mapSectorPrefab;

    private Dictionary<Vector2, MapSector> mapSectors;
    private PoolManager<Vector2> sectorsPoolManager;
    public BlockingQueue<MapSector.SectorData> sectorResultsQueue;

    public GameObject sectorsContainer;
    public MapGenerator mapGenerator;
    public EndlessRoadsGenerator roadsGenerator;
    private bool start = true;

    #endregion

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- UNITY CALLBACKS ---------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region UNITY

    void Awake()
    {
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
        //removeThreshold = LODThresholds[LODThresholds.Length - 1] * keepUnvisible;

        mapSectors = new Dictionary<Vector2, MapSector>();
        int startSize = (int)LODThresholds[LODThresholds.Length - 1] * 2 / scaledSectorSize;
        sectorsPoolManager = new PoolManager<Vector2>(startSize, true, mapSectorPrefab, sectorsContainer);
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
        createNewSectors(viewer.position);
        updateSectors();
        //StartCoroutine(checkForNewSectors());

        //StartCoroutine(checkForUpdates());
        //checkForUpdates();
    }


    /* ----------------------------------------------------------------------------------------- */
    void Update()
    {
        int counter = 0;
        while (!sectorResultsQueue.isEmpty() && counter < 1)
        {
            if (!start) counter++;
            MapSector.SectorData data = sectorResultsQueue.Dequeue();
            onSectorDataReceived(data);
        }


        Vector2 pos = new Vector2(viewer.position.x, viewer.position.z);
        float distance = Vector2.Distance(latestViewerRecordedPosition, pos);
        if (distance >= viewerDistanceUpdate)
        {
            Vector3 position = viewer.position;
            ThreadStart ts = delegate { createNewSectors(position); };
            Thread t = new Thread(ts);
            t.Start();
            updateSectors();
            latestViewerRecordedPosition = pos;
        }

        start = false;
    }

    /* ----------------------------------------------------------------------------------------- */
    //IEnumerator checkForUpdates()
    //{
    //    while(true)
    //    {
    //        Debug.Log("update cycle");
    //        Vector2 pos = new Vector2(viewer.position.x, viewer.position.z);
    //        float distance = Vector2.Distance(latestViewerRecordedPosition, pos);
    //        if (distance >= viewerDistanceUpdate)
    //        {
    //            Vector3 position = viewer.position;
    //            ThreadStart ts = delegate { createNewSectors(position); };
    //            Thread t = new Thread(ts);
    //            t.Start();
    //            updateSectors();
    //            latestViewerRecordedPosition = pos;
    //        }
    //        yield return new WaitForSeconds(1);
    //    }
    //}

    //IEnumerator checkForNewSectors()
    //{
    //    while (true)
    //    {
    //        int counter = 0;
    //        while (!sectorResultsQueue.isEmpty() && counter < 10)
    //        {
    //            if (!start) counter++;
    //            MapSector.SectorData data = sectorResultsQueue.Dequeue();
    //            onSectorDataReceived(data);
    //            yield return new WaitForSeconds(0.1f);
    //        }

    //        yield return new WaitForSeconds(1);
    //    }
    //}

    #endregion

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- CHUNK UPDATING ----------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region CHUNK_UPDATING

    /* ----------------------------------------------------------------------------------------- */
    public void createSector(Vector2 sectorPosition, int LOD)
    {
        mapGenerator.requestSectorData
            (sectorPosition,
            LOD,
            LOD == 0,
            colliderAccuracy);
    }

    
    // checks the list of chunks for updates ------------------------------------------------------
    public void updateSectors()
    {
        List<MapSector> currentSectors = new List<MapSector>(mapSectors.Values);

        for (int index = 0; index < currentSectors.Count; index++)
        {
            MapSector sector = currentSectors[index];
            float dist = Vector2.Distance(
                new Vector2(viewer.position.x, viewer.position.z),
                sector.position * scaledSectorSize);

            // check which is the correct load ---------------
            for (int i = 0; i < LODThresholds.Length; i++)
            {
                if (dist < LODThresholds[i])
                {
                    updateSector(sector, i);
                    break;
                }
            }

            // check if the object should be removed ---------
            if (dist >= LODThresholds[LODThresholds.Length - 1] * 1.1f)
            {
                //Debug.Log("must destroy chunk " + chunk.position);
                sector.resetPrefabObject();
                sectorsPoolManager.releaseObject(sector.position);
                mapSectors.Remove(sector.position);
            }
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
            Mesh collider = null;
            if (LOD == 0 && sector.meshes[colliderAccuracy] != null)
                collider = sector.meshes[colliderAccuracy];

            sector.updateMeshes(collider, sector.meshes[LOD]);
            sector.currentLOD = LOD;
            return;
        }

        mapGenerator.requestSectorData
            (sector.position,
            LOD,
            LOD == 0,
            colliderAccuracy);
    }


    // creates chunks --------------------------------------------------------------
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

                //float realChunkX = x * scaledChunkSize;
                //float realChunkY = y * scaledChunkSize;

                Vector3 center = sectorPosition * scaledSectorSize;
                float dist = Vector2.Distance(viewerPos, center);

                for (int i = 0; i < LODThresholds.Length; i++)
                {
                    if (dist < LODThresholds[i])
                    {
                        //Debug.Log("lod " + i);
                        createSector(sectorPosition, i);
                        break;
                    }
                }

            }
    }

    #endregion


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- MAP DRAWING -------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    public void onSectorDataReceived(MapSector.SectorData sectorData)
    {
        MapSector sector = null;

        if (mapSectors.ContainsKey(sectorData.sectorPosition))
            sector = mapSectors[sectorData.sectorPosition];
        else
        {
            GameObject newSectorObj = sectorsPoolManager.acquireObject(sectorData.sectorPosition);
            sector = new MapSector(
                sectorData.sectorPosition,
                sectorSize,
                scale,
                subdivisions,
                NumberOfLods,
                newSectorObj);

            sector.latestLODRequest = sectorData.meshData.LOD;
            mapSectors.Add(sectorData.sectorPosition, sector);
        }

        // store the mesh inside the object --------------------------------
        Mesh mesh = null;
        if (sector.meshes[sectorData.meshData.LOD] == null)
        {
            mesh = sectorData.meshData.createMesh();
            mesh.name = "mesh" + sectorData.sectorPosition.ToString();
            sector.meshes[sectorData.meshData.LOD] = mesh;
        }
        else
            mesh = sector.meshes[sectorData.meshData.LOD];

        // get the collider mesh --------------------------------------------
        Mesh colliderMesh = null;
        if (sectorData.colliderMeshData != null)
        {
            if (sector.meshes[sectorData.colliderMeshData.LOD] == null)
            {
                colliderMesh = sectorData.colliderMeshData.createMesh();
                sector.meshes[sectorData.colliderMeshData.LOD] = colliderMesh;
            }
            else
                colliderMesh = sector.meshes[sectorData.colliderMeshData.LOD];
        }

        /* ----------------------------------------------------------------- 
            means we came back to a previous LOD before the response of this
            LOD was provided. We have already set the mesh for the current LOD,
            so we just store the new mesh for future use and do not change
            any setting
        */

        if (sector.latestLODRequest != sectorData.meshData.LOD)
            return;

        sector.setPrefabObject(colliderMesh, mesh, sectorData.colorMap, null);
        sector.prefabObject.transform.localScale = new Vector3(scale, scale, scale);
        sector.currentLOD = sectorData.meshData.LOD;
    }

}
