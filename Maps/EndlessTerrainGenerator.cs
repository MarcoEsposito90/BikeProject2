using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EndlessTerrainGenerator : MonoBehaviour
{


    [Range(1, 7)]
    public int chunkDimension;
    public static int chunkSize
    {
        get;
        private set;
    }

    private static int scaledChunkSize;

    [Range(1, 10)]
    public int scale;

    [Range(1, 8)]
    public int subdivisions;

    [Range(1, 3)]
    public int accuracy;
    private float[] LODThresholds;

    [Range(1, 3)]
    public int keepUnvisible;
    private float removeThreshold;

    [Range(0, 2)]
    public int colliderAccuracy;

    [Range(1, 50)]
    public int textureSize;

    public Transform viewer;
    private Vector3 latestViewerRecordedPosition;

    [Range(1, 20)]
    public int viewerDistanceUpdateFrequency;
    private float viewerDistanceUpdate;

    public Material terrainMaterial;

    public bool singleChunk;

    private Dictionary<Vector2, MapChunk> TerrainChunks;
    private MapGenerator mapGenerator;
    public static float seedX, seedY;


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- UNITY CALLBACKS ---------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region UNITY

    void Awake()
    {
        initialize();
    }


    /* ----------------------------------------------------------------------------------------- */
    void Start()
    {

        viewer.position = new Vector3(0, viewer.position.y, 0);

        if (singleChunk)
            createSingleChunk(0, 0);
        else
        {
            createNewChunks();
            updateChunks();
        }
        
    }


    /* ----------------------------------------------------------------------------------------- */
    void Update()
    {
        if (!singleChunk)
        {
            float distance = Vector3.Distance(latestViewerRecordedPosition, viewer.position);

            if (distance >= viewerDistanceUpdate)
            {
                createNewChunks();
                updateChunks();
                latestViewerRecordedPosition = viewer.position;
            }
        }
    }

    #endregion

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- CHUNK UPDATING ----------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region CHUNK_UPDATING

    public void createSingleChunk(int x, int y)
    {
        Vector2 position = new Vector2(x, y);
        MapChunk newChunk = new MapChunk(x, y, chunkSize, scale, subdivisions, textureSize);
        newChunk.mapChunkObject.transform.parent = this.GetComponent<Transform>();
        TerrainChunks.Add(position, newChunk);
        newChunk.currentLOD = LODThresholds.Length - 1;

        mapGenerator.requestChunkData
            (newChunk,
            0,
            false,
            -1,
            onChunkDataReceived);
    }


    // checks the list of chunks for updates ------------------------------------------------------
    public void updateChunks()
    {
        List<Vector2> toBeRemoved = new List<Vector2>();

        foreach (MapChunk chunk in TerrainChunks.Values)
        {
            float dist = chunk.bounds.SqrDistance(new Vector3(viewer.position.x, 0, viewer.position.z));
            dist = Mathf.Sqrt(dist);
            bool visible = false;

            // check which is the correct load ---------------
            for (int i = 0; i < LODThresholds.Length; i++)
            {
                if (dist < LODThresholds[i])
                {
                    updateChunk(chunk, i);
                    visible = true;
                    break;
                }
            }

            // check if the object should be removed ---------
            if (!visible && dist >= removeThreshold)
            {
                //Debug.Log("must destroy chunk " + chunk.position);
                Destroy(chunk.mapChunkObject);
                toBeRemoved.Add(chunk.position);
            }
            else
                chunk.setVisible(visible);
        }

        // delete chunk references from map --------------
        foreach (Vector2 pos in toBeRemoved)
        {
            TerrainChunks.Remove(pos);
            //Debug.Log("TerrainChunks = " + TerrainChunks.Count);
        }
    }


    // updates a single chunk to the specified LOD --------------------------------------------------
    private void updateChunk(MapChunk chunk, int LOD)
    {
        if (chunk.latestLODRequest == LOD)
            return;

        chunk.latestLODRequest = LOD;

        if (chunk.meshes[LOD] != null)
        {
            //Debug.Log("chunk " + chunk.position + " with mesh " + LOD + "available");
            chunk.mapChunkObject.GetComponent<MeshFilter>().mesh = chunk.meshes[LOD];

            chunk.mapChunkObject.GetComponent<MeshCollider>().enabled = (LOD == 0);
            if (LOD == 0 && chunk.meshes[colliderAccuracy] != null)
                chunk.mapChunkObject.GetComponent<MeshCollider>().sharedMesh = chunk.meshes[colliderAccuracy];

            chunk.currentLOD = LOD;
            return;
        }

        bool colliderRequested = LOD == 0 ? true : false;
        mapGenerator.requestChunkData
            (chunk,
            LOD,
            colliderRequested,
            colliderAccuracy,
            onChunkDataReceived);
    }


    // creates chunks --------------------------------------------------------------
    public void createNewChunks()
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
                if (TerrainChunks.ContainsKey(chunkCenter))
                    continue;

                float realChunkX = chunkX * scaledChunkSize;
                float realChunkY = chunkY * scaledChunkSize;

                Vector3 center = new Vector3(realChunkX, 0, realChunkY);
                Vector3 sizes = new Vector3(scaledChunkSize, scaledChunkSize, scaledChunkSize);
                Bounds b = new Bounds(center, sizes);
                float dist = Mathf.Sqrt(b.SqrDistance(position));

                if (dist < LODThresholds[LODThresholds.Length - 1])
                {
                    MapChunk newChunk = new MapChunk(chunkX, chunkY, chunkSize, scale, subdivisions, textureSize);
                    newChunk.mapChunkObject.transform.parent = this.GetComponent<Transform>();
                    TerrainChunks.Add(chunkCenter, newChunk);
                    newChunk.currentLOD = LODThresholds.Length - 1;

                    mapGenerator.requestChunkData
                        (newChunk,
                        LODThresholds.Length - 1,
                        false,
                        -1,
                        onChunkDataReceived);
                }
            }
    }

    #endregion


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- MAP DRAWING -------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    public void onChunkDataReceived(MapGenerator.ChunkData chunkData)
    {
        //Debug.Log(chunkData.chunkPosition + " data received");
        MapChunk chunk = null;
        TerrainChunks.TryGetValue(chunkData.chunkPosition, out chunk);

        if (chunk == null)
        {
            Debug.Log("ATTENTION! trying to set data on null chunk");
            return;
        }

        // store the mesh inside the object --------------------------------
        Mesh mesh = chunkData.meshData.createMesh();
        mesh.name = "mesh" + chunkData.chunkPosition.ToString();
        chunk.meshes[chunkData.meshData.LOD] = mesh;

        Mesh colliderMesh = null;
        if (chunkData.colliderMeshData != null)
        {
            colliderMesh = chunkData.colliderMeshData.createMesh();
            if (chunk.meshes[chunkData.colliderMeshData.LOD] == null)
                chunk.meshes[chunkData.colliderMeshData.LOD] = colliderMesh;
        }


        /* ----------------------------------------------------------------- 
            means we came back to a previous LOD before the response of this
            LOD was provided. We have already set the mesh for the current LOD,
            so we just store the new mesh for future use and do not change
            any setting
        */

        if (chunk.latestLODRequest != chunkData.meshData.LOD)
        {
            //Debug.Log("chunk " + chunk.position + " not updated due to deceased request");
            return;
        }


        // setting mesh -----------------------------------------------------
        GameObject chunkObject = chunk.mapChunkObject;
        chunkObject.GetComponent<MeshFilter>().mesh = mesh;
        Renderer textureRenderer = chunkObject.GetComponent<Renderer>();
        textureRenderer.sharedMaterial = new Material(terrainMaterial);

        // setting texture --------------------------------------------------
        int texSize = Mathf.RoundToInt(((chunkSize + 1) * textureSize) / (float) (chunkData.meshData.LOD + 1));
        Debug.Log("texSize = " + texSize);
        Texture2D texture = new Texture2D(texSize, texSize);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(chunkData.colorMap);
        texture.Apply();
        textureRenderer.sharedMaterial.mainTexture = texture;

        // setting collider -------------------------------------------------
        chunkObject.GetComponent<MeshCollider>().enabled = (colliderMesh != null);
        if (colliderMesh != null)
            chunkObject.GetComponent<MeshCollider>().sharedMesh = colliderMesh;

        // scaling -----------------------------------------------------------
        chunkObject.transform.localScale = new Vector3(scale, scale, scale);

        chunk.currentLOD = chunkData.meshData.LOD;
    }


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- UTILITY ------------------------------------------------------ */
    /* ----------------------------------------------------------------------------------------- */

    public void initialize()
    {
        mapGenerator = this.GetComponent<MapGenerator>();
        TerrainChunks = new Dictionary<Vector2, MapChunk>();
        LODThresholds = new float[MapDisplay.NUMBER_OF_LODS];
        chunkSize = (chunkDimension * 32);
        scaledChunkSize = chunkSize * scale;
        viewerDistanceUpdate = scaledChunkSize / (float)(viewerDistanceUpdateFrequency + 3);

        for (int i = 0; i < LODThresholds.Length; i++)
            LODThresholds[i] = (scaledChunkSize / 2.0f + i * scaledChunkSize) * accuracy / 2.0f;

        removeThreshold = LODThresholds[LODThresholds.Length - 1] * keepUnvisible;

        System.Random random = new System.Random();
        seedX = ((float)random.NextDouble()) * random.Next(100);
        seedY = ((float)random.NextDouble()) * random.Next(100);
    }
}
