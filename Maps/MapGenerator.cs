using UnityEngine;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour {

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- ATTRIBUTES --------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    [Range(1,7)]
    public int chunkDimension;  

    [Range(0.5f, 50.0f)]
    public float noiseScale;

    [Range(1,8)]
    public int numberOfFrequencies;

    [Range(1.0f,10.0f)]
    public float frequencyMultiplier;

    [Range(1.0f,10.0f)]
    public float amplitudeDemultiplier;

    public Transform viewer;
    public float viewerDistanceUpdate;

    public bool autoUpdate;

    private int width;
    private int height;

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- CONSTRUCTORS ------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    public MapGenerator
        (int width,
        int height, 
        float noiseScale, 
        float frequencyMultiplier, 
        float amplitudeDemultiplier)
    {
        this.width = width;
        this.height = height;
        this.noiseScale = noiseScale;
        this.frequencyMultiplier = frequencyMultiplier;
        this.amplitudeDemultiplier = amplitudeDemultiplier;
    }


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- MY FUNCTIONS ------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    public void GenerateMap(int x, int y)
    {
        width = 32 * chunkDimension + 1;
        height = 32 * chunkDimension + 1;

        float[,] noiseMap = Noise.GenerateNoiseMap(
            width, 
            height, 
            noiseScale,
            x*width,
            y*width,
            numberOfFrequencies,
            frequencyMultiplier,
            amplitudeDemultiplier);

        this.GetComponent<MapDisplay>().drawNoiseMap(noiseMap);
    }

}
