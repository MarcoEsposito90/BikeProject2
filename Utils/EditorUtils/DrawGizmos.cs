using UnityEngine;
using System.Collections;

public class DrawGizmos : MonoBehaviour
{

    public Mesh mesh;
    public Color color;
    public float dimension;

    void OnDrawGizmos()
    {
        Gizmos.color = color;

        if (mesh == null)
            Gizmos.DrawSphere(transform.position, dimension);
        else
            Gizmos.DrawMesh(mesh);
    }
}
