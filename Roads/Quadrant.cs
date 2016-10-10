using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Quadrant
{

    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- ATTRIBUTES ----------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #region ATTRIBUTES

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

    public ControlPoint roadsControlPoint
    {
        get;
        set;
    }

    public MapChunk parent
    {
        get;
        private set;
    }

    #endregion


    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- CONSTRUCTOR ---------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #region CONTRUCTORS

    public Quadrant(Vector2 localPosition, Vector2 position, int width, int height, MapChunk parent)
    {
        this.localPosition = localPosition;
        this.position = position;
        this.width = width;
        this.height = height;
        this.parent = parent;

        computeControlPoint();
    }

    #endregion


    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- METHODS -------------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #region METHODS

    private void computeControlPoint()
    {
        float seedX = EndlessTerrainGenerator.seedX;
        float seedY = EndlessTerrainGenerator.seedY;
        int size = EndlessTerrainGenerator.chunkSize;

        // 2) get perlin value relative to coordinates ----------------
        float randomX = Mathf.PerlinNoise(position.x + seedX, position.y + seedX);
        float randomY = Mathf.PerlinNoise(position.x + seedY, position.y + seedY);

        // 3) compute absolute coordinates of point in space ----------
        int X = Mathf.RoundToInt(randomX * (float)width + position.x * size);
        int Y = Mathf.RoundToInt(randomY * (float)height + position.y * size);

        // 4) create point  --------------------------
        Vector2 center = new Vector2(X, Y);
        this.roadsControlPoint = new ControlPoint(center, ControlPoint.Type.Center);
    }


    /* ------------------------------------------------------------------------------------------------- */
    public List<ControlPoint> getNeighbors()
    {
        // returns the list of neighbors, ordered by growing distance

        List<ControlPoint> neighbors = new List<ControlPoint>();

        for (int i = -1; i <= 1; i++)
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;

                float quadrantX = (position.x + (1.0f / (float)parent.subdivisions) * i);
                float quadrantY = (position.y + (1.0f / (float)parent.subdivisions) * j);
                Vector2 neighborQuadrantPos = new Vector2(quadrantX, quadrantY);

                ControlPoint cp = Quadrant.computeControlPoint(neighborQuadrantPos, width, height);
                float currentDistance = Vector2.Distance(cp.position, roadsControlPoint.position);

                if (neighbors.Count == 0)
                    neighbors.Add(cp);
                else
                    for (int k = 0; k < neighbors.Count; k++)
                    {
                        float distance = Vector2.Distance(neighbors[k].position, roadsControlPoint.position);
                        if(currentDistance < distance)
                        {
                            neighbors.Insert(k, cp);
                            break;
                        }
                    }
            }

        return neighbors;
    }


    /* ------------------------------------------------------------------------------------------------- */
    public static ControlPoint computeControlPoint(Vector2 position, int quadrantWidth, int quadrantHeight)
    {
        float seedX = EndlessTerrainGenerator.seedX;
        float seedY = EndlessTerrainGenerator.seedY;
        int size = EndlessTerrainGenerator.chunkSize;

        // 2) get perlin value relative to coordinates ----------------
        float randomX = Mathf.PerlinNoise(position.x + seedX, position.y + seedX);
        float randomY = Mathf.PerlinNoise(position.x + seedY, position.y + seedY);

        // 3) compute absolute coordinates of point in space ----------
        int X = Mathf.RoundToInt(randomX * (float)quadrantWidth + position.x * size);
        int Y = Mathf.RoundToInt(randomY * (float)quadrantHeight + position.y * size);

        // 4) create point  --------------------------
        Vector2 center = new Vector2(X, Y);
        return new ControlPoint(center, ControlPoint.Type.Center);
    }

    #endregion
}
