using UnityEngine;
using System.Collections;
using System;

public class MeshData : IMeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;
    public Vector3[] normals;
    public int LOD;

    /*  NOTE: the vector "triangles" stores the indexes of the vertices of the mesh, in the order
        they compose the triangles. For example, if:
    
        triangles = { 1, 4, 5, 3, 2, 0}
        vertices = { (0.32,0.54,1.03) , (.....) , .... }
    
        means the mesh is composed of two triangles: (1,4,5) ; (3,2,0)
        the coordinates of each vertex are retrieved from the vertices array: 
        in this case vertex 1 has coordinates 0.32 , 0.54, 1.03
    */

    // constructor does nothing except avoiding nullptr exceptions
    // it must be overriden in the hierarchy
    public MeshData()
    {
        vertices = new Vector3[0];
        triangles = new int[0];
        uvs = new Vector2[0];
        normals = new Vector3[0];
        LOD = -1;
    }

    public MeshData(Vector3[] vertices, int[] triangles, Vector2[] uvs, Vector3[] normals, int LOD)
    {
        this.vertices = vertices;
        this.triangles = triangles;
        this.uvs = uvs;
        this.normals = normals;
        this.LOD = LOD;
    }

    public MeshData clone()
    {
        return new MeshData(vertices, triangles, uvs, normals, LOD);
    }

    // this method can be called only on the main thread,
    // since meshes can't be created in secondary threads
    public Mesh createMesh()
    {
        Mesh mesh = new Mesh();

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        if (normals == null || normals.Length == 0)
            mesh.RecalculateNormals();
        else
            mesh.normals = normals;

        return mesh;
    }
}
