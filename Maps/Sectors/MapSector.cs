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
    public int subdivisions { get; private set; }
    public int numberOfLods { get; private set; }
    public GameObject prefabObject { get; private set; }
    //public Bounds bounds { get; private set; }

    private int _currentLOD;
    public int currentLOD
    {
        get { return _currentLOD; }
        set
        {
            _currentLOD = value;
            if (prefabObject != null)
                prefabObject.GetComponent<MapSectorHandler>().currentLOD = _currentLOD;
        }
    }

    public int latestLODRequest;
    public bool isVisible { get; private set; }
    public Mesh[] meshes { get; private set; }
    public float[,] heightMap = null;
    //public float[,] alphaMap = null;
    public bool mapComputed;
    public bool needRedraw;

    #endregion

    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- CONSTRUCTORS --------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #region CONSTRUCTORS 

    public MapSector(
        Vector2 position, 
        int size, 
        int scale, 
        int subdivisions, 
        int numberOfLods, 
        GameObject prefabObject)
    {
        this.position = position;
        this.size = size;
        this.scale = scale;
        this.subdivisions = subdivisions;
        this.numberOfLods = numberOfLods;
        currentLOD = -1;
        latestLODRequest = -1;
        isVisible = true;

        mapComputed = false;
        needRedraw = false;
        //bounds = new Bounds(
        //    new Vector3(x * size * scale, 0, y * size * scale),
        //    new Vector3(size * scale, size * scale * 10, size * scale));

        meshes = new Mesh[numberOfLods];
        this.prefabObject = prefabObject;
        initializePrefabObject();
    }

    #endregion


    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- PREFAB MANAGING ------------------------------------------------ */
    /* ------------------------------------------------------------------------------------------------- */

    #region METHODS

    /* ------------------------------------------------------------------------------------------------- */
    private void initializePrefabObject()
    {
        prefabObject.name = "sector " + position;
        prefabObject.transform.position = new Vector3(position.x * size * scale, 0, position.y * size * scale);

        if (!prefabObject.activeInHierarchy)
            prefabObject.SetActive(true);

        prefabObject.GetComponent<MeshCollider>().enabled = false;
    }

    /* ------------------------------------------------------------------------------------------------- */
    public void setPrefabObject(Mesh collider, Mesh mesh, Color[] heightMap, List<Color[]> alphaMaps)
    {

        MapSectorHandler handler = prefabObject.GetComponent<MapSectorHandler>();
        updateMeshes(collider, mesh);
        handler.setTextures(heightMap, alphaMaps);
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
        //prefabObject.SetActive(false);
        this.prefabObject = null;
    }

    /* ------------------------------------------------------------------------------------------------- */
    public void setVisible(bool visibility)
    {
        if (isVisible == visibility)
            return;

        isVisible = visibility;
        prefabObject.SetActive(visibility);
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
