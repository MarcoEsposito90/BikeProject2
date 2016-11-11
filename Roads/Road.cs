using UnityEngine;
using System.Collections;
using System;

public class Road {

    public ControlPoint start;
    public ControlPoint end;
    public Graph<Vector2, ControlPoint>.Link link;
    public ICurve curve;
    public GameObject prefab;
    public int scale;

    public Road(
        ICurve curve, 
        Graph<Vector2, ControlPoint>.Link link, 
        GameObject prefab, 
        int scale)
    {
        this.curve = curve;
        this.prefab = prefab;
        this.scale = scale;
        this.link = link;
        Vector2 start = curve.startPoint();
        this.prefab.name = "road " + start + " - " + curve.endPoint();
        this.prefab.transform.position = new Vector3(start.x * scale, 0, start.y * scale);
        this.prefab.transform.localScale = new Vector3(scale, scale, scale);
        this.prefab.SetActive(true);
    }


    public void setMesh(Mesh mesh)
    {
        prefab.GetComponent<MeshFilter>().mesh = mesh;
        prefab.GetComponent<MeshCollider>().sharedMesh = mesh;
        prefab.GetComponent<MeshCollider>().enabled = true;
    }

    
    /* ----------------------------------------------------------------------------------- */
    public class RoadData
    {
        public RoadMeshGenerator.RoadMeshData meshData;
        public Graph<Vector2,ControlPoint>.Link key;
        public ICurve curve;

        public RoadData(
            RoadMeshGenerator.RoadMeshData meshData,
            Graph<Vector2, ControlPoint>.Link key, 
            ICurve curve)
        {
            this.key = key;
            this.meshData = meshData;
            this.curve = curve;
        }
    }
}
