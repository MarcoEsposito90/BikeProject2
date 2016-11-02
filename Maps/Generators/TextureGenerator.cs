using UnityEngine;
using System.Collections;

public static class TextureGenerator
{
    private static bool debug = true;

    public static Color[] generateColorHeightMap(float[,] map)
    {
        int width = map.GetLength(0);
        int height = map.GetLength(1);
        Color[] colorMap = new Color[width * height];

        for(int x = 0; x < width; x++)
            for(int y = 0; y < height; y++)
            {
                colorMap[width * y + x] = Color.Lerp(Color.black, Color.white, map[x, y]);
            }

        return colorMap;
    }

    
}
