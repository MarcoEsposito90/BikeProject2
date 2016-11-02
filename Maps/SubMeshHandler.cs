using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SubMeshHandler : MonoBehaviour {

    public GameObject[] children;
    public Material[] materials;
    private bool hasHeightMap;
    private int texturesSize;

    #region UNITY

    // Use this for initialization
    void Start () {

        hasHeightMap = false;
        texturesSize = EndlessTerrainGenerator.sectorSize + 1;



    }

    // Update is called once per frame
    void Update () {
	
	}

    #endregion



    #region METHODS

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
}
