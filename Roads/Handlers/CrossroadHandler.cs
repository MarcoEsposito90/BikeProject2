using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CrossroadHandler : MonoBehaviour
{

    #region ATTRIBUTES

    [Tooltip("0 -> Left, 1 -> Up, 2 -> Right, 3 -> Down")]
    public GameObject[] segments;

    public float localOffset;

    [Tooltip("0 -> Left, 1 -> Up, 2 -> Right, 3 -> Down")]
    public GameObject[] borders;
    #endregion

    /* ------------------------------------------------------------------------------ */
    /* ---------------------------- METHODS ----------------------------------------- */
    /* ------------------------------------------------------------------------------ */

    #region METHODS

    public void setData(ControlPoint.ControlPointData data)
    {
        CrossroadsMeshGenerator.CrossroadMeshData crmd = data.crossRoadMeshData;
        //bool debug = (bool)GlobalInformation.Instance.getData(CreateRoads.ROADS_DEBUG);
        foreach (GeometryUtilities.QuadDirection dir in crmd.meshes.Keys)
        {
            int index = GeometryUtilities.getIndex(dir);
            if (crmd.meshes[dir] == null)
                continue;

            segments[index].SetActive(true);
            borders[index].SetActive(false);
            Mesh m = crmd.meshes[dir].createMesh();
            m.name = dir + " segment";
            segments[index].GetComponent<MeshFilter>().sharedMesh = m;
            segments[index].GetComponent<MeshCollider>().sharedMesh = m;
        }
    }

    #endregion
}
