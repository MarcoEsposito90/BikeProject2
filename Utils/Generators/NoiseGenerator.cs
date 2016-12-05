using UnityEngine;
using System.Collections;

public class NoiseGenerator
{
    /* -------------------------------------------------------------------------------- */
    /* -------------------------- INSTANCE -------------------------------------------- */
    /* -------------------------------------------------------------------------------- */


    #region INSTANCE

    public static bool initialized { get; private set; }
    public static NoiseGenerator Instance { get; private set; }

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
    /* -------------------------- METHODS ------------------------------------------------------ */
    /* ----------------------------------------------------------------------------------------- */

    #region METHODS

    public float[,] GenerateNoiseMap
        (int mapSize,
        float scaleMultiply,
        float topLeftXCoord,
        float topLeftYCoord)
    {

        float[,] noiseMap = new float[mapSize, mapSize];

        // map calculation ---------------------------------------------------------
        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                float sampleValue = getNoiseValue(scaleMultiply, topLeftXCoord + x, topLeftYCoord - y);
                noiseMap[x, y] = sampleValue;
            }
        }

        return noiseMap;
    }


    /* ----------------------------------------------------------------------------------------- */
    public float getNoiseValue(float scaleMultiply, float x, float y)
    {
        if (scaleMultiply == 0)
            return 0;

        float sampleX = (x + offsetX) / (noiseScale * scaleMultiply);
        float sampleY = (y + offsetY) / (noiseScale * scaleMultiply);

        float sampleValue = computeValue(sampleX, sampleY, frequencies, frequencyMultiplier, amplitudeDemultiplier);
        sampleValue = sampleValue / maxValue;
        return sampleValue;
    }


    /* ----------------------------------------------------------------------------------------- */
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
    /* -------------------------- STATICS ------------------------------------------------------ */
    /* ----------------------------------------------------------------------------------------- */

    #region STATICS



    /* ----------------------------------------------------------------------------------------- */
    private static float computeValue(float x, float y, int frequencies, float frequencyMultiplier, float amplitudeDemultiplier)
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
}
