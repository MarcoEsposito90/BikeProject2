using UnityEngine;
using System.Collections;

public class RoadHandler : MonoBehaviour {

    //public Material roadMaterial;

    public void SetMesh(Mesh mesh, Texture2D texture)
    {
        GetComponent<MeshFilter>().mesh = mesh;
        //GetComponent<MeshRenderer>().material = new Material(roadMaterial);
        //GetComponent<MeshRenderer>().material.mainTexture = texture;
        GetComponent<MeshCollider>().sharedMesh = mesh;
        GetComponent<MeshCollider>().enabled = true;
    }

    public void reset()
    {
        GetComponent<MeshFilter>().mesh = null;
        GetComponent<MeshCollider>().sharedMesh = null;
        GetComponent<MeshCollider>().enabled = false;
    }
}
