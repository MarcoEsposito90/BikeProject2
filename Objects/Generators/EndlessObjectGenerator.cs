﻿using UnityEngine;
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
    BlockingQueue<ObjectHandler> resultsQueue;
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
        currentObjects = new Dictionary<Vector2, ObjectHandler>();
        resultsQueue = new BlockingQueue<ObjectHandler>();

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
            ObjectHandler handler = resultsQueue.Dequeue();
            if (!currentObjects.ContainsKey(handler.gridPosition))
                createObject(handler);
        }

        /* create new requests and update current objects */
        Vector2 pos = new Vector2(viewer.transform.position.x, viewer.transform.position.z);
        if (Vector2.Distance(pos, latestViewerRecordedPosition) >= viewerDistanceUpdate)
        {
            requestUpdate(viewer.position);
            updateObjects();

            latestViewerRecordedPosition = pos;
        }

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

                ObjectHandler handler = getObjectData(gridPos);
                resultsQueue.Enqueue(handler);
            }
        }
    }


    /* ----------------------------------------------------------------------------------------- */
    private ObjectHandler getObjectData(Vector2 gridPos)
    {
        // 1) calculate position ---------------------------------------------------
        Vector2 position = Vector2.zero;
        bool feasibility = false;

        float randomX = Mathf.PerlinNoise((gridPos.x + seedX) * 200, (gridPos.y + seedX) * 200) * positionRandomness;
        float randomY = Mathf.PerlinNoise((gridPos.x + seedY) * 200, (gridPos.y + seedY) * 200) * positionRandomness;
        float X = (gridPos.x + randomX) * area;
        float Y = (gridPos.y + randomY) * area;
        position = new Vector2(X * scale, Y * scale);

        float prob = Mathf.PerlinNoise((X + seedX) * noiseScale, (Y + seedY) * noiseScale);

        if (probability >= prob)
        {
            float n = NoiseGenerator.Instance.getNoiseValue(1, X, Y);

            if (n >= minHeight && n <= maxHeight)
                feasibility = true;
        }

        ObjectHandler h = new ObjectHandler(
            gridPos,
            position,
            area, 
            scale, 
            feasibility, 
            acceptsSelfIntersection, 
            flatteningRequested, 
            this);
        return h;
    }


    /* ----------------------------------------------------------------------------------------- */
    private void createObject(ObjectHandler handler)
    {
        if (handler.feasible)
        {
            GameObject obj = objectPoolManager.acquireObject(handler.gridPosition);
            obj.name = ObjectName + " " + handler.position;

            if (!handler.initialize(obj, scaleRandomness))
                releaseObject(handler);
        }

        currentObjects.Add(handler.gridPosition, handler);
    }

    #endregion


    /* ----------------------------------------------------------------------------------------- */
    /* ------------------------------- UPDATE -------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region UPDATE

    /* ----------------------------------------------------------------------------------------- */
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
                    releaseObject(currentObjects[pos]);

                currentObjects.Remove(pos);
            }
        }
    }

    
    /* ----------------------------------------------------------------------------------------- */
    private void releaseObject(ObjectHandler handler)
    {
        handler.reset();
        objectPoolManager.releaseObject(handler.gridPosition);
    }

    #endregion

}
