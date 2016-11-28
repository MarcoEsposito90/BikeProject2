using UnityEngine;
using System.Collections;

public class CrossroadHandler : MonoBehaviour {

    #region ATTRIBUTES

    public Transform LeftStart;
    public Transform RightStart;
    public Transform UpStart;
    public Transform DownStart;

    public GameObject center;
    public GameObject LeftSegment;
    public GameObject RightSegment;
    public GameObject UpSegment;
    public GameObject DownSegment;

    public GameObject LeftBorder;
    public GameObject RightBorder;
    public GameObject UpBorder;
    public GameObject DownBorder;

    public Material crossRoadMaterial;
    public Material roadMaterial;
    #endregion

    /* ------------------------------------------------------------------------------ */
    /* ---------------------------- UNITY ------------------------------------------- */
    /* ------------------------------------------------------------------------------ */

    #region UNITY

    void Start () {
	
	}
	

    /* ------------------------------------------------------------------------------ */
	void Update ()
    {

    }

    #endregion


    /* ------------------------------------------------------------------------------ */
    /* ---------------------------- METHODS ----------------------------------------- */
    /* ------------------------------------------------------------------------------ */

    #region METHODS

    public void setData(ControlPoint.ControlPointData data)
    {
        CrossroadsMeshGenerator.CrossroadMeshData crmd = data.crossRoadMeshData;
        Material cm = new Material(crossRoadMaterial);
        cm.mainTexture = data.crossroadTexture;
        Material rm = new Material(roadMaterial);
        rm.mainTexture = data.roadTexture;

        center.SetActive(true);
        center.GetComponent<MeshRenderer>().material = cm;

        LeftSegment.SetActive(crmd.hasLeft);
        LeftSegment.GetComponent<MeshRenderer>().material = rm;
        LeftBorder.SetActive(!crmd.hasLeft);
        LeftBorder.GetComponent<MeshRenderer>().material = cm;
        if (crmd.hasLeft)
        {
            Mesh m = crmd.left.createMesh();
            m.name = "LeftSegment";
            LeftSegment.GetComponent<MeshFilter>().sharedMesh = m;
            LeftSegment.GetComponent<MeshCollider>().sharedMesh = m;

        }

        RightSegment.SetActive(crmd.hasRight);
        RightSegment.GetComponent<MeshRenderer>().material = rm;
        RightBorder.SetActive(!crmd.hasRight);
        RightBorder.GetComponent<MeshRenderer>().material = cm;
        if (crmd.hasRight)
        {
            Mesh m = crmd.right.createMesh();
            m.name = "RightSegment";
            RightSegment.GetComponent<MeshFilter>().mesh = m;
            RightSegment.GetComponent<MeshCollider>().sharedMesh = m;

        }

        UpSegment.SetActive(crmd.hasUp);
        UpSegment.GetComponent<MeshRenderer>().material = rm;
        UpBorder.SetActive(!crmd.hasUp);
        UpBorder.GetComponent<MeshRenderer>().material = cm;
        if (crmd.hasUp)
        {
            Mesh m = crmd.up.createMesh();
            m.name = "UpSegment";
            UpSegment.GetComponent<MeshFilter>().mesh = m;
            UpSegment.GetComponent<MeshCollider>().sharedMesh = m;

            //Debug.Log("printing mesh ------------------ ");
            //foreach (Vector3 v in m.vertices)
            //    Debug.Log(v);
        }

        DownSegment.SetActive(crmd.hasDown);
        DownSegment.GetComponent<MeshRenderer>().material = rm;
        DownBorder.SetActive(!crmd.hasDown);
        DownBorder.GetComponent<MeshRenderer>().material = cm;
        if (crmd.hasDown)
        {
            Mesh m = crmd.down.createMesh();
            m.name = "DownSegment";
            DownSegment.GetComponent<MeshFilter>().mesh = m;
            DownSegment.GetComponent<MeshCollider>().sharedMesh = m;
        }
    }


    /* ------------------------------------------------------------------------------ */
    public Transform getStartPoint(Vector2 relativePosition)
    {
        if (relativePosition.Equals(new Vector2(-1 , 0)))
            return LeftStart;
        else if (relativePosition.Equals(new Vector2(1, 0)))
            return RightStart;
        else if (relativePosition.Equals(new Vector2(0, -1)))
            return DownStart;
        else if (relativePosition.Equals(new Vector2(0, 1)))
            return UpStart;

        return null;
    }


    #endregion
}
