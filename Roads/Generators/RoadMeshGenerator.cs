using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class RoadMeshGenerator
{

    /* ------------------------------------------------------------------------------ */
    /* ---------------------------- METHODS ----------------------------------------- */
    /* ------------------------------------------------------------------------------ */

    #region METHODS

    public static RoadMeshData generateMeshData(
        ICurve curve, 
        float roadWidth, 
        float distanceFromCP, 
        float segmentLength,
        int adherence,
        MeshData segmentMeshData)
    {
        float curveLength = curve.length();
        float div = (curveLength - 2.0f * distanceFromCP) / segmentLength;
        int numberOfSegments = (int)div + 1;
        float[] heights = getHeightArray(
            curve, 
            numberOfSegments, 
            distanceFromCP, 
            adherence);

        Vector3 startOffsets = new Vector3(distanceFromCP, 0, 0);
        ArrayModifier arrayMod = new ArrayModifier(
            numberOfSegments, 
            true, 
            false, 
            false);

        HeightModifier heightMod = new HeightModifier(heights, null);
        heightMod.mode = HeightModifier.Mode.Array;
        float from = curve.parameterOnCurveArchLength(distanceFromCP / curveLength);
        float to = curve.parameterOnCurveArchLength((curveLength - distanceFromCP) / curveLength);
        CurveModifier curveMod = new CurveModifier(curve, CurveModifier.Axis.X, true, from, to);

        MeshData newMesh = segmentMeshData.clone();
        arrayMod.Apply(newMesh);
        heightMod.Apply(newMesh);
        curveMod.Apply(newMesh);

        RoadMeshData rmd = new RoadMeshData(
            newMesh.vertices,
            newMesh.triangles, 
            newMesh.uvs,
            newMesh.normals,
            newMesh.LOD, 
            curve, 
            numberOfSegments);

        rmd.heights = heights;
        return rmd;
    }


    /* ------------------------------------------------------------------------------ */
    private static float[] getHeightArray(
        ICurve curve, 
        int numberOfSegments, 
        float distanceFromCP, 
        int adherence)
    {
        AnimationCurve heightCurve = (AnimationCurve)GlobalInformation.Instance.getData(MapDisplay.MESH_HEIGHT_CURVE);
        float mul = (float)GlobalInformation.Instance.getData(MapDisplay.MESH_HEIGHT_MUL);

        float curveLength = curve.length();
        float[] heights = new float[numberOfSegments + 1];
        Vector2 start = curve.startPoint();
        Vector2 end = curve.endPoint();
        float startHeight = getHeight(start, heightCurve, mul);
        float endHeight = getHeight(end, heightCurve, mul);
        float denom = 1.0f / (float)adherence;
        float toAdd = (curveLength - 2.0f * distanceFromCP) / (float)numberOfSegments;
        float l = distanceFromCP;

        for (int i = 0; i <= numberOfSegments; i++)
        {
            l += toAdd;
            float t = curve.parameterOnCurveArchLength(l / curveLength);
            Vector2 p = curve.pointOnCurve(t);
            float n = NoiseGenerator.Instance.highestPointOnZone(p, 1, 1, 2);
            float h = getHeight(p, heightCurve, mul);
            float medH = startHeight + (endHeight - startHeight) * t;
            heights[i] = medH + denom * (h - medH);
        }

        return heights;
    }


    private static float getHeight(Vector2 point, AnimationCurve heightCurve, float multiplier)
    {
        float n = NoiseGenerator.Instance.highestPointOnZone(point, 1, 3, 2);
        return heightCurve.Evaluate(n) * multiplier;
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
        //public RoadMeshData(ICurve curve, int numberOfSections)
        //{
        //    this.curve = curve;
        //    this.numberOfSections = numberOfSections;
        //    vertices = new Vector3[numberOfSections * 4];
        //    uvs = new Vector2[numberOfSections * 4];
        //    triangles = new int[numberOfSections * 6];
        //    sectionCounter = 0;
        //}

        /* ----------------------------------------------------------- */
        public RoadMeshData(
            Vector3[] vertices, 
            int[] triangles, 
            Vector2[] uvs,
            Vector3[] normals,
            int LOD,
            ICurve curve,
            int numberOfSections) : base (vertices, triangles, uvs, normals, LOD)
        {
            this.curve = curve;
            this.numberOfSections = numberOfSections;
        }

        /* ----------------------------------------------------------- */
        //public void addSegment(Vector3 point1, Vector3 point2)
        //{
        //    if (sectionCounter > numberOfSections)
        //    {
        //        Debug.Log("exceed segments");
        //        return;
        //    }

        //    int index = sectionCounter * 4 - 2;
        //    int triangleIndex = sectionCounter * 6;

        //    if(sectionCounter < numberOfSections)
        //    {
        //        vertices[index + 2] = point1;
        //        uvs[index + 2] = Vector2.zero;
        //        vertices[index + 3] = point2;
        //        uvs[index + 3] = new Vector2(1, 0);
        //    }
            
        //    if(sectionCounter > 0)
        //    {
        //        vertices[index] = point1;
        //        uvs[index] = new Vector2(0, 1);

        //        vertices[index + 1] = point2;
        //        uvs[index + 1] = Vector2.one;

        //        triangles[triangleIndex - 6] = index - 1;
        //        triangles[triangleIndex - 5] = index - 2;
        //        triangles[triangleIndex - 4] = index;

        //        triangles[triangleIndex - 3] = index;
        //        triangles[triangleIndex - 2] = index + 1;
        //        triangles[triangleIndex - 1] = index - 1;
        //    }

        //    sectionCounter++;
        //}

    }

    #endregion
}
