using UnityEngine;
using System.Collections;

public static class TextureGenerator{
    
    public static Color[] generateColorMap(float[,] map, MapDisplay.DisplayMode mode, MapDisplay.TerrainType[] sections)
    {
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        //Texture2D texture = new Texture2D(width, height);
        Color[] colorMap = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colorMap[width * y + x] = getColorFromMap(map, x, y, mode, sections);
            }
        }

        return colorMap;
        //texture.filterMode = FilterMode.Point;
        //texture.wrapMode = TextureWrapMode.Clamp;
        //texture.SetPixels(colorMap);
        //texture.Apply();
        //return texture;
    }


    /* ---------------------------- COLOUR ASSIGNATIONS ---------------------------------------- */
    public static Color getColorFromMap
        (float[,] map, 
        int x, 
        int y, 
        MapDisplay.DisplayMode displayMode,
        MapDisplay.TerrainType[] sections)
    {
        switch (displayMode)
        {
            // --------------------------------------------------------------------
            case MapDisplay.DisplayMode.GreyScale:

                return Color.Lerp(Color.black, Color.white, map[x, y]);

            // --------------------------------------------------------------------
            case MapDisplay.DisplayMode.Colour:

                for (int i = 0; i < sections.Length; i++)
                    if (map[x, y] < sections[i].height)
                        return sections[i].color;

                return Color.black;

            // --------------------------------------------------------------------
            default:

                return Color.black;
        }

    }
}
