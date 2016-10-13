using UnityEngine;
using System.Collections;

public static class Noise
{

    public static float[,] GenerateNoiseMap
        (int mapWidth,
        int mapHeight,
        float scale,
        float offsetX,
        float offsetY,
        int frequencies,
        float frequencyMultiplier,
        float amplitudeDemultiplier)
    {

        // initialization and checks --------------------------------------------------
        float[,] noiseMap = new float[mapWidth, mapHeight];

        if (scale <= 0)
            scale = 0.0001f;

        if (amplitudeDemultiplier <= 1)
            amplitudeDemultiplier = 1.0f;

        if (frequencyMultiplier <= 1)
            frequencyMultiplier = 1.0f;


        // map calculation ---------------------------------------------------------
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float sampleX = (x + offsetX) / scale;
                float sampleY = -(y - offsetY) / scale;

                float sampleValue = computeValue(sampleX, sampleY, frequencies, frequencyMultiplier, amplitudeDemultiplier);

                //float amplitudeFraction = 1.0f;

                //for (int i = 0; i < frequencies; i++)
                //{
                //    float noiseValue = Mathf.PerlinNoise(sampleX, sampleY);
                //    sampleValue += noiseValue / amplitudeFraction;

                //    sampleX = (x + offsetX) * frequencyMultipler / scale;
                //    sampleY = (y - offsetY) * frequencyMultipler / scale;
                //    amplitudeFraction *= (float)amplitudeDemultiplier;
                //}

                noiseMap[x, y] = sampleValue;
            }
        }

        // normalization -------------------------------------------------------------
        float maxValue = getMaxValue(frequencies, amplitudeDemultiplier);
        for (int y = 0; y < mapHeight; y++)
            for (int x = 0; x < mapWidth; x++)
                noiseMap[x, y] = noiseMap[x, y] / maxValue;

        return noiseMap;
    }

    public static float getNoiseValue
        (float scale,
        float x,
        float y,
        int frequencies,
        float frequencyMultiplier,
        float amplitudeDemultiplier)
    {

        float sampleX = x / scale;
        float sampleY = -y / scale;

        //float amplitudeFraction = 1.0f;

        //for (int i = 0; i < frequencies; i++)
        //{
        //    float noiseValue = Mathf.PerlinNoise(sampleX, sampleY);
        //    sampleValue += noiseValue / amplitudeFraction;

        //    sampleX = x * frequencyMultipler / scale;
        //    sampleY = y * frequencyMultipler / scale;
        //    amplitudeFraction *= (float)amplitudeDemultiplier;
        //}

        float sampleValue = computeValue(sampleX, sampleY, frequencies, frequencyMultiplier, amplitudeDemultiplier);
        sampleValue = sampleValue / getMaxValue(frequencies, amplitudeDemultiplier);
        return sampleValue;
    }



    private static float getMaxValue(int frequencies, float amplitudeDemultiplier)
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
}
