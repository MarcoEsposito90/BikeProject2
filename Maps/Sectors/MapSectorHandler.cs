﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MapSectorHandler : MonoBehaviour {

    public GameObject terrain;

    public bool waterEnabled;
    public GameObject water;
    public Material[] materials;

    [Range(0, 10)]
    public int minimumLODObjectsVisibility;

    private bool hasHeightMap;
    private int texturesSize;
    private int sectorDimension;
    private Texture2D texture;
    private Material material;

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
        hasHeightMap = false;
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
            terrain.GetComponent<MeshCollider>().sharedMesh = colliderMesh;
            terrain.GetComponent<MeshCollider>().enabled = true;
        }
        else
            terrain.GetComponent<MeshCollider>().enabled = false;

        terrain.GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    


    /* ------------------------------------------------------------------------------------------- */
    public void setTextures(Color[] heightMap)
    {
        if (hasHeightMap)
            return;

        if(texture == null)
        {
            int sectorSize = (int)GlobalInformation.Instance.getData(EndlessTerrainGenerator.SECTOR_SIZE);
            texturesSize = sectorSize + 1;
            texture = new Texture2D(texturesSize, texturesSize);
            texture.wrapMode = TextureWrapMode.Clamp;
        }

        if(material == null)
            material = new Material(materials[0]);

        texture.SetPixels(heightMap);
        texture.Apply();

        terrain.GetComponent<Renderer>().material = material;
        material.SetTexture("_HeightMap", texture);

        hasHeightMap = true;
    }


    /* ------------------------------------------------------------------------------------------- */
    public void reset()
    {
        terrain.GetComponent<MeshCollider>().sharedMesh = null;
        terrain.GetComponent<MeshCollider>().enabled = false;
        terrain.GetComponent<MeshFilter>().sharedMesh = null;
        hasHeightMap = false;
    }

    #endregion


    /* ----------------------------------------------------------------------------------------- */
    /* ----------------------- OBJECTS HANDLING ------------------------------------------------ */
    /* ----------------------------------------------------------------------------------------- */

    private void setObjectsVisibility(bool visibility)
    {
        if (waterEnabled)
        {
            float wl = (float)GlobalInformation.Instance.getData(EndlessTerrainGenerator.WATER_LEVEL);
            float h = GlobalInformation.Instance.getHeight(wl);
            water.transform.position = new Vector3(
                water.transform.position.x,
                h,
                water.transform.position.z);
            water.SetActive(visibility);
        }
    }
}
