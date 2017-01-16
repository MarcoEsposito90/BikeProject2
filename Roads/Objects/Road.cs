using UnityEngine;
using System.Collections;
using System;

public class Road {

    public ControlPoint start;
    public ControlPoint end;
    public Graph<Vector2, ControlPoint>.Link link;
    public ICurve curve;
    public int numberOfSections;
    public float[] heights;
    public GameObject prefab;
    public int scale;

    public Road(
        ICurve curve, 
        Graph<Vector2, ControlPoint>.Link link, 
        GameObject prefab, 
        int scale,
        int numberOfSegments,
        float[] heights)
    {
        this.curve = curve;
        this.prefab = prefab;
        this.scale = scale;
        this.link = link;
        this.numberOfSections = numberOfSegments;
        this.heights = heights;

        initializePrefab();  
    }


    /* ----------------------------------------------------------------------------------- */
    private void initializePrefab()
    {
        Vector2 start = curve.startPoint();
        prefab.name = "road " + start + " - " + curve.endPoint();
        float height = GlobalInformation.Instance.getHeight(start) * scale;
        prefab.transform.position = new Vector3(start.x * scale, 0, start.y * scale);
        prefab.SetActive(true);
    }


    /* ----------------------------------------------------------------------------------- */
    public void setMesh(Mesh mesh, Texture2D texture)
    {
        prefab.GetComponent<RoadHandler>().SetMesh(mesh, texture);
    }


    /* ----------------------------------------------------------------------------------- */
    public void resetPrefab()
    {
        prefab.GetComponent<RoadHandler>().reset();
    }



    /* ----------------------------------------------------------------------------------- */
    public class RoadData
    {
        public MeshData meshData;
        public Texture2D texture;
        public Graph<Vector2,ControlPoint>.Link key;
        public ICurve curve;

        public RoadData
            (MeshData meshData,
            Graph<Vector2, ControlPoint>.Link key, 
            ICurve curve,
            Texture2D texture)
        {
            this.key = key;
            this.meshData = meshData;
            this.curve = curve;
            this.texture = texture;
        }
    }
}
