using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using System;

public class MapGenerator : MonoBehaviour {

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- ATTRIBUTES --------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    //[Range(1,7)]
    //public int chunkDimension;  

    [Range(0.5f, 50.0f)]
    public float noiseScale;

    [Range(1,8)]
    public int numberOfFrequencies;

    [Range(1.0f,10.0f)]
    public float frequencyMultiplier;

    [Range(1.0f,10.0f)]
    public float amplitudeDemultiplier;

    private Queue<ChunkCallbackData> resultsQueue;
    private MapDisplay mapDisplayer;
    //public bool autoUpdate;

    //private int width;
    //private int height;

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- UNITY FUNCTIONS ---------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    void Start()
    {
        mapDisplayer = this.GetComponent<MapDisplay>();
        resultsQueue = new Queue<ChunkCallbackData>();
    }


    /* ----------------------------------------------------------------------------------------- */
    void Update()
    {
        lock (resultsQueue)
        {
            if(resultsQueue.Count > 0)
            {
                for(int i = 0; i < resultsQueue.Count; i++)
                {
                    ChunkCallbackData callbackData = resultsQueue.Dequeue();
                    callbackData.callback(callbackData.data);

                    /*  IMPORTANT INFO -------------------------------------------------------------
                        the results are dequeued here in the update function, in order
                        to use them in the main thread. This is necessary, since it is not
                        possible to use them in secondary threads (for example, you cannot create
                        a mesh) 
                    */
                }
            }
        }
    }

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- MY FUNCTIONS ------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    public void requestChunkData
        (int size, 
        Vector2 chunkPosition, 
        int LOD, 
        bool colliderRequested, 
        int colliderAccuracy,
        Action<ChunkData> callback)
    {
        ThreadStart ts  = delegate 
        {
            GenerateMap(size, chunkPosition, LOD, colliderRequested, colliderAccuracy, callback);
        };

        Thread t = new Thread(ts);
        t.Start();
    }


    /* ----------------------------------------------------------------------------------------- */
    private void GenerateMap
        (int size, 
        Vector2 chunkPosition, 
        int LOD, 
        bool colliderRequested, 
        int colliderAccuracy,
        Action<ChunkData> callback)
    {
        float[,] heightMap = Noise.GenerateNoiseMap(
            size + 1,
            size + 1, 
            noiseScale,
            chunkPosition.x * size,
            chunkPosition.y * size,
            numberOfFrequencies,
            frequencyMultiplier,
            amplitudeDemultiplier);

        ChunkData chunkData = mapDisplayer.getChunkData(heightMap, chunkPosition, LOD, colliderRequested, colliderAccuracy);
        chunkData.chunkPosition = chunkPosition;

        lock (resultsQueue)
        {
            resultsQueue.Enqueue(new ChunkCallbackData(chunkData, callback));
        }
    }


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- MY CLASSES --------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    public class ChunkData
    {
        public Vector2 chunkPosition;
        public readonly MeshGenerator.MeshData meshData;
        public readonly MeshGenerator.MeshData colliderMeshData;
        public readonly Color[] colorMap;
        
        public ChunkData(MeshGenerator.MeshData meshData, MeshGenerator.MeshData colliderMeshData, Color[] colorMap)
        {
            this.meshData = meshData;
            this.colorMap = colorMap;
            this.colliderMeshData = colliderMeshData;
        }
    }


    /* ----------------------------------------------------------------------------------------- */
    public struct ChunkCallbackData
    {
        public readonly ChunkData data;
        public readonly Action<ChunkData> callback;

        public ChunkCallbackData(ChunkData data, Action<ChunkData> callback)
        {
            this.data = data;
            this.callback = callback;
        }
    }
}
