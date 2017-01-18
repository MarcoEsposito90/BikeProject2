﻿using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

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

        int sectorSize = (int)GlobalInformation.Instance.getData(EndlessTerrainGenerator.SECTOR_SIZE);
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

        OnSectorChanged(gridPosition);

        return noiseMap;
    }


    /* ----------------------------------------------------------------------------------------- */
    public float getNoiseValue(float scaleMultiply, float x, float y)
    {
        if (scaleMultiply == 0)
            return 0;

        int sectorSize = (int)GlobalInformation.Instance.getData(EndlessTerrainGenerator.SECTOR_SIZE);

        int gridX = GeometryUtilities.roundToInt(x / (float)sectorSize);
        int gridY = GeometryUtilities.roundToInt(y / (float)sectorSize);
        Vector2 gridPos = new Vector2(gridX, gridY);

        lock (heightMaps)
        {
            if (heightMaps.ContainsKey(gridPos))
                return valueFromHeightMap(gridPos, x, y);
        }

        float sampleX = (x + offsetX) / (noiseScale * scaleMultiply);
        float sampleY = (y + offsetY) / (noiseScale * scaleMultiply);

        float sampleValue = computeValue(sampleX, sampleY, frequencies, frequencyMultiplier, amplitudeDemultiplier);
        sampleValue = sampleValue / maxValue;
        return sampleValue;
    }


    /* ----------------------------------------------------------------------------------------- */
    private float valueFromHeightMap(Vector2 gridPos, float x, float y)
    {
        int sectorSize = (int)GlobalInformation.Instance.getData(EndlessTerrainGenerator.SECTOR_SIZE);
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
    private float computeValue(float x, float y, int frequencies, float frequencyMultiplier, float amplitudeDemultiplier)
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



}
