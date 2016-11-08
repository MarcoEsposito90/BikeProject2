using UnityEngine;
using System.Collections;
using System;

public class Road {

    public ICurve curve;
    public MapSector sector;
    public GameObject prefab;
    public int scale;


    public Road(MapSector sector, ICurve curve, GameObject prefab, int scale)
    {
        this.sector = sector;
        this.curve = curve;
        this.prefab = prefab;
        this.scale = scale;
        Vector2 start = curve.startPoint();
        this.prefab.name = "road " + start + " - " + curve.endPoint();
        this.prefab.transform.position = new Vector3(start.x * scale, 0, start.y * scale);
        this.prefab.transform.localScale = new Vector3(scale, scale, scale);
        this.prefab.SetActive(true);
    }


    public void setMesh(Mesh mesh)
    {
        prefab.GetComponent<MeshFilter>().mesh = mesh;
    }


    /* ----------------------------------------------------------------------------------- */
    public class Key
    {
        public readonly Vector2 start;
        public readonly Vector2 end;

        public Key(Vector2 start, Vector2 end)
        {
            this.start = start;
            this.end = end;
        }
        
    }


    /* ----------------------------------------------------------------------------------- */
    public class RoadData
    {
        public RoadMeshGenerator.RoadMeshData meshData;
        public Key key;
        public MapSector sector;
        public ICurve curve;

        public RoadData(RoadMeshGenerator.RoadMeshData meshData, Key key, MapSector sector, ICurve curve)
        {
            this.key = key;
            this.meshData = meshData;
            this.sector = sector;
            this.curve = curve;
        }
    }
}
