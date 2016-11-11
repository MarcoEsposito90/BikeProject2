using UnityEngine;
using System.Collections;

public static class GeometryUtilities {


    public static Vector3 CrossProduct(Vector3 v1, Vector3 v2)
    {
        float x = v1.y * v2.z - v1.z * v2.y;
        float y = v1.x * v2.z - v1.z * v2.x;
        float z = v1.x * v2.y - v1.y * v2.x;
        return new Vector3(x, y, z);
    }
}
