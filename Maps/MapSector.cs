using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MapSector
{

    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- ATTRIBUTES ----------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #region ATTRIBUTES

    public Vector2 position
    {
        get;
        private set;
    }

    public int size
    {
        get;
        private set;
    }

    public int scale
    {
        get;
        private set;
    }

    public int subdivisions
    {
        get;
        private set;
    }

    public int numberOfLods
    {
        get;
        private set;
    }

    public GameObject prefabObject
    {
        get;
        private set;
    }

    public Bounds bounds
    {
        get;
        private set;
    }

    public int currentLOD
    {
        get;
        set;
    }

    public int latestLODRequest
    {
        get;
        set;
    }

    public bool isVisible
    {
        get;
        private set;
    }

    public Mesh[] meshes
    {
        get;
        private set;
    }

    public float[,] heightMap = null;

    public Dictionary<Vector2, Quadrant> quadrants;

    public bool roadsComputed;
    public bool mapComputed;

    #endregion

    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- CONSTRUCTORS --------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #region CONSTRUCTORS 

    public MapSector(int x, int y, int size, int scale, int subdivisions, int numberOfLods, GameObject prefabObject)
    {
        this.position = new Vector2(x, y);
        this.size = size;
        this.scale = scale;
        this.subdivisions = subdivisions;
        this.numberOfLods = numberOfLods;
        currentLOD = -1;
        latestLODRequest = -1;
        isVisible = true;
        roadsComputed = false;

        mapComputed = false;
        bounds = new Bounds(
            new Vector3(x * size * scale, 0, y * size * scale),
            new Vector3(size * scale, size * scale * 10, size * scale));

        meshes = new Mesh[numberOfLods];
        this.prefabObject = prefabObject;
        setPrefabObject(x, y);

        quadrants = new Dictionary<Vector2, Quadrant>();
        createQuadrants();
    }

    #endregion


    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- METHODS -------------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #region METHODS

    /* ------------------------------------------------------------------------------------------------- */
    private void setPrefabObject(int x, int y)
    {
        //prefabObject.AddComponent<MeshFilter>();
        //prefabObject.AddComponent<MeshRenderer>();
        //prefabObject.AddComponent<MeshCollider>();
        prefabObject.name = "sector (" + x + "," + y + ")";
        prefabObject.transform.position = new Vector3(x * size * scale, 0, y * size * scale);
        prefabObject.SetActive(true);
    }


    /* ------------------------------------------------------------------------------------------------- */
    public void resetPrefabObject()
    {
        prefabObject.GetComponent<MeshFilter>().mesh = null;
        prefabObject.GetComponent<Renderer>().sharedMaterial.mainTexture = null;
        prefabObject.GetComponent<MeshCollider>().sharedMesh = null;

        prefabObject.transform.position = Vector3.zero;
        prefabObject.name = "sector (available)";
        prefabObject.transform.localScale = Vector3.one;
        prefabObject.SetActive(false);
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


    /* ------------------------------------------------------------------------------------------------- */
    public void createQuadrants()
    {
        int quadrantWidth = (int)(size / (float)subdivisions);
        int quadrantHeight = (int)(size / (float)subdivisions);

        for (int j = 0; j < subdivisions; j++)
        {
            for (int i = 0; i < subdivisions; i++)
            {
                // 1) compute normalized coordinates of quadrant -------------
                float quadrantX = (position.x - 0.5f + (1.0f / (float)subdivisions) * i);
                float quadrantY = (position.y - 0.5f + (1.0f / (float)subdivisions) * j);

                Vector2 quadrantCoordinates = new Vector2(quadrantX, quadrantY);
                Vector2 localCoordinates = new Vector2(i, j);

                // 1) create quadrant and add it to list ---------------------
                Quadrant q = new Quadrant(new Vector2(i, j), quadrantCoordinates, quadrantWidth, quadrantHeight, this.subdivisions);
                quadrants.Add(localCoordinates, q);
            }
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
        public readonly MeshGenerator.MeshData meshData;
        public readonly MeshGenerator.MeshData colliderMeshData;
        public readonly Color[] colorMap;

        public SectorData(int LOD, MeshGenerator.MeshData meshData, MeshGenerator.MeshData colliderMeshData, Color[] colorMap)
        {
            this.meshData = meshData;
            this.colorMap = colorMap;
            this.colliderMeshData = colliderMeshData;
            this.LOD = LOD;
        }
    }

    #endregion
}
