using UnityEngine;
using System.Collections;
using System;

public class VillageHandler : MonoBehaviour
{
    /* ----------------------------------------------------------------------------------------- */
    /* ------------------------------- CONTANTS ------------------------------------------------ */
    /* ----------------------------------------------------------------------------------------- */

    #region CONSTANTS

    // IMPORTANT: these names must coincide with those in the prefab object. otherwise, the script won't be able to
    // find the objects and correctly create roads and relocate objects
    public static readonly string OBJ_CONTAINER = "Objects";
    public static readonly string STR_CONTAINER = "Streets";
    public const string STR_START = "Start";
    public const string STR_END = "End";
    public const string STR_START_TG = "StartTangent";
    public const string STR_END_TG = "EndTangent";
    public static readonly string CRS_CONTAINER = "Crossroads";

    #endregion

    /* ----------------------------------------------------------------------------------------- */
    /* ------------------------------- ATTRIBUTES ---------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region ATTRIBUTES

    public Transform link;
    public GameObject roadSegment;

    private static MeshData roadSegmentMeshData;
    private static bool initialized = false;
    private static float cpArea;
    private static int scale;

    #endregion


    /* ----------------------------------------------------------------------------------------- */
    /* ------------------------------- UNITY --------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region UNITY

    /* -------------------------------------------------------------------------------------- */
    private void initialize()
    {
        cpArea = (float)GlobalInformation.Instance.getData(EndlessRoadsGenerator.CP_AREA);
        scale = (int)GlobalInformation.Instance.getData(EndlessTerrainGenerator.SCALE);
        Mesh m = roadSegment.GetComponent<MeshFilter>().sharedMesh;
        roadSegmentMeshData = new MeshData(m.vertices, m.triangles, m.uv, m.normals, 0);
    }



    /* -------------------------------------------------------------------------------------- */
    void OnEnable()
    {
        if (!initialized)
            initialize();

        Debug.Log(gameObject.name + " OnEnable. position = " + gameObject.transform.position);
        Vector3 p = link.position;
        float X = p.x / (float)scale;
        float Y = p.z / (float)scale;

        Vector2 gridPos = new Vector2(X / cpArea, Y / cpArea);
        Vector2 pos = new Vector2(X, Y);

        EndlessRoadsGenerator.Instance.createControlPoint(gridPos, pos);
        initializeCrossroads();
        initializeObjects();
        initializeRoads();
    }

    #endregion


    /* ----------------------------------------------------------------------------------------- */
    /* ------------------------------- INITIALIZE ---------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region INITIALIZE


    /* ----------------------------------------------------------------------------------------- */
    private void initializeRoads()
    {
        Transform streetsContainer = transform.Find(STR_CONTAINER);

        foreach (Transform s in streetsContainer)
        {
            Vector2 start = Vector2.zero;
            Vector2 startTangent = Vector2.zero;
            Vector2 end = Vector2.zero;
            Vector2 endTangent = Vector2.zero;
            bool canCreate = true;

            foreach (Transform point in s)
            {
                Vector2 pointPos = new Vector2(point.position.x / (float)scale, point.position.z / (float)scale);
                switch (point.gameObject.name)
                {
                    case STR_START:
                        start = pointPos;
                        break;

                    case STR_START_TG:
                        startTangent = pointPos;
                        break;

                    case STR_END:
                        end = pointPos;
                        break;

                    case STR_END_TG:
                        endTangent = pointPos;
                        break;

                    default:
                        Debug.Log("WARNING: street point not found. " + gameObject.name + " -> " + s.gameObject.name);
                        canCreate = false;
                        break;
                }
            }

            if (!canCreate)
                continue;

            BezierCurve c = new BezierCurve(start, startTangent, end, endTangent);
            RoadMeshGenerator.RoadMeshData rmd = RoadMeshGenerator.generateMeshData(c, 0, roadSegmentMeshData);
            s.gameObject.GetComponent<MeshFilter>().mesh = rmd.createMesh();
            s.rotation = Quaternion.identity;
            s.position = new Vector3(s.position.x, 0, s.position.z);
        }
    }


    /* ----------------------------------------------------------------------------------------- */
    private void initializeObjects()
    {
        Transform objectsContainer = transform.Find(OBJ_CONTAINER);
        foreach (Transform o in objectsContainer)
        {
            Vector2 scaled2DPos = new Vector2(
                (o.position.x) / (float)scale,
                (o.position.z) / (float)scale);

            float h = GlobalInformation.Instance.getHeight(scaled2DPos) * scale;
            o.position = new Vector3(o.position.x, h, o.position.z);
        }
    }


    /* ----------------------------------------------------------------------------------------- */
    private void initializeCrossroads()
    {
        Transform crossroadsContainer = transform.Find(CRS_CONTAINER);

        foreach(Transform cr in crossroadsContainer)
        {
            Vector2 scaled2DPos = new Vector2(
                (cr.position.x) / (float)scale,
                (cr.position.z) / (float)scale);

            float h = GlobalInformation.Instance.getHeight(scaled2DPos) * scale;
            cr.position = new Vector3(cr.position.x, h, cr.position.z);
        }
    }


    #endregion
}
