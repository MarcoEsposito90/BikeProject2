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

    [Range(1.0f, 30.0f)]
    public float frequencyMultiplier;

    [Range(1.0f, 100.0f)]
    public float amplitudeDemultiplier;

    [Range(1.0f, 10.0f)]
    public float alphaScale;

    private Queue<SectorCallbackData> resultsQueue;
    public MapDisplay mapDisplayer;
    private System.Random random;

    public float offsetX
    {
        get; private set;
    }

    public float offsetY
    {
        get;
        private set;
    }

    public float alphaOffsetX
    {
        get; private set;
    }

    public float alphaOffsetY
    {
        get;
        private set;
    }
    //public bool autoUpdate;

    //private int width;
    //private int height;

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- UNITY FUNCTIONS ---------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    void Start()
    {
        resultsQueue = new Queue<SectorCallbackData>();
        random = new System.Random(seed);

        int multiplier = random.Next(1000, 10000);
        offsetX = (float)random.NextDouble() * multiplier;
        offsetY = (float)random.NextDouble() * multiplier;
        alphaOffsetX = (float)random.NextDouble() * multiplier;
        alphaOffsetY = (float)random.NextDouble() * multiplier;
    }


    /* ----------------------------------------------------------------------------------------- */
    void Update()
    {
        lock (resultsQueue)
        {
            for (int i = 0; i < resultsQueue.Count; i++)
            {
                SectorCallbackData callbackData = resultsQueue.Dequeue();
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

    public void requestSectorData
        (MapSector sector,
        int LOD,
        bool colliderRequested,
        int colliderAccuracy,
        Action<MapSector.SectorData> callback)
    {
        ThreadStart ts = delegate
       {
           GenerateMap(sector, LOD, colliderRequested, colliderAccuracy, callback);
       };

        Thread t = new Thread(ts);
        t.Start();
    }


    /* ----------------------------------------------------------------------------------------- */
    private void GenerateMap
        (MapSector sector,
        int LOD,
        bool colliderRequested,
        int colliderAccuracy,
        Action<MapSector.SectorData> callback)
    {

        if (!sector.mapComputed)
        {
            sector.mapComputed = true;
            float[,] heightMap = Noise.GenerateNoiseMap(
            sector.size + 1,
            sector.size + 1,
            noiseScale,
            (sector.position.x - 0.5f) * sector.size + offsetX,
            (sector.position.y + 0.5f) * sector.size + offsetY,
            numberOfFrequencies,
            frequencyMultiplier,
            amplitudeDemultiplier);

            float[,] alphaMap = Noise.GenerateNoiseMap(
                sector.size + 1,
                sector.size + 1,
                noiseScale / alphaScale,
                (sector.position.x - 0.5f) * sector.size + alphaOffsetX,
                (sector.position.y + 0.5f) * sector.size + alphaOffsetY,
                numberOfFrequencies,
                frequencyMultiplier,
                amplitudeDemultiplier);

            sector.heightMap = heightMap;
            sector.alphaMap = alphaMap;
            sector.mapComputed = true;
        }

        MapSector.SectorData chunkData = mapDisplayer.getSectorData(sector, LOD, colliderRequested, colliderAccuracy);
        chunkData.sectorPosition = sector.position;

        lock (resultsQueue)
        {
            resultsQueue.Enqueue(new SectorCallbackData(chunkData, callback));
        }
    }


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- MY CLASSES --------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */


    /* ----------------------------------------------------------------------------------------- */
    public struct SectorCallbackData
    {
        public readonly MapSector.SectorData data;
        public readonly Action<MapSector.SectorData> callback;

        public SectorCallbackData(MapSector.SectorData data, Action<MapSector.SectorData> callback)
        {
            this.data = data;
            this.callback = callback;
        }
    }
}
