using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

public class EndlessObjectGenerator : MonoBehaviour
{


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- ATTRIBUTES --------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region ATTRIBUTES

    public string ObjectName;

    [Range(1, 1000)]
    public int seed;

    private const int DENSITY_ONE = 10;
    [Range(1, 50)]
    public int density;

    [Range(1, 30)]
    public int positionRandomness;

    [Range(0, 0.9f)]
    public float scaleRandomness;

    [Range(1, 100)]
    public int uniformness;

    [Range(0.0f, 1.0f)]
    public float probability;

    [Range(0.1f, 5)]
    public float radius;

    [Range(0.0f, 1.0f)]
    public float minHeight;

    [Range(0.0f, 1.0f)]
    public float maxHeight;

    [Range(20, 500)]
    public int maxObjsForLoop;

    public bool acceptsSelfIntersection;
    public bool flatteningRequested;
    public GameObject prefab;
    public EndlessTerrainGenerator terrainGenerator;

    /* ----------------------------------------------------------------------------------------- */
    BlockingQueue<ObjectData> resultsQueue;
    private Dictionary<Vector2, ObjectData> currentObjects;
    private PoolManager<Vector2> objectPoolManager;

    private int sectorSize;
    private int scale;
    private Transform viewer;
    private Vector2 latestViewerRecordedPosition;
    private float viewerDistanceUpdate;
    private float seedX, seedY;

    private float area;
    private float scaledArea;
    private float distanceThreshold;
    private float overlapsCheckDistanceThreshold;
    private float noiseScale;
    private bool start = true;

    private Vector3 colliderLocalPosition;
    private Vector3 colliderSizes;
    private bool hasCollider;
    private int priority;

    #endregion

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- UNITY CALLBACKS ---------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region UNITY

    void Awake()
    {
        currentObjects = new Dictionary<Vector2, ObjectData>();
        resultsQueue = new BlockingQueue<ObjectData>();

        BoxCollider collider = prefab.GetComponent<BoxCollider>();
        hasCollider = collider != null;
        if (hasCollider)
        {
            colliderLocalPosition = collider.center;
            colliderSizes = collider.size;
            priority = GlobalInformation.getPriority(prefab.tag);
        }


        if (priority == -1)
        {
            Debug.Log("ATTENTION! you have to select a tag for " + prefab);
            priority = 0;
        }

    }


    /* ----------------------------------------------------------------------------------------- */
    void Start()
    {
        System.Random random = new System.Random(seed);
        seedX = ((float)random.NextDouble()) * random.Next(1000);
        seedY = ((float)random.NextDouble()) * random.Next(1000);

        sectorSize = (int)GlobalInformation.Instance.getData(EndlessTerrainGenerator.SECTOR_SIZE);
        scale = (int)GlobalInformation.Instance.getData(EndlessTerrainGenerator.SCALE);
        //viewerDistanceUpdate = (float)GlobalInformation.Instance.getData(EndlessTerrainGenerator.VIEWER_DIST_UPDATE);
        //viewerDistanceUpdate *= 10.0f;
        viewer = (Transform)GlobalInformation.Instance.getData(EndlessTerrainGenerator.VIEWER);

        float multiplier = density >= DENSITY_ONE ?
            density + 1 - DENSITY_ONE :
            1.0f / (DENSITY_ONE - density + 1);

        area = sectorSize / multiplier;
        scaledArea = area * scale;
        distanceThreshold = sectorSize * scale * radius * 2;
        viewerDistanceUpdate = distanceThreshold * 0.25f;
        overlapsCheckDistanceThreshold = sectorSize * scale * 2.0f;
        noiseScale = uniformness == 1 ? 0 : 1.0f / uniformness;

        int startNum = (int)(Mathf.Pow(distanceThreshold / scaledArea, 2) * 1.5f);
        objectPoolManager = new PoolManager<Vector2>(startNum, true, prefab, this.gameObject);

        requestUpdate(viewer.position);
    }


    /* ----------------------------------------------------------------------------------------- */
    void Update()
    {

        /* create objects that are ready */
        int counter = 0;
        while (!resultsQueue.isEmpty() && counter <= maxObjsForLoop)
        {
            if (!start) counter++;
            float init = Time.time;
            ObjectData data = resultsQueue.Dequeue();
            float diff = Time.time - init;
            if (!currentObjects.ContainsKey(data.gridPosition))
                createObject(data);
        }

        /* create new requests and update current objects */
        Vector2 pos = new Vector2(viewer.transform.position.x, viewer.transform.position.z);
        if (Vector2.Distance(pos, latestViewerRecordedPosition) >= viewerDistanceUpdate)
        {
            requestUpdate(viewer.position);
            updateObjects();

            latestViewerRecordedPosition = pos;
        }

        if (start && hasCollider)
            StartCoroutine(checkOverlapsAndFlatteningCoroutine());

        start = false;
    }

