using UnityEngine;
using System.Collections.Generic;
using System.Threading;

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
    }


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- MY FUNCTIONS ------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    public void GenerateMap(EndlessTerrainGenerator.MapChunk mapChunk, int LOD)
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(
            mapChunk.size + 1,
            mapChunk.size + 1, 
            noiseScale,
            mapChunk.position.x * mapChunk.size,
            mapChunk.position.y * mapChunk.size,
            numberOfFrequencies,
            frequencyMultiplier,
            amplitudeDemultiplier);

        mapDisplayer.drawNoiseMap(noiseMap, mapChunk, LOD);
    }

}
