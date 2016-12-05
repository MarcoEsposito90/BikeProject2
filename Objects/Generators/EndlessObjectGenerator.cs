using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EndlessObjectGenerator : MonoBehaviour
{

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

    [Range(1, 20)]
    public int uniformness;

    [Range(0.0f, 1.0f)]
    public float probability;

    [Range(1, 5)]
    public int radius;

    [Range(0.0f, 1.0f)]
    public float minHeight;

    [Range(0.0f, 1.0f)]
    public float maxHeight;

    public GenerableObject[] prefabs;

    private Dictionary<Vector2, ObjectHandler> currentObjects;
    public BlockingQueue<Vector2> destroyRequestQueues { get; private set; }
    private PoolManager<Vector2> objectPoolManagers;

    private int sectorSize;
    private int scale;
    private Transform viewer;
    private Vector2 latestViewerRecordedPosition;
    private float viewerDistanceUpdate;
    private float seedX, seedY;

    private float area;
    private float scaledArea;
    private float distanceThreshold;

    #endregion

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- UNITY CALLBACKS ---------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region UNITY

    void Awake()
    {
        currentObjects = new Dictionary<Vector2, ObjectHandler>();
        destroyRequestQueues = new BlockingQueue<Vector2>();
        objectPoolManagers = new PoolManager<Vector2>(20, true, prefabs[0].prefab, this.gameObject);
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
        viewer = (Transform)GlobalInformation.Instance.getData(EndlessTerrainGenerator.VIEWER);

        float multiplier = density >= DENSITY_ONE ?
            density + 1 - DENSITY_ONE :
            1.0f / (DENSITY_ONE - density + 1);

        area = sectorSize / multiplier;
        scaledArea = area * scale;
        distanceThreshold = sectorSize * scale * radius * 2;

        createObjects();
    }


    /* ----------------------------------------------------------------------------------------- */
    void Update()
    {
        Vector2 pos = new Vector2(viewer.transform.position.x, viewer.transform.position.z);

        if(Vector2.Distance(pos, latestViewerRecordedPosition) >= viewerDistanceUpdate)
        {
            createObjects();
            updateObjects();
            latestViewerRecordedPosition = pos;
        }
    }

    #endregion


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- METHODS ------------------------------------------------------ */
    /* ----------------------------------------------------------------------------------------- */

    #region CREATE_AND_UPDATE

    private void createObjects()
    {
        float startX = viewer.position.x - distanceThreshold;
        float endX = viewer.position.x + distanceThreshold;
        float startY = viewer.position.z - distanceThreshold;
        float endY = viewer.position.z + distanceThreshold;

        for (float x = startX; x <= endX; x += scaledArea)
        {
            for (float y = startY; y <= endY; y += scaledArea)
            {
                int gridX = Mathf.RoundToInt(x / (float)scaledArea + 0.1f);
                int gridY = Mathf.RoundToInt(y / (float)scaledArea + 0.1f);
                Vector2 gridPos = new Vector2(gridX, gridY);
                Vector2 viewerPos = new Vector2(viewer.position.x, viewer.position.z);

                if (Vector2.Distance(gridPos * scaledArea, viewerPos) > distanceThreshold)
                    continue;

                if (!currentObjects.ContainsKey(gridPos))
                    createObject(gridPos);
            }
        }
    }


    /* ----------------------------------------------------------------------------------------- */
    private void createObject(Vector2 gridPos)
    {
        ObjectHandler oh = null;

        // 1) calculate position ---------------------------------------------------
        float randomX = Mathf.PerlinNoise((gridPos.x + seedX) * 200, (gridPos.y + seedX) * 200) * randomness;
        float randomY = Mathf.PerlinNoise((gridPos.x + seedY) * 200, (gridPos.y + seedY) * 200) * randomness;
        float X = (gridPos.x + randomX) * area;
        float Y = (gridPos.y + randomY) * area;
        //Debug.Log(gridPos + " - rand = " + randomX + "," + randomY);
        //Debug.Log(gridPos + " - pos = " + X + "," + Y);

        // 2) check the probability ------------------------------------------------
        float noisescale = uniformness == 1 ? 0 : 1.0f / (uniformness * 100);
        float prob = Mathf.PerlinNoise((X + seedX) * noisescale, (Y + seedY) * noisescale);
        //Debug.Log(gridPos + " - prob = " + prob);

        if (probability >= prob)
        {
            /* create the object only if it is in the height range */
            float n = NoiseGenerator.Instance.getNoiseValue(1, X, Y);

            if(n >= minHeight && n <= maxHeight)
            {
                /* calculate height */
                float height = GlobalInformation.Instance.getHeight(n);
                Vector3 pos = new Vector3(X * scale, height * scale, Y * scale);

                /* calculate rotation */
                System.Random r = new System.Random();
                float y = (1.0f / r.Next(100)) * 100 * 360;
                Vector3 rot = new Vector3(0, y, 0);

                /* instantiate and initialize */
                GameObject newObj = objectPoolManagers.acquireObject(gridPos);
                oh = newObj.GetComponent<ObjectHandler>();
                oh.initialize(pos, rot);
            }
        }

        // 4) add it anyway to the list, to avoid continous updates --------------------
        currentObjects.Add(gridPos, oh);
    }


    /* ----------------------------------------------------------------------------------------- */
    private void updateObjects()
    {

        // 1) find objects to be removed -----------------------------------------
        List<Vector2> toRemove = new List<Vector2>();
        foreach(Vector2 pos in currentObjects.Keys)
        {
            Vector2 viewerPos = new Vector2(viewer.position.x, viewer.position.z);

            if (Vector2.Distance(pos * scaledArea, viewerPos) > distanceThreshold)
                toRemove.Add(pos);
        }

        // 2) remove actually ----------------------------------------------------
        for (int i = 0; i < toRemove.Count; i++)
        {
            ObjectHandler oh = currentObjects[toRemove[i]];

            if(oh != null)
            {
                oh.reset();
                objectPoolManagers.releaseObject(toRemove[i]);
            }

            currentObjects.Remove(toRemove[i]);
        }
    }

    #endregion

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- OBJECT ------------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region OBJECT

    [System.Serializable]
    public class GenerableObject
    {
        public string name;
        public GameObject prefab;
        public int subDensity;
    }

    #endregion

}
