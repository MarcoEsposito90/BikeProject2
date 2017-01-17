using UnityEngine;
using System.Collections;
using System.Threading;

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

    public static MapDisplay Instance;
    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- UNITY -------------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    void Awake()
    {
        GlobalInformation.Instance.addData(MESH_HEIGHT_CURVE, meshHeightCurve);
        GlobalInformation.Instance.addData(MESH_HEIGHT_MUL, meshHeightMultiplier);
        Instance = this;
    }


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- MY FUNCTIONS ------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    public void createSectorDisplayDataAsynch(Vector2 gridPosition, int levelOfDetail)
    {
        ThreadStart ts = delegate
        {
            createSectorDisplayData(gridPosition, levelOfDetail);
        };
        Thread t = new Thread(ts);
        t.Start();
    }


    /* ----------------------------------------------------------------------------------------- */
    public void createSectorDisplayData(Vector2 gridPosition, int LOD)
    {
        AnimationCurve meshHeightCurve = new AnimationCurve(this.meshHeightCurve.keys);
        float[,] heightMap = NoiseGenerator.Instance[gridPosition];

        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        Color[] colorMap = TextureGenerator.generateColorHeightMap(heightMap);

        MapMeshGenerator.MapMeshData newMesh = null;
        if (renderingMode == RenderingMode.Mesh)
            newMesh = MapMeshGenerator.generateMesh(heightMap, meshHeightCurve, meshHeightMultiplier, LOD);
        else
            newMesh = MapMeshGenerator.generateMesh(width, height);

        MapMeshGenerator.MapMeshData colliderMesh = null;
        if (LOD == 0)
        {
            int colliderLOD = 1;
            if (renderingMode == RenderingMode.Mesh)
                colliderMesh = MapMeshGenerator.generateMesh(heightMap, meshHeightCurve, meshHeightMultiplier, colliderLOD);
            else
                colliderMesh = MapMeshGenerator.generateMesh(width, height);
        }

        MapSector.SectorData newData = new MapSector.SectorData(LOD, heightMap, newMesh, colliderMesh, colorMap);
        newData.sectorPosition = gridPosition;
        EndlessTerrainGenerator.Instance.sectorResultsQueue.Enqueue(newData);
    }

}
