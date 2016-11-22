using UnityEngine;
using System.Collections;
using System;

public class HeightModifier : IMeshModifier
{
    public enum Mode { Curve, Array }
    public Mode mode = Mode.Array;
    public ICurve curve;
    public float[] heights;

    public HeightModifier(float[] heights, ICurve curve)
    {
        this.heights = heights;
        this.curve = curve;
    }

    public void Apply(MeshData mesh)
    {
        Vector3[] vertices = mesh.vertices;
        Vector3 dimens = GeometryUtilities.calculateDimensions(vertices);
        switch (mode)
        {
            case Mode.Array:
                modifyWithArray(vertices, dimens);
                    break;
            case Mode.Curve:
                modifyWithCurve(vertices, dimens);
                break;
        }

        mesh.vertices = vertices;
    }


    private void modifyWithCurve(Vector3[] vertices, Vector3 dimens)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            float t = curve.parameterOnCurveArchLength(vertices[i].x / dimens.x);
            Vector2 p = curve.pointOnCurve(t);
            vertices[i] = new Vector3(vertices[i].x, p.y + vertices[i].y, vertices[i].z);
        }
    }

    private void modifyWithArray(Vector3[] vertices, Vector3 dimens)
    {

        for (int i = 0; i < vertices.Length; i++)
        {
            float t = vertices[i].x / dimens.x * (float)(heights.Length - 1);
            float r = (int)t + 1.0f - t;
            int index1 = (int)t;
            int index2 = Mathf.Min(index1 + 1, heights.Length - 1);
            float h = heights[index1] * r + heights[index2] * (1.0f - r);
            vertices[i] = new Vector3(vertices[i].x, h + vertices[i].y, vertices[i].z);
        }
    }
}
