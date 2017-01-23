using UnityEngine;
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
    public int scale;
    public bool linkable;
    public int maximumLinks = 4;

    private float scaledAreaSize;
    public GameObject prefabObject;

    #endregion


    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- CONSTRUCTOR ---------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    public ControlPoint(Vector2 gridPosition, float size, int scale)
    {
        this.gridPosition = gridPosition;
        this.AreaSize = size;
        this.scale = scale;

        computePosition();
        computeHeight();
    }


    /* ------------------------------------------------------------------------------------------------- */
    public ControlPoint(Vector2 gridPosition, Vector2 position, float size, int scale)
    {
        this.gridPosition = gridPosition;
        this.position = position;
        this.AreaSize = size;
        this.scale = scale;

        computeHeight();
    }


    /* ------------------------------------------------------------------------------------------ */
    /* ------------------------------ METHODS --------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------ */




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
    public void initializePrefab()
    {
        float x = position.x * scale;
        float z = position.y * scale;

        Vector3 prefabPos = new Vector3(x, height * scale, z);
        prefabObject.transform.position = prefabPos;
        prefabObject.name = "ControlPoint " + gridPosition;
    }


    /* ------------------------------------------------------------------------------------------------- */
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
    }


    /* ------------------------------------------------------------------------------------------ */
    public void computeHeight()
    {
        float y = NoiseGenerator.Instance.highestPointOnZone(position, 1, 1, 1);
        //float y = NoiseGenerator.Instance.getNoiseValue(1, position.x, position.y);
        height = GlobalInformation.Instance.getHeight(y);

        float wl = (float)GlobalInformation.Instance.getData(EndlessTerrainGenerator.WATER_LEVEL);
        float waterH = GlobalInformation.Instance.getHeight(wl);
        if (height < waterH) height = waterH;
    }


    /* ------------------------------------------------------------------------------------------------- */
    public void setData(ControlPointData data)
    {
        this.prefabObject.SetActive(true);
        this.prefabObject.GetComponent<CrossroadHandler>().setData(data);
    }


    /* ------------------------------------------------------------------------------------------------- */
    public void resetPrefab()
    {
        prefabObject.transform.position = Vector3.zero;
        prefabObject.name = "ControlPoint (available)";
        prefabObject.transform.localScale = Vector3.one;
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
        public readonly CrossroadsMeshGenerator.CrossroadMeshData crossRoadMeshData;
        
        public ControlPointData(
            Vector2 gridPosition, 
            CrossroadsMeshGenerator.CrossroadMeshData crossRoadMeshData)
        {
            this.gridPosition = gridPosition;
            this.crossRoadMeshData = crossRoadMeshData;
        }
    }
}
