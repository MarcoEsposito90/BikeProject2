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

    [Range(5, 500)]
    public int maxObjsForLoop;

    //public bool checkOverlapsEnabled = true;
    //public bool acceptsSelfIntersection;
    public bool flatteningRequested;
    public float flatteningRadius;

    public GameObject prefab;
    public EndlessTerrainGenerator terrainGenerator;

    /* ----------------------------------------------------------------------------------------- */
    BlockingQueue<ObjectHandler> resultsQueue;
    BlockingQueue<ObjectHandler> removeQueue;
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
    private bool start;

    #endregion

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- UNITY CALLBACKS ---------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region UNITY

    void Awake()
    {
        currentObjects = new Dictionary<Vector2, ObjectHandler>();
        resultsQueue = new BlockingQueue<ObjectHandler>();
        removeQueue = new BlockingQueue<ObjectHandler>();
    }


    /* ----------------------------------------------------------------------------------------- */
    void Start()
    {
        NoiseGenerator.Instance.OnSectorChanged += OnSectorChange;

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
        start = true;

        updateAsynch(viewer.position);
    }


    /* ----------------------------------------------------------------------------------------- */
    void Update()
    {
        /* create objects that are ready */
        int counter = 0;
        lock (resultsQueue)
        {
            while (!resultsQueue.isEmpty())
            {
                if (counter > maxObjsForLoop && !start)
                    break;

                ObjectHandler handler = resultsQueue.Dequeue();
                if (!currentObjects.ContainsKey(handler.gridPosition))
                    createObject(handler);

                counter++;
            }
        }

        lock (removeQueue)
        {
            while (!removeQueue.isEmpty())
            {
                if (counter > maxObjsForLoop && !start)
                    break;

                ObjectHandler handler = removeQueue.Dequeue();
                releaseObject(handler);
                currentObjects.Remove(handler.gridPosition);
                counter++;
            }
        }

        /* create new requests and update current objects */
        Vector2 pos = new Vector2(viewer.transform.position.x, viewer.transform.position.z);
        if (Vector2.Distance(pos, latestViewerRecordedPosition) >= viewerDistanceUpdate)
        {
            updateAsynch(viewer.position);
            latestViewerRecordedPosition = pos;
        }

        start = false;
    }


    /* ----------------------------------------------------------------------------------------- */
    private void updateAsynch(Vector3 viewerPosition)
    {
        ThreadStart ts = delegate
        {
            lock (resultsQueue)
            {
                createObjects(viewerPosition);
                updateObjects(viewerPosition);
            }
        };

        Thread t = new Thread(ts);
        t.Start();
    }

    #endregion


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- CREATE ------------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region CREATE

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
                if (currentObjects.ContainsKey(gridPos))
                    continue;

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
            flatteningRequested,
            flatteningRadius,
            this);
        return h;
    }


    /* ----------------------------------------------------------------------------------------- */
    private void createObject(ObjectHandler handler)
    {
        if (handler.feasible)
        {
            GameObject obj = objectPoolManager.acquireObject(handler.gridPosition);
            obj.name = ObjectName + " " + handler.gridPosition;
            handler.initializePrefab(obj, scaleRandomness);

            ObjectCollisionHandler och = obj.GetComponentInChildren<ObjectCollisionHandler>();
            if (och != null)
            {
                och.gridPosition = handler.gridPosition;
                och.onCollision += onObjectCollision;
            }
        }

        currentObjects.Add(handler.gridPosition, handler);
    }

    #endregion


    /* ----------------------------------------------------------------------------------------- */
    /* ------------------------------- UPDATE -------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region UPDATE

    /* ----------------------------------------------------------------------------------------- */
    private void updateObjects(Vector3 viewerPosition)
    {
        // 1) find objects to be removed -----------------------------------------
        List<Vector2> toRemove = new List<Vector2>(currentObjects.Keys);
        foreach (Vector2 pos in toRemove)
        {
            Vector2 viewerPos = new Vector2(viewerPosition.x, viewerPosition.z);
            float d = Vector2.Distance(pos * scaledArea, viewerPos);

            if (d > distanceThreshold)
            {
                if (currentObjects[pos].feasible)
                    removeQueue.Enqueue(currentObjects[pos]);
            }
        }
    }


    /* ----------------------------------------------------------------------------------------- */
    private void releaseObject(ObjectHandler handler)
    {
        if (handler.obj == null)
            return;

        handler.resetPrefab();
        objectPoolManager.releaseObject(handler.gridPosition);
    }

    #endregion


    /* ----------------------------------------------------------------------------------------- */
    /* ------------------------------- NOTIFICATIONS ------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region NOTIFICATIONS

    private void OnSectorChange(Vector2 sectorGridPos)
    {
        //int sectorSize = (int)GlobalInformation.Instance.getData(EndlessTerrainGenerator.SECTOR_SIZE);
        foreach (ObjectHandler o in currentObjects.Values)
        {
            if (!o.feasible)
                continue;

            int scaledSectorSize = scale * sectorSize;
            Vector2 center = sectorGridPos * scaledSectorSize;

            if (o.position.x >= center.x - (scaledSectorSize / 2.0f) &&
                o.position.x <= center.x + (scaledSectorSize / 2.0f) &&
                o.position.y >= center.y - (scaledSectorSize / 2.0f) &&
                o.position.y <= center.y + (scaledSectorSize / 2.0f))
            {
                o.updatePrefab();
            }
        }
    }


    /* ----------------------------------------------------------------------------------------- */
    private void onObjectCollision(Vector2 gridPosition)
    {
        if(!currentObjects.ContainsKey(gridPosition))
            return;

        ObjectHandler oh = currentObjects[gridPosition];
        releaseObject(oh);
        oh.feasible = false;
    }

    #endregion

}
