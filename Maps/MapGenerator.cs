using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using System;

public class MapGenerator : MonoBehaviour
{

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- ATTRIBUTES --------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    //[Range(1,7)]
    //public int chunkDimension;  

    [Range(10.0f, 5000.0f)]
    public float noiseScale;

    [Range(0, 1000)]
    public int seed;

    [Range(1, 8)]
    public int numberOfFrequencies;

    [Range(1.0f, 10.0f)]
    public float frequencyMultiplier;

    [Range(1.0f, 10.0f)]
    public float amplitudeDemultiplier;

    private Queue<ChunkCallbackData> resultsQueue;
    private MapDisplay mapDisplayer;
    private System.Random random;
    private float offsetX, offsetY;
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
        random = new System.Random(seed);

        int multiplier = random.Next(1000, 10000);
        offsetX = (float) random.NextDouble() * multiplier;
        offsetY = (float) random.NextDouble() * multiplier;
    }


    /* ----------------------------------------------------------------------------------------- */
    void Update()
    {
        lock (resultsQueue)
        {
            for (int i = 0; i < resultsQueue.Count; i++)
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

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- MY FUNCTIONS ------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    public void requestChunkData
        (MapChunk chunk,
        int LOD,
        bool colliderRequested,
        int colliderAccuracy,
        Action<ChunkData> callback)
    {
        ThreadStart ts = delegate
       {
           GenerateMap(chunk, LOD, colliderRequested, colliderAccuracy, callback);
       };

        Thread t = new Thread(ts);
        t.Start();
    }


    /* ----------------------------------------------------------------------------------------- */
    private void GenerateMap
        (MapChunk chunk,
        int LOD,
        bool colliderRequested,
        int colliderAccuracy,
        Action<ChunkData> callback)
    {

        if (!chunk.mapComputed)
        {
            chunk.mapComputed = true;
            float[,] heightMap = Noise.GenerateNoiseMap(
            chunk.size + 1,
            chunk.size + 1,
            noiseScale,
            chunk.position.x * chunk.size + offsetX,
            chunk.position.y * chunk.size + offsetY,
            numberOfFrequencies,
            frequencyMultiplier,
            amplitudeDemultiplier);

            chunk.heightMap = heightMap;
        }

        ChunkData chunkData = mapDisplayer.getChunkData(chunk.heightMap, chunk, LOD, colliderRequested, colliderAccuracy);
        chunkData.chunkPosition = chunk.position;

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
