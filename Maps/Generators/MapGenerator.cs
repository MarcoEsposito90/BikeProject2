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
    public void GenerateMap(Vector2 sectorPosition)
    {
        NoiseGenerator.Instance.GenerateNoiseMap(sectorPosition);
    }


    /* ----------------------------------------------------------------------------------------- */
    public void GenerateMapAsynch(Vector2 sectorPosition)
    {
        ThreadStart ts = delegate
        {
            GenerateMap(sectorPosition);
        };
        Thread t = new Thread(ts);
        t.Start();
    }

    #endregion
}
