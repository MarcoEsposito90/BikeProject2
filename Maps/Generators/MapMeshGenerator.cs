using UnityEngine;
using System.Collections;

public static class MapMeshGenerator
{

    public static MapMeshData generateMesh
        (float[,] noiseMap,
        AnimationCurve _meshHeightCurve,
        float heightMultiplier,
        int LOD)
    {

        AnimationCurve meshHeightCurve = new AnimationCurve(_meshHeightCurve.keys);
        int width = noiseMap.GetLength(0);
        int height = noiseMap.GetLength(1);
        float topLeftX = -noiseMap.GetLength(0) / 2.0f;
        float topLeftZ = noiseMap.GetLength(1) / 2.0f;

        MapMeshData meshData = new MapMeshData(width, height, LOD);
        int vertexIndex = 0;
        int increment = (int)Mathf.Pow(2, LOD);

        for (int x = 0; x < width; x += increment)
            for (int y = 0; y < height; y += increment)
            {
                float h = meshHeightCurve.Evaluate(noiseMap[x, y]) * heightMultiplier;
                Vector3 vertex = new Vector3(topLeftX + x, h, topLeftZ - y);
                meshData.vertices[vertexIndex++] = vertex;
            }

        return meshData;
    }


    public static MapMeshData generateMesh(int width, int height)
    {

        float topLeftX = -width / 2.0f;
        float topLeftZ = height / 2.0f;

        MapMeshData meshData = new MapMeshData(2, 2, 0);

        meshData.vertices[0] = new Vector3(topLeftX, 0, topLeftZ);
        meshData.vertices[1] = new Vector3(topLeftX, 0, topLeftZ - height);
        meshData.vertices[2] = new Vector3(topLeftX + width, 0, topLeftZ);
        meshData.vertices[3] = new Vector3(topLeftX + width, 0, topLeftZ - height);
        return meshData;
    }

    /* ---------------------------------------------------------------------- */
    /* ---------------------- MESH DATA ------------------------------------- */
    /* ---------------------------------------------------------------------- */

    public class MapMeshData : MeshData
    {
        public int width;
        public int height;

        /* --------------- CONSTRUCTOR ----------------------------------- */
        public MapMeshData(int width, int height, int LOD)
        {
            this.LOD = LOD;

            int a = (LOD == 0 ? 0 : 1);
            this.width = width / (int)Mathf.Pow(2, LOD) + a;
            this.height = height / (int)Mathf.Pow(2, LOD) + a;

            vertices = new Vector3[width * height];
            triangles = new int[(width - 1) * (height - 1) * 6];
            uvs = new Vector2[width * height];

            for (int y = 0; y < this.height; y++)
                for (int x = 0; x < this.width; x++)
                {
                    addTriangles(x, y);
                    generateUvs(x, y);
                }
        }


        /* ------------- TRIANGLES ASSIGNATION --------------------------- */
        public void addTriangles(int x, int y)
        {
            if (x == width - 1 || y == height - 1)
                return;

            int vertexIndex = x * width + y;
            int triangleIndex = (x * (width - 1) + y) * 6;

            if (triangleIndex >= triangles.Length)
                return;

            // triangle 1
            triangles[triangleIndex] = vertexIndex;
            triangles[triangleIndex + 1] = vertexIndex + width;
            triangles[triangleIndex + 2] = vertexIndex + width + 1;

            // triangle 2
            triangles[triangleIndex + 3] = vertexIndex;
            triangles[triangleIndex + 4] = vertexIndex + width + 1;
            triangles[triangleIndex + 5] = vertexIndex + 1;
        }

        /* ------------- UV COORDINATES ----------------------------------- */
        public void generateUvs(int x, int y)
        {
            int index = x * width + y;

            if (index >= uvs.Length)
                return;

            uvs[index] = new Vector2(x / (float)(width - 1), y / (float)(height - 1));
        }
    }
}
