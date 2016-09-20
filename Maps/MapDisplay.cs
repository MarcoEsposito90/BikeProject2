using UnityEngine;
using System.Collections;

public class MapDisplay : MonoBehaviour {

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- ATTRIBUTES --------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    private GameObject mapObject;

    public enum DisplayMode { GreyScale, Colour };
    public DisplayMode displayMode;

    public enum RenderingMode { Flat, Mesh };
    public RenderingMode renderingMode;

    [Range(0,4)]
    public int levelOfDetail;
    public Transform viewer;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;
    public Material terrainMaterial;    

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
    /* -------------------------- CLASSES ------------------------------------------------------ */
    /* ----------------------------------------------------------------------------------------- */

    public class MapQuadrant
    {
        public Vector2 coordinates;
        public int width;
        public int height;
    }

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- CONSTRUCTORS ------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

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

    public void drawNoiseMap(float[,] map)
    {
        latestNoiseMap = map;
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        if(mapObject == null)
        {
            mapObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            mapObject.transform.position = new Vector3(0, 0, 0);
        }

        if (renderingMode == RenderingMode.Mesh)
            mapObject.GetComponent<MeshFilter>().mesh = MeshGenerator.generateMesh(map, meshHeightCurve, meshHeightMultiplier,levelOfDetail);
        else
            mapObject.GetComponent<MeshFilter>().mesh = MeshGenerator.generateMesh(width,height);

        Texture2D texture = TextureGenerator.generateTexture(map, displayMode, sections);
        Renderer textureRenderer = mapObject.GetComponent<Renderer>();
        textureRenderer.sharedMaterial = terrainMaterial;
        textureRenderer.sharedMaterial.mainTexture = texture;
        mapObject.transform.localScale = new Vector3(width, 1, height);
    }


    /* ----------------------------------------------------------------------------------------- */
    public void drawNoiseMap()
    {
        if (latestNoiseMap != null)
            drawNoiseMap(latestNoiseMap);
    }

}
