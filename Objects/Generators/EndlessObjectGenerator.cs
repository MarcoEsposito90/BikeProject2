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

    private const int DENSITY_ONE = 15;
    [Range(1, DENSITY_ONE * 2)]
    public int density;

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
        seedX = ((float)random.NextDouble()) * random.Next(100);
        seedY = ((float)random.NextDouble()) * random.Next(100);

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
    /* -------------------------- METHODS ------------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

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
                {
                    GameObject newObj = objectPoolManagers.acquireObject(gridPos);
                    ObjectHandler oh = newObj.GetComponent<ObjectHandler>();
                    currentObjects.Add(gridPos, oh);
                    oh.computePosition(gridPos, seedX, seedY, area, scale);
                }
            }
        }
    }


    private void updateObjects()
    {
        List<Vector2> toRemove = new List<Vector2>();
        foreach(Vector2 pos in currentObjects.Keys)
        {
            Vector2 viewerPos = new Vector2(viewer.position.x, viewer.position.z);

            if (Vector2.Distance(pos * scaledArea, viewerPos) > distanceThreshold)
                toRemove.Add(pos);
        }

        for(int i = 0; i < toRemove.Count; i++)
        {
            ObjectHandler oh = currentObjects[toRemove[i]];
            oh.reset();
            objectPoolManagers.releaseObject(toRemove[i]);
            currentObjects.Remove(toRemove[i]);
        }
    }

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
