﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EndlessTerrainGenerator : MonoBehaviour {


    [Range(1, 7)]
    public int chunkDimension;
    private int chunkSize;

    [Range(1,3)]
    public int accuracy;
    private float[] LODThresholds;

    public Transform viewer;
    private Vector3 latestViewerRecordedPosition;

    [Range(1,10)]
    public int viewerDistanceUpdateFrequency;
    private float viewerDistanceUpdate;

    public Material terrainMaterial;

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
        public int latestLOD;
        public bool isVisible;

        public MapChunk(int x, int y, int size)
        {
            this.position = new Vector2(x, y);
            this.size = size;
            latestLOD = -1;
            isVisible = true;
            bounds = new Bounds(new Vector3(x*size, 0, y*size), new Vector3(size,size, size));
            
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
        mapGenerator = this.GetComponent<MapGenerator>();

        chunkSize = (chunkDimension * 32);
        viewerDistanceUpdate = chunkSize / (float)(viewerDistanceUpdateFrequency + 3);
        LODThresholds = new float[MapDisplay.NUMBER_OF_LODS];

        for(int i = 0; i < LODThresholds.Length; i++)
            LODThresholds[i] = (chunkSize / 2.0f + i * chunkSize) * accuracy / 2.0f;

        viewer.position = new Vector3(0, 0, 0);
        TerrainChunks = new Dictionary<Vector2, MapChunk>();

        createNewChunks(viewer.position);
        updateChunks();
    }

   
    /* ----------------------------------------------------------------------------------------- */
    void Update()
    {
        float distance = Vector3.Distance(latestViewerRecordedPosition, viewer.position);

        if (distance >= viewerDistanceUpdate)
        {
            createNewChunks(viewer.position);
            updateChunks();
            latestViewerRecordedPosition = viewer.position;
        }

    }


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- CHUNK UPDATING ----------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    // checks the list of chunks for updates ------------------------------------------------------
    private void updateChunks()
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
        if (chunk.latestLOD == LOD)
            return;

        chunk.latestLOD = LOD;
        mapGenerator.requestChunkData(chunkSize, chunk.position, LOD, onChunkDataReceived);
    }


    // updates a single chunk to the specified LOD --------------------------------------------------
    private void createNewChunks(Vector3 position)
    {
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
                    newChunk.latestLOD = LODThresholds.Length - 1;

                    mapGenerator.requestChunkData(
                        chunkSize, 
                        newChunk.position,
                        LODThresholds.Length - 1, 
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

        GameObject chunkObject = chunk.mapChunkObject;

        // setting mesh -----------------------------------------------------
        Mesh mesh = chunkData.meshData.createMesh();
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
        chunkObject.GetComponent<MeshCollider>().sharedMesh = mesh;
        
    }
}
