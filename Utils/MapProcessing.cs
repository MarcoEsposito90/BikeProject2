using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class MapProcessing
{

    public static void medianFilter(float[,] map, MapChunk chunk, BezierCurve curve, int curveWidth, int kernelSize, MapGenerator mapGenerator)
    {
        bool debug = chunk.position.Equals(new Vector2(0, 0));

        float[,] clonedMap = (float[,])map.Clone();

        for (float t = 0.0f; t <= 1.0f; t += 0.01f)
        {
            Vector2 point = curve.pointOnCurve(t);

            int localX = (int)(point.x - (chunk.position.x - 0.5f) * chunk.size);
            int localY = (int)((chunk.position.y + 0.5f) * chunk.size - point.y);

            int startX = Mathf.Max(0, localX - curveWidth);
            int endX = Mathf.Min(map.GetLength(0) - 1, localX + curveWidth);
            int startY = Mathf.Max(0, localY - curveWidth);
            int endY = Mathf.Min(map.GetLength(1) - 1, localY + curveWidth);

            for (int j = startX; j <= endX; j++)
            {
                for (int k = startY; k <= endY; k++)
                {
                    if (j < 0 || j >= map.GetLength(0)) continue;
                    if (k < 0 || k >= map.GetLength(1)) continue;

                    float newValue = 0.0f;
                    int denom = 0;
                    for (int m = j - kernelSize; m <= j + kernelSize; m++)
                        for (int n = k - kernelSize; n <= k + kernelSize; n++)
                        {
                            if (m < 0 || m >= map.GetLength(0) ||
                                n < 0 || n >= map.GetLength(1))
                            {
                                float x = (chunk.position.x - 0.5f) * chunk.size + mapGenerator.offsetX + m;
                                float y = (chunk.position.y + 0.5f) * chunk.size + mapGenerator.offsetY - n;

                                //if (debug)
                                //    Debug.Log("pixel: " + m + "," + n + "; position = " + x + "," + y);

                                newValue += Noise.getNoiseValue(mapGenerator.noiseScale,
                                                                    x,
                                                                    y,
                                                                    mapGenerator.numberOfFrequencies,
                                                                    mapGenerator.frequencyMultiplier,
                                                                    mapGenerator.amplitudeDemultiplier);
                            }
                            else
                                newValue += clonedMap[m, n];

                            denom++;
                        }

                    newValue = newValue / (float)denom;
                    map[j, k] = newValue;
                }
            }

        }


    }

}
