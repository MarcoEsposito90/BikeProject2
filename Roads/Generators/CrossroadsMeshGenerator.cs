using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class CrossroadsMeshGenerator
{
    static bool debug = true;

    public static CrossroadMeshData generateMeshData(
        ControlPoint center,
        Dictionary<Graph<Vector2, ControlPoint>.Link, ICurve> curves,
        float distanceFromCenter,
        MeshData segmentMeshData,
        CrossroadHandler crossroadPrefab)
    {
        CrossroadMeshData crmd = new CrossroadMeshData();
        int maxAdherence = (int)GlobalInformation.Instance.getData(RoadsGenerator.MAX_ROAD_ADHERENCE);

        foreach (Graph<Vector2, ControlPoint>.Link link in curves.Keys)
        {
            ICurve curve = curves[link];
            ControlPoint other = link.from.item.Equals(center) ? link.to.item : link.from.item;
            Vector2 relativePosition = other.position - center.position;
            GeometryUtilities.QuadDirection dir = GeometryUtilities.getQuadDirection(relativePosition);
            ICurve c = getCurve(curve, center, dir, distanceFromCenter, crossroadPrefab);

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
            crmd.setSegment(dir, md);
        }

        return crmd;
    }


    /* ----------------------------------------------------------------------- */
    private static ICurve getCurve(
        ICurve curve, 
        ControlPoint center,
        GeometryUtilities.QuadDirection direction,
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
        float localOffset = ch.localOffset;
        Vector2 localPos = localOffset * GeometryUtilities.getVector2D(direction);
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

        public void setSegment(GeometryUtilities.QuadDirection direction, MeshData meshData)
        {
            switch (direction)
            {
                case GeometryUtilities.QuadDirection.Left:
                    hasLeft = true;
                    left = meshData;
                    break;

                case GeometryUtilities.QuadDirection.Right:
                    hasRight = true;
                    right = meshData;
                    break;

                case GeometryUtilities.QuadDirection.Up:
                    hasUp = true;
                    up = meshData;
                    break;

                default:
                    hasDown = true;
                    down = meshData;
                    break;
            }
        }
    }

    #endregion
}

