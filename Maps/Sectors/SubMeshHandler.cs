using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SubMeshHandler : MonoBehaviour {

    public GameObject[] children;

    public bool waterEnabled;
    public GameObject water;
    public Material[] materials;

    [Range(0, 10)]
    public int minimumLODObjectsVisibility;

    private bool hasHeightMap;
    private int texturesSize;
    private int sectorDimension;

    private int _currentLOD;
    public int currentLOD
    {
        get { return _currentLOD; }
        set
        {
            _currentLOD = value;
            //Debug.Log("currentLOD = " + _currentLOD + ". setting visibility to " + (_currentLOD <= minimumLODObjectsVisibility));
            setObjectsVisibility(_currentLOD <= minimumLODObjectsVisibility);
        }
    }

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- UNITY -------------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region UNITY

    /* ----------------------------------------------------------------------------------------- */
    void Start ()
    {
        int sectorSize = (int)GlobalInformation.Instance.getData(EndlessTerrainGenerator.SECTOR_SIZE);
        texturesSize = sectorSize + 1;
        Vector3 scale = water.transform.localScale;
        scale *= 4;
        water.transform.localScale = scale;
    }


    /* ----------------------------------------------------------------------------------------- */
    void OnEnable()
    {
        hasHeightMap = false;
        setObjectsVisibility(false);
    }


    /* ----------------------------------------------------------------------------------------- */
    void Update ()
    {

    }

    #endregion

    /* ----------------------------------------------------------------------------------------- */
    /* ----------------------- MESHES AND TEXTURES --------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region MESHES_TEXTURES

    /* ------------------------------------------------------------------------------------------- */
    public void setMeshes(Mesh colliderMesh, Mesh mesh)
    {

        if (colliderMesh != null)
        {
            GetComponent<MeshCollider>().sharedMesh = colliderMesh;
            GetComponent<MeshCollider>().enabled = true;
        }
        else
            GetComponent<MeshCollider>().enabled = false;

        foreach (GameObject c in children)
            c.GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    


    /* ------------------------------------------------------------------------------------------- */
    public void setTextures(Color[] heightMap, List<Color[]> alphaMaps, int textureSize)
    {
        if (hasHeightMap)
            return;

        this.texturesSize = textureSize;
        Texture2D heightMapTexture = new Texture2D(texturesSize, texturesSize);
        heightMapTexture.wrapMode = TextureWrapMode.Clamp;
        heightMapTexture.SetPixels(heightMap);
        heightMapTexture.Apply();

        for (int i = 0; i < children.Length; i++)
        {

            Material mat = new Material(materials[i]);
            children[i].GetComponent<Renderer>().material = mat;
            mat.SetTexture("_HeightMap", heightMapTexture);

            Texture2D alphaMapTexture = new Texture2D(texturesSize, texturesSize);
            alphaMapTexture.SetPixels(alphaMaps[i]);
            alphaMapTexture.wrapMode = TextureWrapMode.Clamp;
            alphaMapTexture.Apply();
            mat.SetTexture("_AlphaMap", alphaMapTexture);
        }

        hasHeightMap = true;
    }


    /* ------------------------------------------------------------------------------------------- */
    public void reset()
    {
        GetComponent<MeshCollider>().sharedMesh = null;
        GetComponent<MeshCollider>().enabled = false;

        foreach (GameObject c in children)
            c.GetComponent<MeshFilter>().sharedMesh = null;

        hasHeightMap = false;
    }

    #endregion


    /* ----------------------------------------------------------------------------------------- */
    /* ----------------------- OBJECTS HANDLING ------------------------------------------------ */
    /* ----------------------------------------------------------------------------------------- */

    private void setObjectsVisibility(bool visibility)
    {
        if(waterEnabled)
            water.SetActive(visibility);
    }
}
