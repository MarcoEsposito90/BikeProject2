using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Threading;

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

    public bool subObjectFlattening;
    public Transform link;
    public GameObject roadSegment;
    private Dictionary<Transform, RoadMeshGenerator.RoadMeshData> roadMeshDatas;

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
        initialized = true;
    }


    /* -------------------------------------------------------------------------------------- */
    void Update()
    {
        lock (roadMeshDatas)
        {
            foreach (Transform s in roadMeshDatas.Keys)
            {
                RoadMeshGenerator.RoadMeshData rmd = roadMeshDatas[s];
                Mesh mesh = rmd.createMesh();
                s.gameObject.GetComponent<MeshFilter>().mesh = mesh;
                s.gameObject.GetComponent<MeshCollider>().sharedMesh = mesh;
                s.rotation = Quaternion.identity;
                s.position = new Vector3(s.position.x, 0, s.position.z);
            }

            roadMeshDatas.Clear();
        }
    }

    /* -------------------------------------------------------------------------------------- */
    void OnEnable()
    {
        Debug.Log(gameObject.name + " onEnable");
        if (!initialized)
            initialize();

        if (roadMeshDatas == null)
            roadMeshDatas = new Dictionary<Transform, RoadMeshGenerator.RoadMeshData>();

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
            createRoadMeshDataAsynch(s, c);
        }
    }


    /* ----------------------------------------------------------------------------------------- */
    private void createRoadMeshDataAsynch(Transform obj, ICurve curve)
    {
        ThreadStart ts = delegate
        {
            RoadMeshGenerator.RoadMeshData rmd = RoadMeshGenerator.generateMeshData(curve, 0, roadSegmentMeshData);

            lock (roadMeshDatas)
            {
                roadMeshDatas.Add(obj, rmd);
            }
        };

        Thread t = new Thread(ts);
        t.Start();
    }


    /* ----------------------------------------------------------------------------------------- */
    private void initializeObjects()
    {
        Transform objectsContainer = transform.Find(OBJ_CONTAINER);
        foreach (Transform obj in objectsContainer)
        {
            Vector2 scaled2DPos = new Vector2(
                (obj.position.x) / (float)scale,
                (obj.position.z) / (float)scale);

            float h = GlobalInformation.Instance.getHeight(scaled2DPos) * scale;
            obj.position = new Vector3(obj.position.x, h, obj.position.z);

            if (!subObjectFlattening)
                continue;

            if (!GlobalInformation.isFlatteningTag(obj.gameObject.tag))
                continue;

            //BoxCollider collider = obj.gameObject.GetComponent<BoxCollider>();
            Vector3 pos = obj.position /*+ (collider.center * obj.localScale.x)*/;
            //Vector2 sizes = new Vector2(collider.size.x, collider.size.z) * obj.localScale.x * 0.5f;
            Vector2 worldPos = new Vector2(pos.x, pos.z);
            //float radius = Mathf.Max(sizes.x, sizes.y) * 1.5f;
            NoiseGenerator.Instance.redrawRequest(worldPos, 50);
        }
    }


    /* ----------------------------------------------------------------------------------------- */
    private void initializeCrossroads()
    {
        Transform crossroadsContainer = transform.Find(CRS_CONTAINER);

        foreach (Transform cr in crossroadsContainer)
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
