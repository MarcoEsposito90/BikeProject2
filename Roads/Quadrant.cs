using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class Quadrant
{

    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- ATTRIBUTES ----------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #region ATTRIBUTES

    public enum Neighborhood { Quad, Octo };

    public int width
    {
        get;
        private set;
    }

    public int height
    {
        get;
        private set;
    }

    public int subdivisions
    {
        get;
        private set;
    }

    public Vector2 localPosition
    {
        get;
        private set;
    }

    public Vector2 position
    {
        get;
        private set;
    }

    //public ControlPoint roadsControlPoint
    //{
    //    get;
    //    set;
    //}

    #endregion


    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- CONSTRUCTOR ---------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #region CONTRUCTORS

    public Quadrant(Vector2 localPosition, Vector2 position, int width, int height, int subdivisions)
    {
        this.localPosition = localPosition;
        this.position = position;
        this.width = width;
        this.height = height;
        this.subdivisions = subdivisions;

        //computeControlPoint();
    }

    #endregion


    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- METHODS -------------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #region METHODS

    //private void computeControlPoint()
    //{
    //    float seedX = EndlessTerrainGenerator.seedX;
    //    float seedY = EndlessTerrainGenerator.seedY;
    //    int size = EndlessTerrainGenerator.sectorSize;

    //    // 2) get perlin value relative to coordinates ----------------
    //    float randomX = Mathf.PerlinNoise(position.x + seedX, position.y + seedX);
    //    float randomY = Mathf.PerlinNoise(position.x + seedY, position.y + seedY);
    //    // NOTE: randomX and randomY are in [0,1]

    //    // 3) compute absolute coordinates of point in space ----------
    //    int X = Mathf.RoundToInt(randomX * (float)width + position.x * size);
    //    int Y = Mathf.RoundToInt(randomY * (float)height + position.y * size);

    //    // 4) create point  --------------------------
    //    Vector2 center = new Vector2(X, Y);
    //    //this.roadsControlPoint = new ControlPoint(center);
    //}


    /* ------------------------------------------------------------------------------------------------- */
    //public List<ControlPoint> getNeighborsPoints(Neighborhood neighborhood)
    //{
    //    // returns the list of neighbors, ordered by growing distance
    //    List<ControlPoint> neighbors = new List<ControlPoint>();

    //    foreach (Quadrant q in this.getNeighbors(neighborhood))
    //    {
    //        float currentDistance = Vector2.Distance(q.roadsControlPoint.position, roadsControlPoint.position);

    //        neighbors.Add(q.roadsControlPoint);
    //        neighbors.Sort(new ControlPoint.NearestPointComparer(roadsControlPoint));
    //    }

    //    return neighbors;
    //}


    /* ------------------------------------------------------------------------------------------------- */
    public List<Quadrant> getNeighbors(Neighborhood neighborhood)
    {
        List<Quadrant> quadrants = new List<Quadrant>();

        for (int i = -1; i <= 1; i++)
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;

                bool isCorner = (i == -1 && j == -1) ||
                                (i == 1 && j == 1) ||
                                (i == -1 && j == 1) ||
                                (i == 1 && j == -1);

                if (isCorner && neighborhood.Equals(Neighborhood.Quad))
                    continue;

                float quadrantX = (position.x + (1.0f / (float)subdivisions) * i);
                float quadrantY = (position.y + (1.0f / (float)subdivisions) * j);
                Vector2 neighborQuadrantPos = new Vector2(quadrantX, quadrantY);

                Quadrant neighbor = new Quadrant(Vector2.zero, neighborQuadrantPos, width, height, subdivisions);
                quadrants.Add(neighbor);
            }

        return quadrants;
    }


    /* ------------------------------------------------------------------------------------------------- */
    //public static ControlPoint computeControlPoint(Vector2 position, int quadrantWidth, int quadrantHeight)
    //{
    //    float seedX = EndlessTerrainGenerator.seedX;
    //    float seedY = EndlessTerrainGenerator.seedY;
    //    int size = EndlessTerrainGenerator.sectorSize;

    //    // 2) get perlin value relative to coordinates ----------------
    //    float randomX = Mathf.PerlinNoise(position.x + seedX, position.y + seedX);
    //    float randomY = Mathf.PerlinNoise(position.x + seedY, position.y + seedY);

    //    // 3) compute absolute coordinates of point in space ----------
    //    int X = Mathf.RoundToInt(randomX * (float)quadrantWidth + position.x * size);
    //    int Y = Mathf.RoundToInt(randomY * (float)quadrantHeight + position.y * size);

    //    // 4) create point  --------------------------
    //    Vector2 center = new Vector2(X, Y);
    //    return new ControlPoint(center);
    //}




    #endregion


    
}
