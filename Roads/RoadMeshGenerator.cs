using UnityEngine;
using System.Collections;

public static class RoadMeshGenerator
{

    public static RoadMeshData generateMeshData(
        ICurve curve, 
        float roadWidth, 
        float distanceFromCP, 
        float segmentLength)
    {
        
        float curveLength = curve.length();
        Vector2 start = curve.startPoint();
        float div = (curveLength - 2.0f * distanceFromCP) / segmentLength;
        int numberOfSegments = (int)div + 1;
        float recomputedLength = (curveLength - 2.0f * distanceFromCP) / (float)numberOfSegments;

        RoadMeshData rmd = new RoadMeshData(curve, numberOfSegments);
        AnimationCurve ac = (AnimationCurve)GlobalInformation.Instance.getData(MapDisplay.MESH_HEIGHT_CURVE);
        AnimationCurve heightCurve = new AnimationCurve(ac.keys);
        float mul = (float)GlobalInformation.Instance.getData(MapDisplay.MESH_HEIGHT_MUL);

        //Debug.Log("number of segs = " + numberOfSegments + "; length = " + recomputedLength + " (" + curve.startPoint() + " - " + curve.endPoint() + ")");
        for (int i = 0; i <= numberOfSegments; i++)
        {
            float l = distanceFromCP + i * ((curveLength - 2.0f * distanceFromCP) / (float)numberOfSegments);
            l /= curveLength;
            float t = curve.parameterOnCurveArchLength(l, true);

            Vector2 p = curve.pointOnCurve(t);
            Vector2 rightVect = curve.getRightVector(t);
            float n = NoiseGenerator.Instance.highestPointOnSegment(
                p, 
                p + rightVect, 
                1, 
                10);

            float height = heightCurve.Evaluate(n) * mul;

            // create vertices --------------------------------------------------------
            Vector3 vertex1 = new Vector3(
                (p.x - start.x) - rightVect.x * roadWidth,
                height,
                (p.y - start.y) - rightVect.y * roadWidth);

            Vector3 vertex2 = new Vector3(
                (p.x - start.x) + rightVect.x * roadWidth,
                height,
                (p.y - start.y) + rightVect.y * roadWidth);

            
            rmd.addSegment(vertex1, vertex2);
        }

        return rmd;
    }

    //private static Vector3 CrossProduct(Vector3 v1, Vector3 v2)
    //{
    //    float x = v1.y * v2.z - v1.z * v2.y;
    //    float y = v1.x * v2.z - v1.z * v2.x;
    //    float z = v1.x * v2.y - v1.y * v2.x;
    //    return new Vector3(x, y, z);
    //}



    #region ROADMESHDATA

    public class RoadMeshData : MeshData
    {
        public ICurve curve;
        public int numberOfSegments;
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
            if (segmentCounter > numberOfSegments)
            {
                Debug.Log("exceed segments");
                return;
            }

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

    }

    #endregion
}
