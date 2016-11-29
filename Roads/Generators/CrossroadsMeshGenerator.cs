using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class CrossroadsMeshGenerator
{

    static bool debug = true;

    public static CrossroadMeshData generateMeshData(
        ControlPoint center,
        List<Road> roads,
        float distanceFromCenter,
        float roadWidth,
        MeshData segmentMeshData,
        GameObject crossroadPrefab)
    {

        CrossroadHandler ch = crossroadPrefab.GetComponent<CrossroadHandler>();
        CrossroadMeshData crmd = new CrossroadMeshData();
        AnimationCurve heightCurve = (AnimationCurve)GlobalInformation.Instance.getData(MapDisplay.MESH_HEIGHT_CURVE);
        float mul = (float)GlobalInformation.Instance.getData(MapDisplay.MESH_HEIGHT_MUL);

        foreach (Road r in roads)
        {
            bool isStart = r.curve.startPoint().Equals(center.position);
            ControlPoint other = isStart ? r.link.to.item : r.link.from.item;

            float d = isStart ? distanceFromCenter : r.curve.length() - distanceFromCenter;
            d /= r.curve.length();
            float t = r.curve.parameterOnCurveArchLength(d);
            Vector2 curveEnd = r.curve.pointOnCurve(t);
            Vector2 derivate = r.curve.derivate1(t, true);
            if (isStart) derivate *= -1;
            Vector2 controlEnd = curveEnd + derivate;

            Vector2 relativePosition = other.gridPosition - center.gridPosition;
            Transform startPoint = ch.getStartPoint(relativePosition);
            Vector2 localPos = new Vector2(startPoint.localPosition.x, startPoint.localPosition.z);
            Vector2 curveStart = center.position + localPos;
            Vector2 controlStart = curveStart + localPos * 2;

            //Debug.Log(center.position + ": " + curveStart + " - " + controlStart + " - " + controlEnd + " - " + curveEnd);

            ArrayModifier aMod = new ArrayModifier(3, true, false, false);
            int index = isStart ? 0 : r.heights.Length - 1;
            float finalH = NoiseGenerator.Instance.getNoiseValue(1, curveEnd.x, curveEnd.y);
            finalH = heightCurve.Evaluate(finalH) * mul;
            float startH = NoiseGenerator.Instance.getNoiseValue(1, center.position.x, center.position.y);
            startH = heightCurve.Evaluate(startH) * mul;
            float[] heights = new float[3];
            
            for(int i = 0; i < heights.Length; i++)
            {
                float h = startH + (finalH - startH) * i / (float)heights.Length;
                h -= startH;
                heights[i] = h;
            }
            HeightModifier hMod = new HeightModifier(heights, null);
            BezierCurve c = new BezierCurve(curveStart, controlStart, curveEnd, controlEnd);
            RoadModifier cMod = new RoadModifier(c, RoadModifier.Axis.X, true);

            MeshData md = segmentMeshData.clone();
            aMod.Apply(md);
            hMod.Apply(md);
            cMod.Apply(md);
            crmd.setSegment(relativePosition, md);
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
        public MeshData left;
        public MeshData right;
        public MeshData up;
        public MeshData down;
        public bool hasLeft;
        public bool hasRight;
        public bool hasUp;
        public bool hasDown;

        public CrossroadMeshData() : base()
        {
            hasLeft = false;
            hasRight = false;
            hasUp = false;
            hasDown = false;
        }

        public void setSegment(Vector2 relativePosition, MeshData meshData)
        {
            if (relativePosition.Equals(new Vector2(-1, 0)))
            {
                hasLeft = true;
                left = meshData;
            }
            else if (relativePosition.Equals(new Vector2(1, 0)))
            {
                hasRight = true;
                right = meshData;
            }
            else if (relativePosition.Equals(new Vector2(0, -1)))
            {
                hasDown = true;
                down = meshData;
            }
            else if (relativePosition.Equals(new Vector2(0, 1)))
            {
                hasUp = true;
                up = meshData;
            }

        }


    }

    #endregion
}

