using UnityEngine;
using System.Collections;

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

    public void computeControlPoint()
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
        this.roadsControlPoint = new ControlPoint(center, ControlPoint.Type.Center, localPosition, position, this);
    }

    #endregion
}
