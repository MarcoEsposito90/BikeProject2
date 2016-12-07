using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using System;

public class MapGenerator : MonoBehaviour
{

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- ATTRIBUTES --------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

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

    public MapDisplay mapDisplayer;
    public EndlessTerrainGenerator parent;
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

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- UNITY FUNCTIONS ---------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    void Awake()
    {
        random = new System.Random(seed);

        int multiplier = random.Next(1000, 10000);
        offsetX = (float)random.NextDouble() * multiplier;
        offsetY = (float)random.NextDouble() * multiplier;
        alphaOffsetX = (float)random.NextDouble() * multiplier;
        alphaOffsetY = (float)random.NextDouble() * multiplier;

        NoiseGenerator.Initialize(
            noiseScale,
            numberOfFrequencies,
            frequencyMultiplier,
            amplitudeDemultiplier,
            offsetX,
            offsetY);
    }

    void Start()
    {

    }


    /* ----------------------------------------------------------------------------------------- */
    void Update()
    {
    }

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- MY FUNCTIONS ------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    public void requestSectorData
        (Vector2 sectorPosition,
        int LOD,
        bool colliderRequested,
        int colliderAccuracy)
    {
        ThreadStart ts = delegate
       {
           GenerateMap(sectorPosition, LOD, colliderRequested, colliderAccuracy);
       };

        Thread t = new Thread(ts);
        t.Start();
    }


    /* ----------------------------------------------------------------------------------------- */
    private void GenerateMap
        (Vector2 sectorPosition,
        int LOD,
        bool colliderRequested,
        int colliderAccuracy)
    {
        // 1) generate noise maps
        //sector.mapComputed = true;

        int sectorSize = (int)GlobalInformation.Instance.getData(EndlessTerrainGenerator.SECTOR_SIZE);
        float[,] heightMap = NoiseGenerator.Instance.GenerateNoiseMap(
            sectorSize + 1,
            1,
            (sectorPosition.x - 0.5f) * sectorSize,
            (sectorPosition.y + 0.5f) * sectorSize);

        float[,] alphaMap = NoiseGenerator.Instance.GenerateNoiseMap(
            sectorSize + 1,
            1.0f / alphaScale,
            (sectorPosition.x - 0.5f) * sectorSize + alphaOffsetX,
            (sectorPosition.y + 0.5f) * sectorSize + alphaOffsetY);

        //sector.heightMap = heightMap;
        //sector.alphaMap = alphaMap;
        //sector.mapComputed = true;

        // 2) generate meshes and textures
        MapSector.SectorData sectorData = mapDisplayer.getSectorData(
            heightMap,
            alphaMap,
            LOD,
            colliderRequested,
            colliderAccuracy);

        sectorData.sectorPosition = sectorPosition;

        // 3) enqueue results
        parent.sectorResultsQueue.Enqueue(sectorData);
    }

}
