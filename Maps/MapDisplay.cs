using UnityEngine;
using System.Collections;

public class MapDisplay : MonoBehaviour {

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- ATTRIBUTES --------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    private const int NumberOfLODS = 5;
    public static int NUMBER_OF_LODS = 5;

    public enum DisplayMode { GreyScale, Colour };
    public DisplayMode displayMode;

    public enum RenderingMode { Flat, Mesh };
    public RenderingMode renderingMode;

    //[Range(0, NumberOfLODS)]
    //public int levelOfDetail;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;
      

    public TerrainType[] sections;

    public bool autoUpdate;

    private float[,] latestNoiseMap;

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- STRUCTURES --------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    [System.Serializable]
    public struct TerrainType
    {
        public string name;

        [Range(0.0f, 2.0f)]
        public float height;
        public Color color;

    }

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- UNITY -------------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    void Start () {

	}
	
	void Update () {
	
	}

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- MY FUNCTIONS ------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    public MapGenerator.ChunkData getChunkData(float[,] map, int levelOfDetail)
    {
        latestNoiseMap = map;
        int width = map.GetLength(0);
        int height = map.GetLength(0);
        //GameObject chunkObject = mapChunk.mapChunkObject;

        MeshGenerator.MeshData newMesh = null;
        if (renderingMode == RenderingMode.Mesh)
            newMesh = MeshGenerator.generateMesh(map, meshHeightCurve, meshHeightMultiplier,levelOfDetail);
        else
            newMesh = MeshGenerator.generateMesh(width,height);

        Color[] colorMap = TextureGenerator.generateColorMap(map, displayMode, sections);

        return new MapGenerator.ChunkData(newMesh, colorMap);
        //chunkObject.GetComponent<MeshFilter>().mesh = newMesh;
        //Renderer textureRenderer = chunkObject.GetComponent<Renderer>();
        //textureRenderer.sharedMaterial = new Material(terrainMaterial);
        //textureRenderer.sharedMaterial.mainTexture = texture;
        //chunkObject.transform.localScale = new Vector3(width, 1, height);
    }


    /* ----------------------------------------------------------------------------------------- */
    //public void drawNoiseMap( mapObject)
    //{
    //    if (latestNoiseMap != null)
    //        drawNoiseMap(latestNoiseMap, mapObject);
    //}

}
