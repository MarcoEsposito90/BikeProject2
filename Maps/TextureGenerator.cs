using UnityEngine;
using System.Collections;

public static class TextureGenerator
{
    private static bool debug = true;

    public static Color[] generateColorMap(
        float[,] map,
        MapDisplay.DisplayMode mode,
        MapDisplay.Section[] sections,
        int LOD,
        int textureSize)
    {

        int width = Mathf.RoundToInt(map.GetLength(0) * textureSize / (float)(LOD + 1));
        int height = Mathf.RoundToInt(map.GetLength(1) * textureSize / (float)(LOD + 1));

        if (debug)
            Debug.Log("texture is " + width + "x" + height);

        bool localDebug = debug;
        if (debug) debug = false;

        Color[] colorMap = new Color[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                //if (localDebug)
                //    Debug.Log("xy = " + x + "," + y);

                colorMap[width * y + x] = getColorFromMap(map,
                                                          x,
                                                          y,
                                                          mode,
                                                          sections,
                                                          textureSize,
                                                          localDebug && y == 0);

                //if (localDebug)
                //    Debug.Log(" after xy = " + x + "," + y);
            }

        return colorMap;
    }


    /* ---------------------------- COLOUR ASSIGNATIONS ---------------------------------------- */
    public static Color getColorFromMap
        (float[,] map,
        int x,
        int y,
        MapDisplay.DisplayMode displayMode,
        MapDisplay.Section[] sections,
        int textureSize,
        bool debug)
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
            case MapDisplay.DisplayMode.Textured:

                int mapX = Mathf.FloorToInt(x / (float)textureSize);
                int mapY = Mathf.FloorToInt(y / (float)textureSize);
                //if (debug)
                //    Debug.Log("map = " + mapX + "," + mapY);
                int textureWidth = map.GetLength(0) * textureSize;
                int textureHeight = map.GetLength(1) * textureSize;
                //Debug.Log("texture: " + textureWidth + "," + textureHeight);

                for (int i = 0; i < sections.Length; i++)
                {
                    int imageWidth = sections[i].colorMap.GetLength(0);
                    int imageHeight = sections[i].colorMap.GetLength(1);
                    int tiles = sections[i].tiles;

                    if (map[mapX, mapY] < sections[i].height)
                    {
                        int u = Mathf.FloorToInt(((x / (float)textureWidth) * (float)imageWidth) * tiles) % imageWidth;
                        int v = Mathf.FloorToInt(((y / (float)textureHeight) * (float)imageHeight) * tiles) % imageHeight;

                        //if (debug)
                        //    Debug.Log("xy =  " + x + "," + y + "; uv = " + u + "," + v);
                        return sections[i].colorMap[u, v];
                    }
                }


                return Color.black;
            // --------------------------------------------------------------------

            default:

                return Color.black;
        }

    }
}
