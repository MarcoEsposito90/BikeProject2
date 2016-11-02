using UnityEngine;
using System.Collections;

public class SubMeshHandler : MonoBehaviour {

    public GameObject[] children;
    public Material[] materials;
    private bool hasHeightMap;
    private int texturesSize;

    #region UNITY

    // Use this for initialization
    void Start () {

        hasHeightMap = false;
        texturesSize = 0;

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
    public void setHeightMap(Color[] heightMap, int textureSize)
    {
        if (hasHeightMap)
            return;

        for(int i = 0; i < children.Length; i++)
        {
            Material mat = new Material(materials[i]);
            children[i].GetComponent<Renderer>().sharedMaterial = mat;
            Texture2D heightMapTexture = new Texture2D(textureSize, textureSize);
            heightMapTexture.SetPixels(heightMap);
            heightMapTexture.wrapMode = TextureWrapMode.Clamp;
            mat.SetTexture("_HeightMap", heightMapTexture);
        }

        hasHeightMap = true;
    }


    /* ------------------------------------------------------------------------------------------- */
    public void setAlphaTexture(Color[] alphaMap, int textureSize, int index)
    {
        if (index > children.Length)
        {
            Debug.Log("ERROR! trying to set textures on unknown child (SubMeshHandler.setSubTexture)");
            return;
        }

        Material mat = children[index].GetComponent<Renderer>().sharedMaterial;
        Texture2D alphaMapTexture = new Texture2D(textureSize, textureSize);
        alphaMapTexture.SetPixels(alphaMap);
        alphaMapTexture.wrapMode = TextureWrapMode.Clamp;
        mat.SetTexture("_AlphaMap", alphaMapTexture);

    }

    /* ------------------------------------------------------------------------------------------- */
    public void reset()
    {
        GetComponent<MeshCollider>().sharedMesh = null;
        GetComponent<MeshCollider>().enabled = false;

        //foreach (GameObject c in children)
        //{
        //    c.GetComponent<MeshFilter>().sharedMesh = null;
        //    c.GetComponent<Renderer>().sharedMaterial.SetTexture("_HeightMap", null);
        //    c.GetComponent<Renderer>().sharedMaterial.SetTexture("_AlphaMap", null);
        //}
    }


    #endregion
}
