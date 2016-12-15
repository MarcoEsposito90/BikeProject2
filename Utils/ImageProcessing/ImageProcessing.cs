using UnityEngine;
using System.Collections;

public static class ImageProcessing
{

    public static float[,] radialFlattening(float[,] heightMap, int radius, int centerX, int centerY, float value)
    {
        Vector2 center = new Vector2(centerX, centerY);
        for (int x = centerX - 2 * radius; x < centerX + 2 * radius; x++)
            for (int y = centerY - 2 * radius; y < centerY + 2 * radius; y++)
            {
                if (x < 0 || x > heightMap.GetLength(0))
                    continue;
                if (y < 0 || y >= heightMap.GetLength(1))
                    continue;

                Vector3 v = new Vector3(x, 0, y);
                float d = Vector2.Distance(center, new Vector2(x, y));
                if (d < radius)
                    d = radius;

                d = d / radius - 1;
                if (d > 1) d = 1;
                heightMap[x, y] = d * heightMap[x, y] + (1 - d) * value;
            }

        return heightMap;
    }
}
