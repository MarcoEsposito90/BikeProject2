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
        float localOffset)
    {
        CrossroadMeshData crmd = new CrossroadMeshData();
        int maxAdherence = (int)GlobalInformation.Instance.getData(RoadsGenerator.MAX_ROAD_ADHERENCE);

        foreach (Graph<Vector2, ControlPoint>.Link link in curves.Keys)
        {
            ControlPoint other = link.from.item.Equals(center) ? link.to.item : link.from.item;
            Vector2 relativePosition = other.position - center.position;
            crmd.addLinkPosition(relativePosition);
        }

        crmd.sortPositions();

        foreach (Graph<Vector2, ControlPoint>.Link link in curves.Keys)
        {
            ICurve curve = curves[link];
            ControlPoint other = link.from.item.Equals(center) ? link.to.item : link.from.item;
            Vector2 relativePosition = other.position - center.position;
            GeometryUtilities.QuadDirection dir = crmd.getDirection(relativePosition);
            ICurve c = getCurve(curve, center, dir, distanceFromCenter, localOffset);

            float endNoise = NoiseGenerator.Instance.highestPointOnZone(c.endPoint(), 1, 0.5f, 1);
            float endHeight = GlobalInformation.Instance.getHeight(endNoise);

            ArrayModifier aMod = new ArrayModifier(15, true, false, false);
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
        float localOffset)
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
        public Dictionary<GeometryUtilities.QuadDirection, MeshData> meshes;

        private List<Vector2> links;
        private Vector2[] linkPositions;
        private Dictionary<GeometryUtilities.QuadDirection, bool> freeDirs;

        // -------------------------------------------------------- 
        public CrossroadMeshData() : base()
        {
            meshes = new Dictionary<GeometryUtilities.QuadDirection, MeshData>();
            meshes.Add(GeometryUtilities.QuadDirection.Down, null);
            meshes.Add(GeometryUtilities.QuadDirection.Up, null);
            meshes.Add(GeometryUtilities.QuadDirection.Right, null);
            meshes.Add(GeometryUtilities.QuadDirection.Left, null);
            
            freeDirs = new Dictionary<GeometryUtilities.QuadDirection, bool>();
            freeDirs.Add(GeometryUtilities.QuadDirection.Down, true);
            freeDirs.Add(GeometryUtilities.QuadDirection.Up, true);
            freeDirs.Add(GeometryUtilities.QuadDirection.Right, true);
            freeDirs.Add(GeometryUtilities.QuadDirection.Left, true);

            linkPositions = new Vector2[4];
            links = new List<Vector2>();
        }


        // -------------------------------------------------------- 
        public void addLinkPosition(Vector2 relativePosition)
        {
            // 1) insert the position where we can 
            if (!links.Contains(relativePosition))
                links.Add(relativePosition);
        }


        // -------------------------------------------------------- 
        public void sortPositions()
        {
            // clean free directions
            freeDirs[GeometryUtilities.QuadDirection.Left] = true;
            freeDirs[GeometryUtilities.QuadDirection.Right] = true;
            freeDirs[GeometryUtilities.QuadDirection.Up] = true;
            freeDirs[GeometryUtilities.QuadDirection.Down] = true;

            // sort the array
            int bound = Mathf.Min(linkPositions.Length, links.Count);
            for (int j = 0; j < bound; j++)
            {
                if (links[j] == null)
                    continue;

                Vector2 pos = links[j];
                GeometryUtilities.QuadDirection[] dirs = GeometryUtilities.getQuadDirections(pos);
                for (int i = 0; i < dirs.Length; i++)
                {
                    if (freeDirs[dirs[i]])
                    {
                        int index = GeometryUtilities.getIndex(dirs[i]);
                        linkPositions[index] = pos;
                        freeDirs[dirs[i]] = false;
                        break;
                    }
                }

            }
        }


        // -------------------------------------------------------- 
        public GeometryUtilities.QuadDirection getDirection(Vector2 relativePosition)
        {
            //bool debug = (bool)GlobalInformation.Instance.getData(CreateRoads.ROADS_DEBUG);

            for (int i = 0; i < linkPositions.Length; i++)
                if (relativePosition.Equals(linkPositions[i]))
                    return GeometryUtilities.getDirection(i);

            return GeometryUtilities.QuadDirection.Down;
        }


        // -------------------------------------------------------- 
        public void setSegment(GeometryUtilities.QuadDirection direction, MeshData meshData)
        {
            meshes[direction] = meshData;
        }



    }

    #endregion
}

