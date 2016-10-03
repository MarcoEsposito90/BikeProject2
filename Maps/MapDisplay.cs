using UnityEngine;
using System.Collections;

public class MapDisplay : MonoBehaviour {

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- ATTRIBUTES --------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    private const int NumberOfLods = 5;
    public static int NUMBER_OF_LODS = 5;
    private RoadsGenerator roadsGenerator;

    public enum DisplayMode { GreyScale, Colour };
    public DisplayMode displayMode;

    public enum RenderingMode { Flat, Mesh };
    public RenderingMode renderingMode;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    public TerrainType[] sections;

    

    //public bool autoUpdate;
    //private float[,] latestNoiseMap;

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

        roadsGenerator = this.GetComponent<RoadsGenerator>();
	}
	
	void Update () {
	
	}

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- MY FUNCTIONS ------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    public MapGenerator.ChunkData getChunkData
        (float[,] map,
        Vector2 chunkPosition, 
        int levelOfDetail, 
        bool colliderRequested, 
        int colliderAccuracy)

    {
        //latestNoiseMap = map;
        //GameObject chunkObject = mapChunk.mapChunkObject;

        int width = map.GetLength(0);
        int height = map.GetLength(1);

        if (roadsGenerator != null)
            roadsGenerator.generateRoads(map, chunkPosition);

        MeshGenerator.MeshData newMesh = null;
        if (renderingMode == RenderingMode.Mesh)
            newMesh = MeshGenerator.generateMesh(map, meshHeightCurve, meshHeightMultiplier,levelOfDetail);
        else
            newMesh = MeshGenerator.generateMesh(width,height);

        Color[] colorMap = TextureGenerator.generateColorMap(map, displayMode, sections);

        MeshGenerator.MeshData colliderMesh = null;
        if (colliderRequested)
        {
            int colliderLOD = levelOfDetail + colliderAccuracy;
            if (renderingMode == RenderingMode.Mesh)
                colliderMesh = MeshGenerator.generateMesh(map, meshHeightCurve, meshHeightMultiplier, colliderLOD);
            else
                colliderMesh = MeshGenerator.generateMesh(width, height);
        }

        return new MapGenerator.ChunkData(newMesh, colliderMesh, colorMap);
    }


    /* ----------------------------------------------------------------------------------------- */
    //public void drawNoiseMap( mapObject)
    //{
    //    if (latestNoiseMap != null)
    //        drawNoiseMap(latestNoiseMap, mapObject);
    //}

}
