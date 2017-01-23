using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Threading;

public class NoiseGenerator
{
    /* -------------------------------------------------------------------------------- */
    /* -------------------------- INSTANCE -------------------------------------------- */
    /* -------------------------------------------------------------------------------- */


    #region INSTANCE

    public static bool initialized { get; private set; }
    public static NoiseGenerator Instance { get; private set; }

    public delegate void SectorChanged(Vector2 position);
    public event SectorChanged OnSectorChanged;
    public event SectorChanged OnSectorCreated;

    #endregion


    /* -------------------------------------------------------------------------------- */
    /* -------------------------- ATTRIBUTES ------------------------------------------ */
    /* -------------------------------------------------------------------------------- */

    #region ATTRIBUTES

    public float noiseScale { get; private set; }
    public float offsetX { get; private set; }
    public float offsetY { get; private set; }
    public int frequencies { get; private set; }
    public float frequencyMultiplier { get; private set; }
    public float amplitudeDemultiplier { get; private set; }
    private float maxValue;
    private int sectorSize;
    private int scale;

    #endregion

    /* -------------------------------------------------------------------------------- */
    /* -------------------------- DATA ------------------------------------------------ */
    /* -------------------------------------------------------------------------------- */

    #region DATA

    private Dictionary<Vector2, float[,]> heightMaps;
    public float[,] this[Vector2 key]
    {
        get
        {
            return heightMaps[key];
        }
        private set
        {
            heightMaps[key] = value;
        }
    }

    private List<Vector2> notOriginalSectors;

    #endregion


    /* -------------------------------------------------------------------------------- */
    /* -------------------------- INITIALIZE ------------------------------------------ */
    /* -------------------------------------------------------------------------------- */

    #region INITIALIZE

    public static void Initialize(
        float noiseScale,
        int frequencies,
        float frequencyMultiplier,
        float amplitudeDemultiplier,
        float offsetX,
        float offsetY)
    {
        if (initialized) return;
        Instance = new NoiseGenerator(
            noiseScale,
            frequencies,
            frequencyMultiplier,
            amplitudeDemultiplier,
            offsetX,
            offsetY);
        initialized = true;
    }


    /* ----------------------------------------------------------------------------------------- */
    private NoiseGenerator(
        float noiseScale,
        int frequencies,
        float frequencyMultiplier,
        float amplitudeDemultiplier,
        float offsetX,
        float offsetY)
    {
        if (noiseScale <= 0)
            noiseScale = 0.0001f;

        if (amplitudeDemultiplier <= 1)
            amplitudeDemultiplier = 1.0f;

        if (frequencyMultiplier <= 1)
            frequencyMultiplier = 1.0f;

        this.noiseScale = noiseScale;
        this.frequencies = frequencies;
        this.frequencyMultiplier = frequencyMultiplier;
        this.amplitudeDemultiplier = amplitudeDemultiplier;
        this.offsetX = offsetX;
        this.offsetY = offsetY;
        this.maxValue = getMaxValue(frequencies, amplitudeDemultiplier);

        heightMaps = new Dictionary<Vector2, float[,]>();
        notOriginalSectors = new List<Vector2>();
        sectorSize = (int)GlobalInformation.Instance.getData(EndlessTerrainGenerator.SECTOR_SIZE);
        scale = (int)GlobalInformation.Instance.getData(EndlessTerrainGenerator.SCALE);
    }


    /* ----------------------------------------------------------------------------------------- */
    private float getMaxValue(int frequencies, float amplitudeDemultiplier)
    {
        float maxValue = 0.0f;
        float fraction = 1.0f;
        for (int i = 0; i < frequencies; i++)
        {
            maxValue += 1.0f / fraction;
            fraction *= amplitudeDemultiplier;
        }

        return maxValue;
    }


    #endregion


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- MAP GENERATION ----------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region METHODS

