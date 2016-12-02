﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class ControlPoint
{

    //public enum Type { Center, Tangent };


    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- ATTRIBUTES ----------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    public enum Neighborhood { Quad, Octo };

    #region ATTRIBUTES

    public Vector2 gridPosition { get; private set; }
    public Vector2 position { get; private set; }
    public float height { get; private set; }

    public float AreaSize;
    public Bounds bounds;
    public int scale;
    public bool linkable;

    private float scaledAreaSize;
    public GameObject prefabObject;

    #endregion


    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- CONSTRUCTOR ---------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    public ControlPoint(Vector2 gridPosition, GameObject prefab, float size, int scale)
    {
        this.gridPosition = gridPosition;
        this.AreaSize = size;
        this.scale = scale;
        this.prefabObject = prefab;

        Vector3 boundsCenter = new Vector3(gridPosition.x, 0, gridPosition.y) * scale * AreaSize;
        Vector3 boundsSizes = new Vector3(AreaSize * scale, AreaSize * scale, AreaSize * scale);
        this.bounds = new Bounds(boundsCenter, boundsSizes);

        computePosition();
        initializePrefab();
    }


    /* ------------------------------------------------------------------------------------------ */
    /* ------------------------------ METHODS --------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------ */

    private void computePosition()
    {
        float seedX = (float)GlobalInformation.Instance.getData(EndlessRoadsGenerator.MAP_SEEDX);
        float seedY = (float)GlobalInformation.Instance.getData(EndlessRoadsGenerator.MAP_SEEDY);

        // 2) get perlin value relative to coordinates ----------------
        float randomX = Mathf.PerlinNoise(gridPosition.x + seedX, gridPosition.y + seedX);
        float randomY = Mathf.PerlinNoise(gridPosition.x + seedY, gridPosition.y + seedY);

        // 3) compute absolute coordinates of point in space ----------
        int X = Mathf.RoundToInt((gridPosition.x + randomX) * (float)AreaSize);
        int Y = Mathf.RoundToInt((gridPosition.y + randomY) * (float)AreaSize);

        // 4) create point --------------------------------------------
        this.position = new Vector2(X, Y);

        // 5) compute height ------------------------------------------
        float y = NoiseGenerator.Instance.highestPointOnZone(position, 1, 2, 1);
        height = GlobalInformation.Instance.getHeight(y);
    }


    /* ------------------------------------------------------------------------------------------------- */
    public float distance(ControlPoint other)
    {
        return Vector2.Distance(this.position, other.position);
    }

    /* ------------------------------------------------------------------------------------------------- */
    public List<Vector2> getNeighborsGridPositions(Neighborhood neighborhood)
    {
        List<Vector2> neighborsPositions = new List<Vector2>();

        int x = (int)gridPosition.x;
        int y = (int)gridPosition.y;

        for (int i = -1; i <= 1; i++)
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;
                bool corner = i != 0 && j != 0;
                if (corner && neighborhood.Equals(Neighborhood.Quad))
                    continue;

                neighborsPositions.Add(new Vector2(x + i, y + j));
            }

        return neighborsPositions;
    }

    /* ------------------------------------------------------------------------------------------------- */
    public Vector2 getAverageVector(List<Vector2> points, bool absolute, bool normalized, bool reflectTowards, Vector2 towards)
    {
        float averageX = 0;
        float averageY = 0;
        int denom = points.Count;
        float xDist = 0;
        float yDist = 0;

        for (int k = 0; k < points.Count; k++)
        {
            xDist = points[k].x - position.x;
            yDist = points[k].y - position.y;
            averageX += xDist / (float)denom;
            averageY += yDist / (float)denom;
        }

        Vector2 mean = new Vector2(averageX, averageY);

        if(reflectTowards)
        {
            float d1 = Vector2.Distance(position, towards);
            float d2 = Vector2.Distance(position + mean, towards);

            if (d2 > d1)
                mean *= -1;
        }

        if (absolute)
            mean = position + mean;

        if (normalized)
            mean /= mean.magnitude;

        return mean;
    }

    /* ---------------------------------------------------------------------------- */
    /* ------------------------------ PREFAB -------------------------------------- */
    /* ---------------------------------------------------------------------------- */

    #region PREFAB

    /* ------------------------------------------------------------------------------------------------- */
    private void initializePrefab()
    {
        float x = position.x * scale;
        float z = position.y * scale;

        Vector3 prefabPos = new Vector3(x, height * scale, z);
        //Debug.Log(prefabPos);
        prefabObject.transform.position = prefabPos;
        prefabObject.name = "ControlPoint " + gridPosition;
        prefabObject.transform.localScale = new Vector3(scale, scale, scale);
        prefabObject.SetActive(true);
    }

    /* ------------------------------------------------------------------------------------------------- */
    public void setData(ControlPoint.ControlPointData data)
    {
        prefabObject.GetComponent<CrossroadHandler>().setData(data);
        //mesh.name = "Crossroad " + gridPosition;
        //prefabObject.GetComponent<MeshFilter>().mesh = mesh;
        //prefabObject.GetComponent<MeshCollider>().sharedMesh = mesh;
    }


    /* ------------------------------------------------------------------------------------------------- */
    public void resetPrefab()
    {
        prefabObject.transform.position = Vector3.zero;
        prefabObject.name = "ControlPoint (available)";
        prefabObject.transform.localScale = Vector3.one;
        //prefabObject.GetComponent<MeshFilter>().mesh = null;
        prefabObject.SetActive(false);
    }


    #endregion


    /* ----------------------------------------------------------------------------- */
    /* ------------------- COMPARERS ----------------------------------------------- */
    /* ----------------------------------------------------------------------------- */

    #region COMPARER

    public class NearestPointComparer : IComparer<ControlPoint>
    {
        public ControlPoint center;

        public NearestPointComparer(ControlPoint center)
        {
            this.center = center;
        }

        public int Compare(ControlPoint x, ControlPoint y)
        {
            float distanceX = Vector2.Distance(center.position, x.position);
            float distanceY = Vector2.Distance(center.position, y.position);

            if (distanceX < distanceY) return -1;
            else if (distanceX == distanceY) return 0;
            return 1;
        }
    }


    #endregion


    /* ---------------------------------------------------------------------------- */
    /* ------------------------------ OBJECT DATA --------------------------------- */
    /* ---------------------------------------------------------------------------- */

    public class ControlPointData
    {
        public readonly Vector2 gridPosition;
        public readonly Texture2D crossroadTexture;
        public readonly Texture2D roadTexture;
        public readonly CrossroadsMeshGenerator.CrossroadMeshData crossRoadMeshData;
        
        public ControlPointData(
            Vector2 gridPosition, 
            CrossroadsMeshGenerator.CrossroadMeshData crossRoadMeshData,
            Texture2D crossroadTexture,
            Texture2D roadTexture)
        {
            this.gridPosition = gridPosition;
            this.crossRoadMeshData = crossRoadMeshData;
            this.crossroadTexture = crossroadTexture;
            this.roadTexture = roadTexture;
        }
    }
}