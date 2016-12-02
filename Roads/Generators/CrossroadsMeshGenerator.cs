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
        MeshData segmentMeshData,
        GameObject crossroadPrefab)
    {

        CrossroadHandler ch = crossroadPrefab.GetComponent<CrossroadHandler>();
        CrossroadMeshData crmd = new CrossroadMeshData();
        int maxAdherence = (int)GlobalInformation.Instance.getData(RoadsGenerator.MAX_ROAD_ADHERENCE);

        foreach (Road r in roads)
        {
            bool isStart = r.curve.startPoint().Equals(center.position);
            ControlPoint other = isStart ? r.link.to.item : r.link.from.item;
            Vector2 relativePosition = other.gridPosition - center.gridPosition;
            ICurve c = getCurve(r.curve, center, relativePosition, distanceFromCenter, ch);
            float endHeight = GlobalInformation.Instance.getHeight(c.endPoint());

            ArrayModifier aMod = new ArrayModifier(20, true, false, false);
            RoadModifier rMod = new RoadModifier(
                c, 
                RoadModifier.Axis.X,
                true,
                true,
                0,
                1);
            rMod.startHeight = center.height;
            rMod.endHeight = endHeight;
            rMod.adherence = maxAdherence;

            MeshData md = segmentMeshData.clone();
            aMod.Apply(md);
            rMod.Apply(md);
            crmd.setSegment(relativePosition, md);
        }

        return crmd;
    }


    /* ----------------------------------------------------------------------- */
    private static ICurve getCurve(
        ICurve curve, 
        ControlPoint center,
        Vector2 relativePosition,
        float distanceFromCenter, 
        CrossroadHandler ch)
    {
        int scale = (int)GlobalInformation.Instance.getData(EndlessTerrainGenerator.SCALE);

        // 1) get connection point with road --------------------------------
        bool isStart = curve.startPoint().Equals(center.position);
        float d = isStart ? distanceFromCenter : curve.length() - distanceFromCenter;
        d /= curve.length();
        float t = curve.parameterOnCurveArchLength(d);
        Vector2 curveEnd = curve.pointOnCurve(t);
        Vector2 derivate = curve.derivate1(t, true);
        if (isStart) derivate *= -1;
        Vector2 controlEnd = curveEnd + derivate * 5;

        // 2) get start point from crossroad --------------------------------
        Transform startPoint = ch.getStartPoint(relativePosition);
        Vector2 localPos = new Vector2(startPoint.localPosition.x, startPoint.localPosition.z);
        Vector2 curveStart = center.position + localPos / scale;
        Vector2 controlStart = curveStart + localPos * 5 / scale;

        BezierCurve c = new BezierCurve(curveStart, controlStart, curveEnd, controlEnd);
        return c;
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

