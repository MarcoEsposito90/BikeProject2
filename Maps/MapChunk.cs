using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MapChunk
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

    public GameObject mapChunkObject
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

    public int textureSize
    {
        get;
        private set;
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

    public MapChunk(int x, int y, int size, int scale, int subdivisions, int textureSize)
    {
        this.position = new Vector2(x, y);
        this.size = size;
        this.scale = scale;
        this.subdivisions = subdivisions;
        currentLOD = -1;
        latestLODRequest = -1;
        isVisible = true;
        roadsComputed = false;
        this.textureSize = textureSize;
        mapComputed = false;
        bounds = new Bounds(
            new Vector3(x * size * scale, 0, y * size * scale), 
            new Vector3(size * scale, size * scale, size * scale));

        meshes = new Mesh[MapDisplay.NUMBER_OF_LODS];

        mapChunkObject = new GameObject("chunk (" + x + "," + y + ")");
        mapChunkObject.AddComponent<MeshFilter>();
        mapChunkObject.AddComponent<MeshRenderer>();
        mapChunkObject.AddComponent<MeshCollider>();
        mapChunkObject.transform.position = new Vector3(x * size * scale, 0, y * size * scale);

        quadrants = new Dictionary<Vector2, Quadrant>();
        createQuadrants();
    }

    #endregion


    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- METHODS -------------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #region METHODS

    public void setVisible(bool visibility)
    {
        if (isVisible == visibility)
            return;

        isVisible = visibility;
        mapChunkObject.SetActive(visibility);
    }


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
}
