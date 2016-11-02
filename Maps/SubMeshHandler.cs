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

        //for (int y = 0; y < textureSize; y++)
        //    for (int x = 0; x < textureSize; x++)
        //        heightMapTexture.SetPixel(x, y, heightMap[y * textureSize + x]);

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

            Texture2D alphaMapTexture = new Texture2D(textureSize, textureSize);
            alphaMapTexture.SetPixels(alphaMaps[i]);
            alphaMapTexture.wrapMode = TextureWrapMode.Clamp;
            alphaMapTexture.Apply();
            mat.SetTexture("_AlphaMap", alphaMapTexture);
        }

        hasHeightMap = true;
    }


    /* ------------------------------------------------------------------------------------------- */
    //public void setAlphaTextures(List<Color[]> alphaMaps, int textureSize)
    //{
    //    if (index > children.Length)
    //    {
    //        Debug.Log("ERROR! trying to set textures on unknown child (SubMeshHandler.setSubTexture)");
    //        return;
    //    }

    //    Material mat = children[index].GetComponent<Renderer>().sharedMaterial;
    //    Texture2D alphaMapTexture = new Texture2D(textureSize, textureSize);
    //    alphaMapTexture.SetPixels(alphaMap);
    //    alphaMapTexture.wrapMode = TextureWrapMode.Clamp;
    //    mat.SetTexture("_AlphaMap", alphaMapTexture);

    //}

    /* ------------------------------------------------------------------------------------------- */
    public void reset()
    {
        GetComponent<MeshCollider>().sharedMesh = null;
        GetComponent<MeshCollider>().enabled = false;

        foreach (GameObject c in children)
        {
            c.GetComponent<MeshFilter>().sharedMesh = null;
            //c.GetComponent<Renderer>().material.SetTexture("_HeightMap", new Texture2D(texturesSize, texturesSize));
            //c.GetComponent<Renderer>().material.SetTexture("_AlphaMap", new Texture2D(texturesSize, texturesSize));
        }

        hasHeightMap = false;
    }


    #endregion
}