    #endregion


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- CREATE ------------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region CREATE

    private void requestUpdate(Vector3 viewerPosition)
    {
        ThreadStart ts = delegate
        {
            createObjects(viewerPosition);
        };

        Thread t = new Thread(ts);
        t.Start();
    }


    /* ----------------------------------------------------------------------------------------- */
    private void createObjects(Vector3 viewerPosition)
    {
        float startX = viewerPosition.x - distanceThreshold;
        float endX = viewerPosition.x + distanceThreshold;
        float startY = viewerPosition.z - distanceThreshold;
        float endY = viewerPosition.z + distanceThreshold;
        int startGridX = Mathf.RoundToInt(startX / (float)scaledArea + 0.1f);
        int endGridX = Mathf.RoundToInt(endX / (float)scaledArea + 0.1f);
        int startGridY = Mathf.RoundToInt(startY / (float)scaledArea + 0.1f);
        int endGridY = Mathf.RoundToInt(endY / (float)scaledArea + 0.1f);

        for (int x = startGridX; x <= endGridX; x++)
        {
            for (int y = startGridY; y <= endGridY; y++)
            {
                Vector2 gridPos = new Vector2(x, y);
                Vector2 viewerPos = new Vector2(viewerPosition.x, viewerPosition.z);

                if (Vector2.Distance(gridPos * scaledArea, viewerPos) > distanceThreshold)
                    continue;

                ObjectData data = getObjectData(gridPos);
                resultsQueue.Enqueue(data);
            }
        }
    }


    /* ----------------------------------------------------------------------------------------- */
    private ObjectData getObjectData(Vector2 gridPos)
    {
        // 1) calculate position ---------------------------------------------------
        Vector3 position = Vector3.zero;
        Vector3 rotation = Vector3.zero;
        Vector3 objectScale = Vector3.one;
        bool feasibility = false;

        float randomX = Mathf.PerlinNoise((gridPos.x + seedX) * 200, (gridPos.y + seedX) * 200) * positionRandomness;
        float randomY = Mathf.PerlinNoise((gridPos.x + seedY) * 200, (gridPos.y + seedY) * 200) * positionRandomness;
        float X = (gridPos.x + randomX) * area;
        float Y = (gridPos.y + randomY) * area;

        float prob = Mathf.PerlinNoise((X + seedX) * noiseScale, (Y + seedY) * noiseScale);

        if (probability >= prob)
        {
            float n = NoiseGenerator.Instance.getNoiseValue(1, X, Y);

            if (n >= minHeight && n <= maxHeight)
            {
                feasibility = true;

                /* calculate height */
                float height = GlobalInformation.Instance.getHeight(new Vector2(X, Y));
                position = new Vector3(X * scale, height * scale, Y * scale);

                /* calculate rotation */
                System.Random r = new System.Random((int)(X * Y));
                float y = (float)(r.NextDouble() * 360);
                rotation = new Vector3(0, y, 0);

                /* calculate scale */
                float s = (float)(r.NextDouble() * 2.0 - 1.0);
                s = s * scaleRandomness;
                objectScale = new Vector3(s, s, s);
            }
        }

        ObjectData data = new ObjectData(gridPos, feasibility, position, rotation, objectScale);
        return data;
    }

    #endregion


    /* ----------------------------------------------------------------------------------------- */
    /* ------------------------------- UPDATE -------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region UPDATE

    private void updateObjects()
    {
        // 1) find objects to be removed -----------------------------------------
        List<Vector2> toRemove = new List<Vector2>(currentObjects.Keys);
        foreach (Vector2 pos in toRemove)
        {
            Vector2 viewerPos = new Vector2(viewer.position.x, viewer.position.z);

            if (Vector2.Distance(pos * scaledArea, viewerPos) > distanceThreshold)
            {
                if (currentObjects[pos].feasible)
                    objectPoolManager.releaseObject(pos);

                currentObjects.Remove(pos);
            }
        }
    }

    #endregion


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- DATA RECEIVED ------------------------------------------------ */
    /* ----------------------------------------------------------------------------------------- */

