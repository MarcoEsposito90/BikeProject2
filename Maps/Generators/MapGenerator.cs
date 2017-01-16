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

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- UNITY FUNCTIONS ---------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    void Awake()
    {
        random = new System.Random(seed);

        int multiplier = random.Next(1000, 10000);
        offsetX = (float)random.NextDouble() * multiplier;
        offsetY = (float)random.NextDouble() * multiplier;

        NoiseGenerator.Initialize(
            noiseScale,
            numberOfFrequencies,
            frequencyMultiplier,
            amplitudeDemultiplier,
            offsetX,
            offsetY);
    }


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- MY FUNCTIONS ------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region MAP_GENERATION

    /* ----------------------------------------------------------------------------------------- */
    public void GenerateMap
        (Vector2 sectorPosition,
        int LOD,
        bool colliderRequested,
        int colliderAccuracy)
    {
        int sectorSize = (int)GlobalInformation.Instance.getData(EndlessTerrainGenerator.SECTOR_SIZE);
        float[,] heightMap = NoiseGenerator.Instance.GenerateNoiseMap(sectorPosition);

        // 2) generate meshes and textures
        MapSector.SectorData sectorData = mapDisplayer.getSectorData(
            heightMap,
            LOD,
            colliderRequested,
            colliderAccuracy);
        sectorData.sectorPosition = sectorPosition;

        // 3) enqueue results
        parent.sectorResultsQueue.Enqueue(sectorData);
    }

    #endregion
}