    public float[,] GenerateNoiseMap(Vector2 gridPosition)
    {
        if (heightMaps.ContainsKey(gridPosition))
            return heightMaps[gridPosition];

        //int sectorSize = (int)GlobalInformation.Instance.getData(EndlessTerrainGenerator.SECTOR_SIZE);
        int mapSize = sectorSize + 1;

        float[,] noiseMap = new float[mapSize, mapSize];
        int half = (int)(mapSize / 2.0f) + 1;
        int xCoord = (int)gridPosition.x * sectorSize;
        int yCoord = (int)gridPosition.y * sectorSize;

        // map calculation ---------------------------------------------------------
        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                int X = xCoord - half + x;
                int Y = yCoord + half - y;
                float sampleValue = getNoiseValue(1.0f, X, Y);
                noiseMap[x, y] = sampleValue;

            }
        }

        lock (heightMaps)
        {
            heightMaps.Add(gridPosition, noiseMap);
        }

        OnSectorCreated(gridPosition);

        return noiseMap;
    }


    /* ----------------------------------------------------------------------------------------- */
    public void removeNoiseMap(Vector2 sectorgridPosition)
    {
        lock (heightMaps)
        {
            if (!heightMaps.ContainsKey(sectorgridPosition))
                return;

            heightMaps.Remove(sectorgridPosition);

            if (notOriginalSectors.Contains(sectorgridPosition))
                notOriginalSectors.Remove(sectorgridPosition);
        }
    }

    /* ----------------------------------------------------------------------------------------- */
    public float getNoiseValue(float scaleMultiply, float x, float y)
    {
        if (scaleMultiply == 0)
            return 0;

        //int sectorSize = (int)GlobalInformation.Instance.getData(EndlessTerrainGenerator.SECTOR_SIZE);

        int gridX = GeometryUtilities.roundToInt(x / (float)sectorSize);
        int gridY = GeometryUtilities.roundToInt(y / (float)sectorSize);
        Vector2 gridPos = new Vector2(gridX, gridY);

        if (notOriginalSectors.Contains(gridPos))
        {
            lock (heightMaps)
            {
                if (heightMaps.ContainsKey(gridPos))
                    return valueFromHeightMap(gridPos, x, y);
            }
        }


        float sampleX = (x + offsetX) / (noiseScale * scaleMultiply);
        float sampleY = (y + offsetY) / (noiseScale * scaleMultiply);

        float sampleValue = valueFromPerlin(sampleX, sampleY, frequencies, frequencyMultiplier, amplitudeDemultiplier);
        sampleValue = sampleValue / maxValue;
        return sampleValue;
    }


    /* ----------------------------------------------------------------------------------------- */
    private float valueFromHeightMap(Vector2 gridPos, float x, float y)
    {
        //int sectorSize = (int)GlobalInformation.Instance.getData(EndlessTerrainGenerator.SECTOR_SIZE);
        float[,] map = heightMaps[gridPos];

        int mapSize = sectorSize + 1;
        int half = (int)(mapSize / 2.0f);

        int leftX = (int)gridPos.x * sectorSize - half;
        int topY = (int)gridPos.y * sectorSize + half;
        float X = x - (float)leftX;
        float Y = (float)topY - y;

        // bilinear interpolation ----------------------------------------
        int X1 = (int)X;
        int X2 = X1 + 1;
        if (X2 >= mapSize) X2 = mapSize - 1;

        int Y1 = (int)Y;
        int Y2 = Y1 + 1;
        if (Y2 >= mapSize) Y2 = mapSize - 1;

        float X1Coeff = X - (float)X1;
        float Y1Coeff = Y - (float)Y1;

        float A = map[X1, Y1] * (1.0f - X1Coeff) + map[X2, Y1] * X1Coeff;
        float B = map[X1, Y2] * (1.0f - X1Coeff) + map[X2, Y2] * X1Coeff;
        float res = A * Y1Coeff + B * (1.0f - Y1Coeff);
        return res;
    }


    /* ----------------------------------------------------------------------------------------- */
    private float valueFromPerlin(float x, float y, int frequencies, float frequencyMultiplier, float amplitudeDemultiplier)
    {
        float sampleValue = 0.0f;

        float amplitudeFraction = 1.0f;

        for (int i = 0; i < frequencies; i++)
        {
            float noiseValue = Mathf.PerlinNoise(x, y);
            sampleValue += noiseValue / amplitudeFraction;

            x = x * frequencyMultiplier;
            y = y * frequencyMultiplier;
            amplitudeFraction *= (float)amplitudeDemultiplier;
        }

        return sampleValue;
    }

    #endregion

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- OTHER METHODS ------------------------------------------------ */
    /* ----------------------------------------------------------------------------------------- */

    #region OTHERS

    public float highestPointOnSegment(Vector2 from, Vector2 to, float noiseScale, int precision)
    {
        float n = 0;
        Vector2 dist = to - from;
        if (precision < 1) precision = 1;
        if (precision > 20) precision = 20;
        float increment = 1.0f / (float)precision;

        for (float j = 0; j < 1 + increment; j += increment)
        {
            Vector2 pos = from + j * dist;
            float temp = getNoiseValue(noiseScale, pos.x, pos.y);
            if (temp > n)
                n = temp;
        }

        return n;
    }


    /* ----------------------------------------------------------------------------------------- */
    public float highestPointOnZone(Vector2 position, float scaleMultiply, float radius, int precision)
    {
        float x = position.x;
        float y = position.y;
        float currentMax = 0;
        if (precision < 1) precision = 1;
        else if (precision > 20) precision = 20;
        float increment = 1.0f / (float)precision;

        radius = 0;
        for (float i = -radius; i <= radius; i += increment)
        {
            for (float j = -radius; j <= radius; j += increment)
            {
                float n = getNoiseValue(scaleMultiply, x + i, y + j);
                if (n > currentMax)
                    currentMax = n;
            }
        }

        return currentMax;
    }

    #endregion


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- REDRAW ------------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    public void redrawRequest(Vector2 worldPosition, float radius)
    {
        int X = GeometryUtilities.roundToInt(worldPosition.x / (float)(scale * sectorSize));
        int Y = GeometryUtilities.roundToInt(worldPosition.y / (float)(scale * sectorSize));
        int r = GeometryUtilities.roundToInt(radius / (float)scale);

        float unscaledX = worldPosition.x / (float)scale;
        float unscaledY = worldPosition.y / (float)scale;

        Vector3 sizes = new Vector3(sectorSize, 0, sectorSize);

        for (int i = -1; i <= 1; i++)
            for (int j = -1; j <= 1; j++)
            {
                Vector2 other = new Vector2(X + i, Y + j);
                Vector3 center = new Vector3(other.x * sectorSize, 0, other.y * sectorSize);
                Bounds b = new Bounds(center, sizes);
                Vector3 p = new Vector3(worldPosition.x / (float)scale, 0, worldPosition.y / (float)scale);
                float dist = b.SqrDistance(p);
                dist = Mathf.Sqrt(dist);

                if (dist <= (r * 2))
                {
                    flattenSector(other, unscaledX, unscaledY, r);
                    notOriginalSectors.Add(other);
                    OnSectorChanged(other);
                }
            }
    }


    /* ----------------------------------------------------------------------------------------- */
    private void flattenSector(Vector2 gridPos, float x, float y, int r)
    {
        int centerX = (int)((x - (gridPos.x - 0.5f) * sectorSize));
        int centerY = (int)(((gridPos.y + 0.5f) * sectorSize - y));
        float n = getNoiseValue(1, x, y);

        heightMaps[gridPos] = ImageProcessing.radialFlattening(
            heightMaps[gridPos],
            r,
            centerX + 1,
            centerY + 1,
            n);

    }
}
