using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EndlessTerrainGenerator : MonoBehaviour {

    [Range(1, 7)]
    public int chunkDimension;

    [Range(0.5f, 50.0f)]
    public float noiseScale;

    [Range(1, 8)]
    public int numberOfFrequencies;

    [Range(1.0f, 10.0f)]
    public float frequencyMultiplier;

    [Range(1.0f, 10.0f)]
    public float amplitudeDemultiplier;

    public Transform viewer;
    public float viewerDistanceUpdate;

    public bool autoUpdate;

    private List<MapGenerator> TerrainChunks;


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- MY FUNCTIONS ------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */


}
