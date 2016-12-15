using UnityEngine;
using System.Collections;

public class MapDisplay : MonoBehaviour
{

    public static string MESH_HEIGHT_CURVE = "MapDisplay.MeshHeightCurve";
    public static string MESH_HEIGHT_MUL = "MapDisplay.MeshHeightMultiplier";

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- ATTRIBUTES --------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    public enum RenderingMode { Flat, Mesh };
    public RenderingMode renderingMode;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;
    //public static float MESH_HEIGHT_MUL;
    //public static AnimationCurve MESH_HEIGHT_CURVE;

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- UNITY -------------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    void Awake()
    {
        GlobalInformation.Instance.addData(MESH_HEIGHT_CURVE, meshHeightCurve);
        GlobalInformation.Instance.addData(MESH_HEIGHT_MUL, meshHeightMultiplier);
    }

    /* ----------------------------------------------------------------------------------------- */
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

    public MapSector.SectorData getSectorData
        (float[,] heightMap,
        int levelOfDetail,
        bool colliderRequested,
        int colliderAccuracy)

    {
        AnimationCurve meshHeightCurve = new AnimationCurve(this.meshHeightCurve.keys);

        //float[,] heightMap = sector.heightMap;
        //float[,] alphaMap = sector.alphaMap;

        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Color[] colorMap = TextureGenerator.generateColorHeightMap(heightMap);
        //Color[] ColorAlphaMap = TextureGenerator.generateColorHeightMap(alphaMap);

        MapMeshGenerator.MapMeshData newMesh = null;
        if (renderingMode == RenderingMode.Mesh)
            newMesh = MapMeshGenerator.generateMesh(heightMap, meshHeightCurve, meshHeightMultiplier, levelOfDetail);
        else
            newMesh = MapMeshGenerator.generateMesh(width, height);

        MapMeshGenerator.MapMeshData colliderMesh = null;
        if (colliderRequested)
        {
            int colliderLOD = levelOfDetail + colliderAccuracy;
            if (renderingMode == RenderingMode.Mesh)
                colliderMesh = MapMeshGenerator.generateMesh(heightMap, meshHeightCurve, meshHeightMultiplier, colliderLOD);
            else
                colliderMesh = MapMeshGenerator.generateMesh(width, height);
        }

        return new MapSector.SectorData(levelOfDetail, heightMap, newMesh, colliderMesh, colorMap);
    }
    
}
