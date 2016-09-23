using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EndlessTerrainGenerator : MonoBehaviour {


    [Range(1, 7)]
    public int chunkDimension;
    private int chunkSize;

    [Range(1,3)]
    public int accuracy;
    private float[] LODThresholds;

    [Range(0, 2)]
    public int colliderAccuracy;

    public Transform viewer;
    private Vector3 latestViewerRecordedPosition;

    [Range(1,10)]
    public int viewerDistanceUpdateFrequency;
    private float viewerDistanceUpdate;

    public Material terrainMaterial;
    public bool autoUpdate;

    private Dictionary<Vector2,MapChunk> TerrainChunks;
    private MapGenerator mapGenerator;

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- MY CLASSES --------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    public class MapChunk
    {
        public Vector2 position;
        public int size;
        public GameObject mapChunkObject;
        public Bounds bounds;
        public int currentLOD;
        public int latestLODRequest;
        public bool isVisible;
        public Mesh[] meshes;

        public MapChunk(int x, int y, int size)
        {
            this.position = new Vector2(x, y);
            this.size = size;
            currentLOD = -1;
            latestLODRequest = -1;
            isVisible = true;
            bounds = new Bounds(new Vector3(x*size, 0, y*size), new Vector3(size,size, size));
            meshes = new Mesh[MapDisplay.NUMBER_OF_LODS];

            mapChunkObject = new GameObject("chunk (" + x  + "," + y + ")");
            mapChunkObject.AddComponent<MeshFilter>();
            mapChunkObject.AddComponent<MeshRenderer>();
            mapChunkObject.AddComponent<MeshCollider>();
            mapChunkObject.transform.position = new Vector3(x * size, 0, y * size);
        }

        public void setVisible(bool visibility)
        {
            if (isVisible == visibility)
                return;

            isVisible = visibility;
            mapChunkObject.SetActive(visibility);
        }
    }

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- UNITY CALLBACKS ---------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    void Start()
    {
        initialize();

        viewer.position = new Vector3(0, viewer.position.y, 0);
        createNewChunks();
        updateChunks();
    }

   
    /* ----------------------------------------------------------------------------------------- */
    void Update()
    {
        float distance = Vector3.Distance(latestViewerRecordedPosition, viewer.position);

        if (distance >= viewerDistanceUpdate)
        {
            createNewChunks();
            updateChunks();
            latestViewerRecordedPosition = viewer.position;
        }

    }


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- CHUNK UPDATING ----------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    // checks the list of chunks for updates ------------------------------------------------------
    public void updateChunks()
    {
        foreach (MapChunk chunk in TerrainChunks.Values)
        {
            float dist = chunk.bounds.SqrDistance(new Vector3(viewer.position.x, 0, viewer.position.z));
            dist = Mathf.Sqrt(dist);
            bool visible = false;

            for (int i = 0; i < LODThresholds.Length; i++)
            {
                if (dist < LODThresholds[i])
                {
                    updateChunk(chunk, i);
                    visible = true;
                    break;
                }
            }

            chunk.setVisible(visible);
        }
    }


    // updates a single chunk to the specified LOD --------------------------------------------------
    private void updateChunk(MapChunk chunk, int LOD)
    {
        if (chunk.latestLODRequest == LOD)
            return;

        chunk.latestLODRequest = LOD;

        if(chunk.meshes[LOD] != null)
        {
            Debug.Log("chunk " + chunk.position + " with mesh " + LOD + "available");
            chunk.mapChunkObject.GetComponent<MeshFilter>().mesh = chunk.meshes[LOD];

            if(LOD == 0 && chunk.meshes[colliderAccuracy] != null)
                chunk.mapChunkObject.GetComponent<MeshCollider>().sharedMesh = chunk.meshes[colliderAccuracy];

            chunk.currentLOD = LOD;
            return;
        }

        bool colliderRequested = LOD == 0 ? true : false;
        mapGenerator.requestChunkData
            (chunkSize,
            chunk.position, 
            LOD, 
            colliderRequested, 
            colliderAccuracy,
            onChunkDataReceived);
    }


    // updates a single chunk to the specified LOD --------------------------------------------------
    public void createNewChunks()
    {
        Vector3 position = viewer.position;
        float startX = position.x - LODThresholds[LODThresholds.Length - 1];
        float endX = position.x + LODThresholds[LODThresholds.Length - 1];
        float startY = position.z - LODThresholds[LODThresholds.Length - 1];
        float endY = position.z + LODThresholds[LODThresholds.Length - 1];

        for(float x = startX; x <= endX; x += chunkSize)
            for(float y = startY; y <= endY; y += chunkSize)
            {
                int chunkX = Mathf.RoundToInt(x / (float)chunkSize + 0.1f);
                int chunkY = Mathf.RoundToInt(y / (float)chunkSize + 0.1f);

                Vector2 chunkCenter = new Vector2(chunkX, chunkY);
                if (TerrainChunks.ContainsKey(chunkCenter))
                    continue;

                float realChunkX = chunkX * chunkSize;
                float realChunkY = chunkY * chunkSize;

                Vector3 center = new Vector3(realChunkX, 0, realChunkY);
                Vector3 sizes = new Vector3(chunkSize, chunkSize, chunkSize);
                Bounds b = new Bounds(center, sizes);
                float dist = Mathf.Sqrt(b.SqrDistance(position));

                if(dist < LODThresholds[LODThresholds.Length - 1])
                {
                    MapChunk newChunk = new MapChunk(chunkX, chunkY, chunkSize);
                    newChunk.mapChunkObject.transform.parent = this.GetComponent<Transform>();
                    TerrainChunks.Add(chunkCenter, newChunk);
                    newChunk.currentLOD = LODThresholds.Length - 1;

                    mapGenerator.requestChunkData(
                        chunkSize, 
                        newChunk.position,
                        LODThresholds.Length - 1,
                        false,
                        -1,
                        onChunkDataReceived);
                }
            }
    }

    
    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- MAP DRAWING -------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    public void onChunkDataReceived(MapGenerator.ChunkData chunkData)
    {
        Debug.Log(chunkData.chunkPosition + " data received");
        MapChunk chunk = null;
        TerrainChunks.TryGetValue(chunkData.chunkPosition, out chunk);

        if(chunk == null)
        {
            Debug.Log("ATTENTION! trying to set data on null chunk");
            return;
        }

        // store the mesh inside the object --------------------------------
        Mesh mesh = chunkData.meshData.createMesh();
        chunk.meshes[chunkData.meshData.LOD] = mesh;


        /* ----------------------------------------------------------------- 
            means we came back to a previous LOD before the response of this
            LOD was provided. We have already set the mesh for the current LOD,
            so we just store the new mesh for future use and do not change
            any setting
        */

        if (chunk.latestLODRequest != chunkData.meshData.LOD)
            return;


        // setting mesh -----------------------------------------------------
        GameObject chunkObject = chunk.mapChunkObject;
        chunkObject.GetComponent<MeshFilter>().mesh = mesh;
        Renderer textureRenderer = chunkObject.GetComponent<Renderer>();
        textureRenderer.sharedMaterial = new Material(terrainMaterial);

        // setting texture --------------------------------------------------
        Texture2D texture = new Texture2D(chunkSize + 1, chunkSize + 1);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(chunkData.colorMap);
        texture.Apply();
        textureRenderer.sharedMaterial.mainTexture = texture;

        // setting collider -------------------------------------------------
        if(chunkData.colliderMeshData != null)
        {
            Mesh colliderMesh = chunkData.colliderMeshData.createMesh();
            chunkObject.GetComponent<MeshCollider>().sharedMesh = colliderMesh;

            if (chunk.meshes[chunkData.colliderMeshData.LOD] == null)
                chunk.meshes[chunkData.colliderMeshData.LOD] = colliderMesh;
        }

        chunk.currentLOD = chunkData.meshData.LOD;
    }


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- MAP DRAWING -------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    public void initialize()
    {
        mapGenerator = this.GetComponent<MapGenerator>();
        TerrainChunks = new Dictionary<Vector2, MapChunk>();
        LODThresholds = new float[MapDisplay.NUMBER_OF_LODS];
        chunkSize = (chunkDimension * 32);
        viewerDistanceUpdate = chunkSize / (float)(viewerDistanceUpdateFrequency + 3);

        for (int i = 0; i < LODThresholds.Length; i++)
            LODThresholds[i] = (chunkSize / 2.0f + i * chunkSize) * accuracy / 2.0f;
    }
}
