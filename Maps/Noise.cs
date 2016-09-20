using UnityEngine;
using System.Collections;

public static class Noise {

    public static float[,] GenerateNoiseMap
        (int mapWidth, 
        int mapHeight, 
        float scale,
        float offsetX,
        float offsetY,
        int frequencies,
        float frequencyMultipler,
        float amplitudeDemultiplier)
    {

        // initialization and chacks --------------------------------------------------

        float[,] noiseMap = new float[mapWidth, mapHeight];

        if (scale <= 0)
            scale = 0.0001f;

        if (amplitudeDemultiplier <= 1)
            amplitudeDemultiplier = 1.0f;

        if (frequencyMultipler <= 1)
            frequencyMultipler = 1.0f;


        // map calculation ---------------------------------------------------------
        for (int y = 0; y < mapHeight; y++)
        {
            for(int x = 0; x < mapWidth; x++)
            {
                float sampleValue = 0.0f;
                float sampleX = (x ) / scale;
                float sampleY = (y ) / scale;

                float amplitudeFraction = 1.0f;

                for (int i = 0; i < frequencies; i++)
                {
                    float noiseValue = Mathf.PerlinNoise(sampleX, sampleY);
                    sampleValue += noiseValue / amplitudeFraction;

                    sampleX = (x * frequencyMultipler) / scale;
                    sampleY = (y * frequencyMultipler) / scale;
                    amplitudeFraction *= (float)amplitudeDemultiplier;
                }

                noiseMap[x, y] = sampleValue; 
            }
        }

        // normalization -------------------------------------------------------------
        float maxValue = 0.0f;
        float fraction = 1.0f;
        for (int i = 0; i < frequencies; i++)
        {
            maxValue += 1.0f / fraction;
            fraction *= amplitudeDemultiplier;
        }

        for (int y = 0; y < mapHeight; y++)
            for (int x = 0; x < mapWidth; x++)
                noiseMap[x, y] = noiseMap[x, y] / maxValue;

        return noiseMap;
    }
}
