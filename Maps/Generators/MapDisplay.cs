using UnityEngine;
using System.Collections;

public class MapDisplay : MonoBehaviour
{

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- ATTRIBUTES --------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    public enum RenderingMode { Flat, Mesh };
    public RenderingMode renderingMode;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;
    public static float MESH_HEIGHT_MUL;
    public static AnimationCurve MESH_HEIGHT_CURVE;

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- UNITY -------------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    void Awake()
    {
        MESH_HEIGHT_MUL = meshHeightMultiplier;
        MESH_HEIGHT_CURVE = new AnimationCurve(this.meshHeightCurve.keys);
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
        (MapSector sector,
        int levelOfDetail,
        bool colliderRequested,
        int colliderAccuracy)

    {
        AnimationCurve meshHeightCurve = new AnimationCurve(this.meshHeightCurve.keys);

        float[,] heightMap = sector.heightMap;
        float[,] alphaMap = sector.alphaMap;

        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Color[] colorMap = TextureGenerator.generateColorHeightMap((float[,])heightMap.Clone());
        Color[] ColorAlphaMap = TextureGenerator.generateColorHeightMap(alphaMap);

        MeshGenerator.MeshData newMesh = null;
        if (renderingMode == RenderingMode.Mesh)
            newMesh = MeshGenerator.generateMesh(heightMap, meshHeightCurve, meshHeightMultiplier, levelOfDetail);
        else
            newMesh = MeshGenerator.generateMesh(width, height);

        MeshGenerator.MeshData colliderMesh = null;
        if (colliderRequested)
        {
            int colliderLOD = levelOfDetail + colliderAccuracy;
            if (renderingMode == RenderingMode.Mesh)
                colliderMesh = MeshGenerator.generateMesh(heightMap, meshHeightCurve, meshHeightMultiplier, colliderLOD);
            else
                colliderMesh = MeshGenerator.generateMesh(width, height);
        }

        return new MapSector.SectorData(levelOfDetail, newMesh, colliderMesh, colorMap, ColorAlphaMap);
    }

}
