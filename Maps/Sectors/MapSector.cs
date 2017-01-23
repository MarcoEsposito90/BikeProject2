using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MapSector
{

    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- ATTRIBUTES ----------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #region ATTRIBUTES

    public Vector2 position { get; private set; }
    public int size { get; private set; }
    public int scale { get; private set; }
    //public int numberOfLods { get; private set; }
    public GameObject prefabObject { get; private set; }
    //public Bounds bounds { get; private set; }

    public int currentLOD;
    public int latestLODRequest;
    public bool needRedraw;
    public Mesh[] meshes { get; private set; }
    public bool[] meshUpdated;

    #endregion

    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- CONSTRUCTORS --------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #region CONSTRUCTORS 

    public MapSector(
        Vector2 position, 
        int size, 
        int scale)
    {
        this.position = position;
        this.size = size;
        this.scale = scale;
        currentLOD = -1;
        latestLODRequest = -1;

        needRedraw = false;

        int nLods = (int)GlobalInformation.Instance.getData(EndlessTerrainGenerator.NUMBER_OF_LODS);
        meshes = new Mesh[nLods];
        meshUpdated = new bool[nLods];

        for (int i = 0; i < meshUpdated.Length; i++)
            meshUpdated[i] = false;

        //isVisible = true;
        //mapComputed = false;

        //initializePrefabObject();
    }

    #endregion


    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- PREFAB MANAGING ------------------------------------------------ */
    /* ------------------------------------------------------------------------------------------------- */

    #region METHODS

    /* ------------------------------------------------------------------------------------------------- */
    public void initializePrefabObject(GameObject prefab)
    {
        this.prefabObject = prefab;
        prefabObject.name = "sector " + position;
        prefabObject.transform.position = new Vector3(position.x * size * scale, 0, position.y * size * scale);
        prefabObject.transform.localScale = new Vector3(scale, scale, scale);

        if (!prefabObject.activeInHierarchy)
            prefabObject.SetActive(true);
    }

    /* ------------------------------------------------------------------------------------------------- */
    public void setPrefabObject(Mesh collider, Mesh mesh, Color[] heightMap)
    {
        MapSectorHandler handler = prefabObject.GetComponent<MapSectorHandler>();
        updateMeshes(collider, mesh);
        handler.setTextures(heightMap);
    }


    /* ------------------------------------------------------------------------------------------------- */
    public void updateMeshes(Mesh collider, Mesh mesh)
    {
        MapSectorHandler handler = prefabObject.GetComponent<MapSectorHandler>();
        handler.setMeshes(collider, mesh);
    }


    /* ------------------------------------------------------------------------------------------------- */
    public void resetPrefabObject()
    {
        prefabObject.GetComponent<MapSectorHandler>().reset();

        prefabObject.transform.position = Vector3.zero;
        prefabObject.name = "sector (available)";
        prefabObject.transform.localScale = Vector3.one;
        prefabObject.SetActive(false);
        this.prefabObject = null;
    }

    #endregion

    /* ------------------------------------------------------------------------------------------------- */
    /* ------------------------------------ MESHES ----------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #region MESHES

    /* ------------------------------------------------------------------------------------------------- */
    public Mesh getMesh(int LOD, MeshData meshData)
    {
        Mesh mesh = null;

        if (meshes[LOD] == null || !meshUpdated[LOD])
        {
            mesh = meshData.createMesh();
            meshes[LOD] = mesh;
            meshUpdated[LOD] = true;
        }
        else
        {
            mesh = meshes[LOD];
        }

        return mesh;
    }


    /* ------------------------------------------------------------------------------------------------- */
    public void resetMeshes()
    {
        for(int i = 0; i < meshes.Length; i++)
        {
            meshes[i] = null;
            meshUpdated[i] = false;
        }
    }

    #endregion


    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- SUBCLASSES ----------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #region SUBCLASSES

    public class SectorData
    {
        public Vector2 sectorPosition;
        public readonly int LOD;
        public readonly float[,] heightMap;
        public readonly MeshData meshData;
        public readonly MeshData colliderMeshData;
        public readonly Color[] colorMap;

        public SectorData
            (int LOD,
            float[,] heightMap,
            MeshData meshData,
            MeshData colliderMeshData,
            Color[] colorMap)
        {
            this.meshData = meshData;
            this.colorMap = colorMap;
            this.colliderMeshData = colliderMeshData;
            this.LOD = LOD;
            this.heightMap = heightMap;
        }
    }

    #endregion
}
