using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class CrossroadsMeshGenerator {



    public static CrossroadMeshData generateMeshData(
        ControlPoint center, 
        List<ICurve> links,
        float distanceFromCenter,
        float roadWidth)
    {
        List<Vector2> points = new List<Vector2>();
        AnimationCurve ac = (AnimationCurve)GlobalInformation.Instance.getData(MapDisplay.MESH_HEIGHT_CURVE);
        AnimationCurve heightCurve = new AnimationCurve(ac.keys);
        float mul = (float)GlobalInformation.Instance.getData(MapDisplay.MESH_HEIGHT_MUL);

        foreach (ICurve curve in links)
        {
            bool fromStart = curve.startPoint().Equals(center.position);
            float t = curve.parameterOnCurveArchLength(distanceFromCenter, fromStart);

            Vector2 p = curve.pointOnCurve(t);
            Vector2 rightVect = curve.getRightVector(t);

            // calculate height ------------------------------------------------------
            float n = NoiseGenerator.Instance.highestPointOnSegment(
                p, 
                p + rightVect, 
                1,
                10);

            float height = heightCurve.Evaluate(n) * mul;

            // create vertices --------------------------------------------------------
            Vector3 vertex1 = new Vector3(
                (p.x - center.position.x) - rightVect.x * roadWidth,
                height,
                (p.y - center.position.y) - rightVect.y * roadWidth);

            Vector3 vertex2 = new Vector3(
                (p.x - center.position.x) + rightVect.x * roadWidth,
                height,
                (p.y - center.position.y) + rightVect.y * roadWidth);
        }
        return null;
    }



    public class CrossroadMeshData : MeshData
    {
        private int numberOfLinks;
        private int linkCounter;

        public CrossroadMeshData(int numberOfLinks)
        {
            this.numberOfLinks = numberOfLinks;
            vertices = new Vector3[numberOfLinks * 2 + 1];
            triangles = new int[numberOfLinks * 6];
            uvs = new Vector2[numberOfLinks * 2 + 1];
            linkCounter = 0;

            // first vertex is the center of the crossroad
            vertices[0] = new Vector3(0, 0, 0);
            uvs[0] = new Vector2(0, 0); // TODO
        }

        public void addLink()
        {

        }
    }
}

