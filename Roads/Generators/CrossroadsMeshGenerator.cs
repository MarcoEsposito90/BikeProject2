using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class CrossroadsMeshGenerator {

    static bool debug = true;

    public static CrossroadMeshData generateMeshData(
        ControlPoint center, 
        List<Road> roads,
        float distanceFromCenter,
        float roadWidth)
    {
        CrossroadMeshData crmd = new CrossroadMeshData(roads.Count);
        List<Vector2> points = new List<Vector2>();
        Dictionary<Vector2, float> heightLevels = new Dictionary<Vector2, float>();
        AnimationCurve heightCurve = (AnimationCurve)GlobalInformation.Instance.getData(MapDisplay.MESH_HEIGHT_CURVE);
        //AnimationCurve  = new AnimationCurve(ac.keys);
        float mul = (float)GlobalInformation.Instance.getData(MapDisplay.MESH_HEIGHT_MUL);

        foreach (Road r in roads)
        {

            bool isStart = r.curve.startPoint().Equals(center.position);
            float d = isStart ? distanceFromCenter : r.curve.length() - distanceFromCenter;
            d /= r.curve.length();

            float t = r.curve.parameterOnCurveArchLength(d);
            Vector2 p = r.curve.pointOnCurve(t);
            Vector2 rightVect = r.curve.getRightVector(t, true);

            // calculate height ------------------------------------------------------
            //float n = NoiseGenerator.Instance.highestPointOnSegment(
            //    p - rightVect, 
            //    p + rightVect, 
            //    1,
            //    10);

            Vector2 c1 = (p - center.position) - rightVect * roadWidth;
            Vector2 c2 = (p - center.position) + rightVect * roadWidth;
            int index = isStart ? 0 : r.heights.Length - 1;

            points.Add(c1);
            points.Add(c2);
            heightLevels.Add(c1, r.heights[index]);
            heightLevels.Add(c2, r.heights[index]);
            
        }

        // add points to the mesh -------------------------------------------
        GeometryUtilities.ClockWiseComparer comp = new GeometryUtilities.ClockWiseComparer(Vector2.zero, true);
        points.Sort(comp);
        float centerHeight = NoiseGenerator.Instance.getNoiseValue(1, center.position.x, center.position.y);
        centerHeight = heightCurve.Evaluate(centerHeight) * mul;
        crmd.addPoint(new Vector3(0, centerHeight, 0));

        for (int i = 0; i < points.Count; i++)
        {
            Vector2 p = points[i];
            float h = heightLevels[p];
            //float height = heightCurve.Evaluate(n) * mul;
            
            // create vertices ----------------------------------------------
            Vector3 vertex1 = new Vector3(p.x, h, p.y);
            crmd.addPoint(vertex1);
        }

        return crmd;
    }


    /* ----------------------------------------------------------------------- */
    /* -------------------------- MESH DATA ---------------------------------- */
    /* ----------------------------------------------------------------------- */

    #region MESHDATA

    public class CrossroadMeshData : MeshData
    {
        // -------------------------------------------------------- 
        private int numberOfLinks;
        private int pointCounter;

        public CrossroadMeshData(int numberOfLinks)
        {
            this.numberOfLinks = numberOfLinks;
            vertices = new Vector3[numberOfLinks * 2 + 1];
            triangles = new int[numberOfLinks * 6];
            uvs = new Vector2[numberOfLinks * 2 + 1];
            pointCounter = 0;
        }


        // -------------------------------------------------------- 
        public void addPoint(Vector3 point)
        {
            if (pointCounter == vertices.Length)
                return;

            vertices[pointCounter] = point;
            uvs[pointCounter] = Vector2.zero;

            if (pointCounter > 1)
            {
                int triangleIndex = (pointCounter - 2) * 3;
                triangles[triangleIndex] = pointCounter;
                triangles[triangleIndex + 1] = pointCounter - 1;
                triangles[triangleIndex + 2] = 0;
            }

            if (pointCounter == vertices.Length - 1 && pointCounter > 0)
                closeMesh();

            pointCounter++;
        }


        // -------------------------------------------------------- 
        private void closeMesh()
        {
            // last triangle
            int triangleIndex = (pointCounter - 1) * 3;
            triangles[triangleIndex] = 1;
            triangles[triangleIndex + 1] = pointCounter;
            triangles[triangleIndex + 2] = 0;

            // center
            Vector3 center = Vector3.zero;
            for(int i = 1; i < vertices.Length; i++)
                center += vertices[i];

            center /= (float)(vertices.Length - 1);
            vertices[0] = center;
            calculateUVs();

        }

        // -------------------------------------------------------- 
        private void calculateUVs()
        {
            float left = vertices[0].x;
            float right = vertices[0].x;
            float top = vertices[0].z;
            float bottom = vertices[0].z;

            for (int i = 1; i < vertices.Length; i++)
            {
                if (vertices[i].x < left)
                    left = vertices[i].x;

                if (vertices[i].x > right)
                    right = vertices[i].x;

                if (vertices[i].y < bottom)
                    bottom = vertices[i].y;

                if (vertices[i].y > top)
                    top = vertices[i].y;

            }

            for(int i = 0; i < vertices.Length; i++)
            {
                float u = (vertices[i].x - left) / (right - left) ;
                float v = (vertices[i].y - bottom) / (top - bottom);
                uvs[i] = new Vector2(u, v);
            }
        }
    }

    #endregion
}

