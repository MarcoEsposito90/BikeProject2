using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

public class EndlessObjectGenerator : MonoBehaviour
{

    //public static readonly string DIST_THRESHOLD = "EndlessObjectGenerator.DistanceThreshold";

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- ATTRIBUTES --------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region ATTRIBUTES

    [Range(1, 1000)]
    public int seed;

    private const int DENSITY_ONE = 10;
    [Range(1, 50)]
    public int density;

    [Range(1, 30)]
    public int randomness;

    [Range(1, 100)]
    public int uniformness;

    [Range(0.0f, 1.0f)]
    public float probability;

    [Range(1, 5)]
    public int radius;

    [Range(0.0f, 1.0f)]
    public float minHeight;

    [Range(0.0f, 1.0f)]
    public float maxHeight;

    [Range(1, 5)]
    public int numberOfLods;

    public GameObject prefab;

    BlockingQueue<ObjectData> resultsQueue;
    private Dictionary<Vector2, ObjectHandler> currentObjects;
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
    private float noiseScale;
    private int maxObjsForLoop = 50;
    private bool start = true;
    //private float[] LODDistances;

    #endregion

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- UNITY CALLBACKS ---------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region UNITY

    void Awake()
    {
        currentObjects = new Dictionary<Vector2, ObjectHandler>();
        resultsQueue = new BlockingQueue<ObjectData>();
    }


    /* ----------------------------------------------------------------------------------------- */
    void Start()
    {
        System.Random random = new System.Random(seed);
        seedX = ((float)random.NextDouble()) * random.Next(1000);
        seedY = ((float)random.NextDouble()) * random.Next(1000);

        sectorSize = (int)GlobalInformation.Instance.getData(EndlessTerrainGenerator.SECTOR_SIZE);
        scale = (int)GlobalInformation.Instance.getData(EndlessTerrainGenerator.SCALE);
        viewerDistanceUpdate = (float)GlobalInformation.Instance.getData(EndlessTerrainGenerator.VIEWER_DIST_UPDATE);
        viewerDistanceUpdate *= 10.0f;
        viewer = (Transform)GlobalInformation.Instance.getData(EndlessTerrainGenerator.VIEWER);

        float multiplier = density >= DENSITY_ONE ?
            density + 1 - DENSITY_ONE :
            1.0f / (DENSITY_ONE - density + 1);

        area = sectorSize / multiplier;
        scaledArea = area * scale;
        distanceThreshold = sectorSize * scale * radius * 2;
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
        if(start)
            while (!resultsQueue.isEmpty() && counter <= maxObjsForLoop)
            {
                if (!start) counter++;
                float init = Time.time;
                ObjectData data = resultsQueue.Dequeue();
                float diff = Time.time - init;
                if (!currentObjects.ContainsKey(data.gridPosition))
                    createObject(data);
            }

        //Debug.Log("created " + counter + " objects");

        /* create new requests and update current objects */
        //Vector2 pos = new Vector2(viewer.transform.position.x, viewer.transform.position.z);
        //if (Vector2.Distance(pos, latestViewerRecordedPosition) >= viewerDistanceUpdate)
        //{
        //    //requestUpdate(viewer.position);
        //    //updateObjects();

        //    latestViewerRecordedPosition = pos;
        //    Debug.Log("current size = " + objectPoolManager.currentSize);
        //}

        start = false;
    }

    #endregion


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- METHODS ------------------------------------------------------ */
    /* ----------------------------------------------------------------------------------------- */

    #region CREATE_AND_UPDATE

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
        //ObjectHandler oh = null;

        // 1) calculate position ---------------------------------------------------
        Vector3 position = Vector3.zero;
        Vector3 rotation = Vector3.zero;
        bool feasibility = false;

        float randomX = Mathf.PerlinNoise((gridPos.x + seedX) * 200, (gridPos.y + seedX) * 200) * randomness;
        float randomY = Mathf.PerlinNoise((gridPos.x + seedY) * 200, (gridPos.y + seedY) * 200) * randomness;
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
                float height = GlobalInformation.Instance.getHeight(n);
                position = new Vector3(X * scale, height * scale, Y * scale);

                /* calculate rotation */
                System.Random r = new System.Random();
                float y = (float)(r.NextDouble() * 360);
                rotation = new Vector3(0, y, 0);

                /* instantiate and initialize */
                //GameObject newObj = objectPoolManager.acquireObject(gridPos);
                //oh = newObj.GetComponent<ObjectHandler>();
                //oh.initialize(pos, rot);
            }
        }

        // 4) add it anyway to the list, to avoid continous updates --------------------
        //currentObjects.Add(gridPos, oh);
        ObjectData data = new ObjectData(gridPos, feasibility, position, rotation);
        return data;
    }


    /* ----------------------------------------------------------------------------------------- */
    private void createObject(ObjectData data)
    {
        ObjectHandler oh = null;
        if (data.feasible)
        {
            GameObject obj = objectPoolManager.acquireObject(data.gridPosition);
            obj.transform.position = data.position;
            obj.transform.Rotate(data.rotation);

            if (!obj.activeInHierarchy)
                obj.SetActive(true);
            obj.name = " " + data.position;
        }

        currentObjects.Add(data.gridPosition, oh);
    }


    /* ----------------------------------------------------------------------------------------- */
    private void updateObjects()
    {

        // 1) find objects to be removed -----------------------------------------
        List<Vector2> toRemove = new List<Vector2>();
        foreach (Vector2 pos in currentObjects.Keys)
        {
            Vector2 viewerPos = new Vector2(viewer.position.x, viewer.position.z);

            if (Vector2.Distance(pos * scaledArea, viewerPos) > distanceThreshold)
                toRemove.Add(pos);
        }

        // 2) remove actually ----------------------------------------------------
        for (int i = 0; i < toRemove.Count; i++)
        {
            ObjectHandler oh = currentObjects[toRemove[i]];

            //if (oh != null)
            //{
            //    oh.reset();
            //}

            if (currentObjects.ContainsKey(toRemove[i]))
                objectPoolManager.releaseObject(toRemove[i]);

            currentObjects.Remove(toRemove[i]);
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

        public ObjectData(Vector2 gridPosition, bool feasible, Vector3 position, Vector3 rotation)
        {
            this.gridPosition = gridPosition;
            this.feasible = feasible;
            this.position = position;
            this.rotation = rotation;
        }
    }


    #endregion
}
