using UnityEngine;
using System.Collections;

public class MapDisplay : MonoBehaviour
{

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- ATTRIBUTES --------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    [Range(2,10)]
    public int NumberOfLods;

    //public enum DisplayMode { GreyScale, Colour, Textured };
    //public DisplayMode displayMode;

    public enum RenderingMode { Flat, Mesh };
    public RenderingMode renderingMode;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;
    public RoadsGenerator roadsGenerator;

    //public Section[] sections;
    //public bool autoUpdate;
    //private float[,] latestNoiseMap;

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- STRUCTURES --------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    [System.Serializable]
    public class Section
    {
        [SerializeField]
        public string name;

        [SerializeField, Range(0.0f, 2.0f)]
        public float height;

        [SerializeField]
        public Color color;

        [SerializeField, Range(1, 20)]
        public int tiles;

        [SerializeField]
        public Texture2D texture;

        [SerializeField]
        public Color[,] colorMap { get; private set; }

        public void generateColorMap()
        {
            colorMap = new Color[texture.width, texture.height];
            for (int i = 0; i < texture.width; i++)
                for (int j = 0; j < texture.height; j++)
                    colorMap[i, j] = texture.GetPixel(i, j);
        }
    }

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- UNITY -------------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    //void Awake()
    //{
    //    //foreach (Section s in sections)
    //    //    s.generateColorMap();
    //}

    void Start()
    {
        if (roadsGenerator == null)
            Debug.Log("editor won't generate roads");
    }

    //void Update()
    //{

    //}

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- MY FUNCTIONS ------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    public MapSector.SectorData getSectorData
        (float[,] map,
        MapSector sector,
        int levelOfDetail,
        bool colliderRequested,
        int colliderAccuracy)

    {
        AnimationCurve meshHeightCurve = new AnimationCurve(this.meshHeightCurve.keys);

        int width = map.GetLength(0);
        int height = map.GetLength(1);

        Color[] colorMap = TextureGenerator.generateColorHeightMap(map);

        if (roadsGenerator != null && !sector.roadsComputed)
            roadsGenerator.generateRoads(map, sector);

        MeshGenerator.MeshData newMesh = null;
        if (renderingMode == RenderingMode.Mesh)
            newMesh = MeshGenerator.generateMesh(map, meshHeightCurve, meshHeightMultiplier, levelOfDetail);
        else
            newMesh = MeshGenerator.generateMesh(width, height);

        MeshGenerator.MeshData colliderMesh = null;
        if (colliderRequested)
        {
            int colliderLOD = levelOfDetail + colliderAccuracy;
            if (renderingMode == RenderingMode.Mesh)
                colliderMesh = MeshGenerator.generateMesh(map, meshHeightCurve, meshHeightMultiplier, colliderLOD);
            else
                colliderMesh = MeshGenerator.generateMesh(width, height);
        }

        return new MapSector.SectorData(levelOfDetail, newMesh, colliderMesh, colorMap);
    }

}