    #region DATA_RECEIVED

    private void createObject(ObjectData data)
    {
        if (data.feasible)
        {
            GameObject obj = objectPoolManager.acquireObject(data.gridPosition);
            obj.transform.position = data.position;
            obj.transform.Rotate(data.rotation);
            obj.transform.localScale += data.scale;

            if (!obj.activeInHierarchy)
                obj.SetActive(true);
            obj.name = ObjectName + " " + data.position;
            data.obj = obj;


        }

        currentObjects.Add(data.gridPosition, data);
    }

    #endregion

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- DATA RECEIVED ------------------------------------------------ */
    /* ----------------------------------------------------------------------------------------- */

    #region OVERLAPS_CHECK

    IEnumerator checkOverlapsAndFlatteningCoroutine()
    {
        while (true)
        {
            float startX = viewer.position.x - overlapsCheckDistanceThreshold;
            float endX = viewer.position.x + overlapsCheckDistanceThreshold;
            float startY = viewer.position.z - overlapsCheckDistanceThreshold;
            float endY = viewer.position.z + overlapsCheckDistanceThreshold;
            int startGridX = Mathf.RoundToInt(startX / (float)scaledArea + 0.1f);
            int endGridX = Mathf.RoundToInt(endX / (float)scaledArea + 0.1f);
            int startGridY = Mathf.RoundToInt(startY / (float)scaledArea + 0.1f);
            int endGridY = Mathf.RoundToInt(endY / (float)scaledArea + 0.1f);
            Vector2 viewerPos = new Vector2(viewer.position.x, viewer.position.z);

            for (int x = startGridX; x <= endGridX; x++)
                for (int y = startGridY; y <= endGridY; y++)
                {
                    Vector2 gridPos = new Vector2(x, y);

                    if (!currentObjects.ContainsKey(gridPos))
                        continue;

                    ObjectData data = currentObjects[gridPos];
                    if (!data.feasible)
                        continue;

                    Vector2 objPos = new Vector2(data.position.x, data.position.z);
                    if (Vector2.Distance(viewerPos, objPos) > overlapsCheckDistanceThreshold)
                        continue;

                    checkOverlaps(data);

                    if (!data.feasible)
                    {
                        data.obj.SetActive(false);
                        objectPoolManager.releaseObject(gridPos);
                        continue;
                    }

                    // flattening ----
                    if (flatteningRequested && !data.flatteningDone)
                    {
                        Vector3 pos = data.position + (colliderLocalPosition * data.obj.transform.localScale.x);
                        Vector2 sizes = new Vector2(colliderSizes.x, colliderSizes.z) * data.obj.transform.localScale.x * 0.5f;
                        float radius = Mathf.Max(sizes.x, sizes.y) * 1.5f;

                        EndlessTerrainGenerator.RedrawRequest r = new EndlessTerrainGenerator.RedrawRequest(
                            new Vector2(pos.x, pos.z),
                            radius);
                        terrainGenerator.sectorRedrawRequests.Enqueue(r);
                        data.flatteningDone = true;
                    }
                }

            yield return new WaitForSeconds(5);
        }
    }


    /* ----------------------------------------------------------------------------------------- */
    private void checkOverlaps(ObjectData data)
    {
        Collider[] intersects = Physics.OverlapBox(
                data.position + (colliderLocalPosition * data.obj.transform.localScale.x),
                colliderSizes * 0.5f * data.obj.transform.localScale.x);

        foreach (Collider overlap in intersects)
        {
            if (overlap.Equals(data.obj.GetComponent<BoxCollider>()))
                continue;

            if (!overlap.gameObject.activeInHierarchy)
                continue;

            string tag = overlap.gameObject.tag;
            int p = GlobalInformation.getPriority(tag);

            if (priority <= p)
            {
                if (priority == p && acceptsSelfIntersection)
                    continue;

                data.feasible = false;
                return;
            }
        }
    }

    #endregion

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- OBJECT DATA -------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region OBJECT_DATA

    public class ObjectData
    {
        public Vector2 gridPosition;
        public bool feasible;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
        public GameObject obj;
        public bool flatteningDone;

        public ObjectData(
            Vector2 gridPosition,
            bool feasible,
            Vector3 position,
            Vector3 rotation,
            Vector3 scale)
        {
            this.gridPosition = gridPosition;
            this.feasible = feasible;
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
            flatteningDone = false;
        }
    }


    #endregion
}
