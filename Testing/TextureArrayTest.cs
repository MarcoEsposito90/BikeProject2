using UnityEngine;
using System.Collections;

public class TextureArrayTest : MonoBehaviour
{

    int width = 20;
    int height = 20;
    public Texture2D[] textures;
    public Texture2D heightMap;

    // Use this for initialization
    void Start()
    {

        System.Random r = new System.Random();

        heightMap = new Texture2D(width, height);
        for (int i = 0; i < width; i++)
            for (int j = 0; j < height; j++)
            {
                float v = r.Next(1000) / 1000.0f;
                heightMap.SetPixel(i, j, Color.Lerp(Color.white, Color.black, v));

            }

        Material m = this.GetComponent<MeshRenderer>().material;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
