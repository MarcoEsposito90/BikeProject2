using UnityEngine;
using System.Collections;
using System;

public class CurveModifier : IMeshModifier
{
    public enum Axis { X, Z }
    public Axis axis;
    public ICurve curve;
    bool relative;
    float from;
    float to;
    private int index;
    private int index2;


    public CurveModifier(
        ICurve curve, 
        Axis axis, 
        bool relative, 
        float from, 
        float to)
    {
        this.axis = axis;
        this.curve = curve;
        this.relative = relative;

        if (from < 0) from = 0;
        else if (from > 1) from = 1;
        if (to < from) to = from;
        else if (to > 1) to = 1;

        this.from = from;
        this.to = to;
        setIndex();
    }

    public CurveModifier(ICurve curve, Axis axis, bool relative)
        : this(curve, axis, relative, 0, 1)
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
        Vector3[] vertices = mesh.vertices;
        Vector3 dims = GeometryUtilities.calculateDimensions(vertices);
        Vector2 start = relative ? curve.startPoint() : Vector2.zero;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 v = vertices[i];
            float l = v[index] / dims[index];
            float t = curve.parameterOnCurveArchLength(l);
            t = from + (to - from) * t;
            //Debug.Log("l = " + l + "; t = " + t);

            Vector2 p = curve.pointOnCurve(t);
            Vector3 right = curve.getRightVector(t, true);
            vertices[i] = new Vector3(
                (p.x - start.x) - right.x * v[index2], 
                v.y,
                (p.y - start.y) - right.y * v[index2]);
        }

        mesh.vertices = vertices;
    }

    
}
