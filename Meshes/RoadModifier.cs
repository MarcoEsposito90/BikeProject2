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
    int adherence;
    private int index;
    private int index2;


    public RoadModifier(
        ICurve curve, 
        Axis axis, 
        bool relative,
        int adherence,
        float from, 
        float to)
    {
        this.axis = axis;
        this.curve = curve;
        this.relative = relative;
        this.adherence = adherence;

        if (from < 0) from = 0;
        else if (from > 1) from = 1;
        if (to < from) to = from;
        else if (to > 1) to = 1;

        this.from = from;
        this.to = to;   
        setIndex();
    }

    public RoadModifier(ICurve curve, Axis axis, bool relative)
        : this(curve, axis, relative, 0, 1, 1)
    {
        //this.axis = axis;
        //this.curve = curve;
        //this.relative = relative;
        //setIndex();
    }

    


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
                index2 = 0;
                break;
        }
    }


    public void Apply(MeshData mesh)
    {
        AnimationCurve heightCurve = (AnimationCurve)GlobalInformation.Instance.getData(MapDisplay.MESH_HEIGHT_CURVE);
        float mul = (float)GlobalInformation.Instance.getData(MapDisplay.MESH_HEIGHT_MUL);
        float startHeight = getHeight(curve.startPoint(), heightCurve, mul);
        float endHeight = getHeight(curve.endPoint(), heightCurve, mul);
        float denom = 1.0f / (float)adherence;

        Vector3[] vertices = mesh.vertices;
        Vector3 dims = GeometryUtilities.calculateDimensions(vertices);
        Vector2 start = relative ? curve.startPoint() : Vector2.zero;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 v = vertices[i];


            float l = v[index] / dims[index];
            float t = curve.parameterOnCurveArchLength(l);
            t = from + (to - from) * t;

            Vector2 p = curve.pointOnCurve(t);
            Vector3 right = curve.getRightVector(t, true) * v[index2];
            float h = getHeight(p, heightCurve, mul);
            float medH = startHeight + (endHeight - startHeight) * t;
            h = medH + denom * (h - medH);

            vertices[i] = new Vector3(
                (p.x - start.x) - right.x, 
                h + v.y,
                (p.y - start.y) - right.y);

        }

        mesh.vertices = vertices;
    }


    private float getHeight(Vector2 point, AnimationCurve heightCurve, float multiplier)
    {
        float n = NoiseGenerator.Instance.highestPointOnZone(point, 1, 0.5f, 1);
        return heightCurve.Evaluate(n) * multiplier;
    }

}
