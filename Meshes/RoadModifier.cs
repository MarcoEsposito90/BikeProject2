using UnityEngine;
using System.Collections;
using System;

public class RoadModifier : IMeshModifier
{

    public enum Axis { X, Z }
    public Axis axis;
    public ICurve curve;
    bool relative;
    float from;
    float to;
    public int adherence;
    private bool relativeHeight;
    public float startHeight;
    public float endHeight;
    private int index;
    private int index2;


    /* -------------------------------------------------------------------------------------- */
    /* -------------------------------- CONSTRUCTORS ---------------------------------------- */
    /* -------------------------------------------------------------------------------------- */

    #region CONSTRUCTORS

    public RoadModifier(
        ICurve curve,
        Axis axis,
        bool relative,
        bool relativeHeight,
        float from,
        float to)
    {
        this.axis = axis;
        this.curve = curve;
        this.relative = relative;
        this.relativeHeight = relativeHeight;

        if (from < 0) from = 0;
        else if (from > 1) from = 1;
        if (to < from) to = from;
        else if (to > 1) to = 1;

        this.from = from;
        this.to = to;
        setIndex();

        //AnimationCurve heightCurve = (AnimationCurve)GlobalInformation.Instance.getData(MapDisplay.MESH_HEIGHT_CURVE);
        //float mul = (float)GlobalInformation.Instance.getData(MapDisplay.MESH_HEIGHT_MUL);
        //startHeight = getHeight(curve.pointOnCurve(from), heightCurve, mul);
        //endHeight = getHeight(curve.pointOnCurve(to), heightCurve, mul);
        startHeight = GlobalInformation.Instance.getHeight(curve.pointOnCurve(from));
        endHeight = GlobalInformation.Instance.getHeight(curve.pointOnCurve(to));
        adherence = (int)GlobalInformation.Instance.getData(RoadsGenerator.ROAD_ADHERENCE);
    }


    /* -------------------------------------------------------------------------------------- */
    public RoadModifier(ICurve curve, Axis axis, bool relative)
        : this(curve, axis, relative, false, 0, 1)
    { }

    
    /* -------------------------------------------------------------------------------------- */
    private void setIndex()
    {
        switch (axis)
        {
            case Axis.X:
                index = 0;
                index2 = 2;
                break;
            case Axis.Z:
                index = 2;
                index = 0;
                break;
            default:
                index = 0;
                index2 = 2;
                break;
        }
    }


    #endregion


    /* -------------------------------------------------------------------------------------- */
    /* -------------------------------- APPLY ----------------------------------------------- */
    /* -------------------------------------------------------------------------------------- */

    #region APPLY

    public void Apply(MeshData mesh)
    {
        int maxAdherence = (int)GlobalInformation.Instance.getData(RoadsGenerator.MAX_ROAD_ADHERENCE);
        float coeff = (adherence - 1) / (float)(maxAdherence - 1);
        float denom = 1.0f / (float)adherence;

        Vector3[] vertices = mesh.vertices;
        Vector3 dims = GeometryUtilities.calculateDimensions(vertices);
        Vector2 start = relative ? curve.startPoint() : Vector2.zero;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 v = vertices[i];
            float l = v[index] / dims[index];
            float t = curve.parameterOnCurveArchLength(l);
            float tRemap = from + (to - from) * t;

            Vector2 p = curve.pointOnCurve(tRemap);
            Vector3 right = curve.getRightVector(tRemap, true) * v[index2];

            float n = NoiseGenerator.Instance.highestPointOnZone(p, 1, 0.5f, 1);
            float terrainH = GlobalInformation.Instance.getHeight(n);
            float medH = startHeight + (endHeight - startHeight) * t;
            float height = medH + (1.0f - coeff) * (terrainH - medH);

            if (height < terrainH) height = terrainH;
            if (relativeHeight) height = height - startHeight;

            vertices[i] = new Vector3(
                (p.x - start.x) - right.x,
                height + v.y,
                (p.y - start.y) - right.y);

        }

        mesh.vertices = vertices;
    }

    #endregion

    

}
