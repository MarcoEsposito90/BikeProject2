using UnityEngine;
using System.Collections;

public class ArrayModifier : IMeshModifier {

    public int numberOfCopies;
    public Vector3 offsets;
    public Vector3 startOffsets;
    public bool xAxis, yAxis, zAxis;


    /* ------------------------------------------------------------------------ */
    /* ---------------------------- CONSTRUCTORS ------------------------------ */
    /* ------------------------------------------------------------------------ */

    #region CONTSTRUCTORS

    public ArrayModifier(
        int numberOfCopies, 
        Vector3 offsets, 
        Vector3 startOffsets, 
        bool xAxis, 
        bool yAxis, 
        bool zAxis)
    {
        this.numberOfCopies = numberOfCopies;
        this.offsets = offsets;
        this.startOffsets = startOffsets;
        this.xAxis = xAxis;
        this.yAxis = yAxis;
        this.zAxis = zAxis;
    }

    /* ------------------------------------------------------------------------ */
    public ArrayModifier(
        int numberOfCopies, 
        bool xAxis, 
        bool yAxis, 
        bool zAxis)
        : this(numberOfCopies, Vector3.zero, Vector3.zero, xAxis, yAxis, zAxis)
    {

    }

    /* ------------------------------------------------------------------------ */
    public ArrayModifier(
        int numberOfCopies, 
        Vector3 offsets, 
        bool xAxis, 
        bool yAxis, 
        bool zAxis)
        : this(numberOfCopies, offsets, Vector3.zero, xAxis, yAxis, zAxis)
    {

    }

    #endregion

    /* ------------------------------------------------------------------------ */
    /* ---------------------------- APPLY .......------------------------------ */
    /* ------------------------------------------------------------------------ */

    #region APPLY

    public void Apply(MeshData mesh)
    {
        Vector3[] vertices = new Vector3[mesh.vertices.Length * numberOfCopies];
        int[] triangles = new int[mesh.triangles.Length * numberOfCopies];
        Vector2[] uvs = new Vector2[mesh.uvs.Length * numberOfCopies];
        Vector3[] normals = new Vector3[mesh.normals.Length * numberOfCopies];

        Vector3 dimens = GeometryUtilities.calculateDimensions(mesh.vertices);
        int xMul = xAxis ? 1 : 0;
        int yMul = yAxis ? 1 : 0;
        int zMul = zAxis ? 1 : 0;

        // replicate vertices and uvs --------------------------------------------
        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            Vector2 uv = mesh.uvs[i];

            for (int j = 0; j < numberOfCopies; j++)
            {

                vertices[mesh.vertices.Length * j + i] = new Vector3(
                        (mesh.vertices[i].x + startOffsets.x) + (dimens.x + offsets.x) * j * xMul,
                        (mesh.vertices[i].y + startOffsets.y) + (dimens.y + offsets.y) * j * yMul,
                        (mesh.vertices[i].z + startOffsets.z) + (dimens.z + offsets.z) * j * zMul);

                uvs[mesh.uvs.Length * j + i] = mesh.uvs[i];
                normals[mesh.normals.Length * j + i] = mesh.normals[i];
            }
        }

        // replicate triangles ----------------------------------------------------
        for (int i = 0; i < mesh.triangles.Length; i++)
        {
            for (int j = 0; j < numberOfCopies; j++)
            {
                triangles[mesh.triangles.Length * j + i] = mesh.triangles[i] + mesh.vertices.Length * j;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uvs = uvs;
        mesh.normals = normals;
    }

    #endregion


}
