using UnityEngine;
using System.Collections;

public static class RoadMeshGenerator
{

    public static RoadMeshData generateRoadMeshData(ICurve curve)
    {
        int numberOfSegments = 10;
        int roadWidth = 2;
        Vector2 start = curve.startPoint();
        RoadMeshData rmd = new RoadMeshData(curve, numberOfSegments);

        for (int i = 0; i <= numberOfSegments; i++)
        {
            float t = curve.parameterOnCurveArchLength(i / (float)numberOfSegments, true);
            Vector2 p = curve.pointOnCurve(t);
            Vector2 v1 = curve.derivate1(t, true);
            Vector3 crossProd = CrossProduct(Vector3.up, new Vector3(v1.x, 0, v1.y));
            Vector2 v2 = new Vector2(crossProd.x, crossProd.z);

            Vector3 vertex1 = new Vector3(
                (p.x - start.x) - v2.x * (float)roadWidth,
                0,
                (p.y - start.y) - v2.y * (float)roadWidth);

            Vector3 vertex2 = new Vector3(
                (p.x - start.x) + v2.x * (float)roadWidth,
                0,
                (p.y - start.y) + v2.y * (float)roadWidth);

            rmd.addSegment(vertex1, vertex2);
        }

        return rmd;
    }

    private static Vector3 CrossProduct(Vector3 v1, Vector3 v2)
    {
        float x = v1.y * v2.z - v1.z * v2.y;
        float y = v1.x * v2.z - v1.z * v2.x;
        float z = v1.x * v2.y - v1.y * v2.x;
        return new Vector3(x, y, z);
    }



    #region ROADMESHDATA

    public class RoadMeshData
    {
        public ICurve curve;
        public int numberOfSegments;

        public Vector3[] vertices;
        public int[] triangles;
        public Vector2[] uvs;

        private int segmentCounter;


        /* ----------------------------------------------------------- */
        public RoadMeshData(ICurve curve, int numberOfSegments)
        {
            this.curve = curve;
            this.numberOfSegments = numberOfSegments;
            vertices = new Vector3[numberOfSegments * 4];
            uvs = new Vector2[numberOfSegments * 4];
            triangles = new int[numberOfSegments * 6];
            segmentCounter = 0;
        }


        /* ----------------------------------------------------------- */
        public void addSegment(Vector3 point1, Vector3 point2)
        {
            if (segmentCounter == numberOfSegments)
                return;

            int index = segmentCounter * 4 - 2;
            int triangleIndex = segmentCounter * 6;

            if(segmentCounter < numberOfSegments)
            {
                vertices[index + 2] = point1;
                uvs[index + 2] = Vector2.zero;
                vertices[index + 3] = point2;
                uvs[index + 3] = new Vector2(1, 0);
            }
            
            if(segmentCounter > 0)
            {
                vertices[index] = point1;
                uvs[index] = new Vector2(0, 1);

                vertices[index + 1] = point2;
                uvs[index + 1] = Vector2.one;

                triangles[triangleIndex - 6] = index - 1;
                triangles[triangleIndex - 5] = index - 2;
                triangles[triangleIndex - 4] = index;

                triangles[triangleIndex - 3] = index;
                triangles[triangleIndex - 2] = index + 1;
                triangles[triangleIndex - 1] = index - 1;
            }

            segmentCounter++;
        }


        /* ----------------------------------------------------------- */
        public Mesh createMesh()
        {
            // this method can be called only on the main thread
            Mesh mesh = new Mesh();

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            return mesh;
        }
    }

    #endregion
}
