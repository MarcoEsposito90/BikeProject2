using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class RoadMeshGenerator
{

    /* ------------------------------------------------------------------------------ */
    /* ---------------------------- METHODS ----------------------------------------- */
    /* ------------------------------------------------------------------------------ */

    private static bool debug = true;

    #region METHODS

    public static RoadMeshData generateMeshData(
        //Graph<Vector2, ControlPoint>.Link link,
        ICurve curve,
        float distanceFromCP,
        MeshData segmentMeshData)
    {
        bool localDebug = debug;
        debug = false;

        int scale = (int)GlobalInformation.Instance.getData(EndlessTerrainGenerator.SCALE);
        float curveLength = curve.length();
        float meshLength = GeometryUtilities.calculateDimensions(segmentMeshData.vertices).x;
        float div = (curveLength - 2.0f * distanceFromCP) / meshLength * scale;
        int numberOfSegments = (int)div + 1;
        ArrayModifier arrayMod = new ArrayModifier(numberOfSegments, true, false, false);

        float from = curve.parameterOnCurveArchLength(distanceFromCP / curveLength);
        float to = curve.parameterOnCurveArchLength((curveLength - distanceFromCP) / curveLength);
        RoadModifier roadMod = new RoadModifier(curve, RoadModifier.Axis.X, true, false, from, to);
        //roadMod.startHeight = link.from.item.height;
        //roadMod.endHeight = link.to.item.height;
        roadMod.startHeight = GlobalInformation.Instance.getHeight(curve.startPoint());
        roadMod.endHeight = GlobalInformation.Instance.getHeight(curve.endPoint());

        MeshData newMesh = segmentMeshData.clone();
        arrayMod.Apply(newMesh);
        roadMod.Apply(newMesh);

        RoadMeshData rmd = new RoadMeshData(
            newMesh.vertices,
            newMesh.triangles,
            newMesh.uvs,
            newMesh.normals,
            newMesh.LOD,
            curve,
            numberOfSegments);

        return rmd;
    }

    #endregion


    /* ------------------------------------------------------------------------------ */
    /* ---------------------------- MESH DATA --------------------------------------- */
    /* ------------------------------------------------------------------------------ */

    #region ROADMESHDATA

    public class RoadMeshData : MeshData
    {
        public ICurve curve;
        public int numberOfSections;
        public float[] heights;
        private int sectionCounter;

        /* ----------------------------------------------------------- */
        public RoadMeshData(
            Vector3[] vertices,
            int[] triangles,
            Vector2[] uvs,
            Vector3[] normals,
            int LOD,
            ICurve curve,
            int numberOfSections) : base(vertices, triangles, uvs, normals, LOD)
        {
            this.curve = curve;
            this.numberOfSections = numberOfSections;
        }

    }

    #endregion
}
