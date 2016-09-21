using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EndlessTerrainGenerator : MonoBehaviour {

    [Range(1, 7)]
    public int chunkDimension;
    private int chunkSize;

    public Transform viewer;
    private Vector3 latestViewerRecordedPosition;

    [Range(1,10)]
    public int viewerDistanceUpdateFrequency;
    private float viewerDistanceUpdate;

    private Dictionary<Vector2,MapChunk> TerrainChunks;

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- MY CLASSES --------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    public class MapChunk
    {
        public Vector2 position;
        public int size;
        public GameObject mapChunkObject;

        public MapChunk(int x, int y, int size)
        {
            this.position = new Vector2(x, y);
            this.size = size;
            mapChunkObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            mapChunkObject.transform.position = new Vector3(x * size, 0, y * size);
        }
    }

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- UNITY CALLBACKS ---------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    void Start()
    {
        chunkSize = (chunkDimension * 32 + 1);
        viewerDistanceUpdate = chunkSize / (float)(viewerDistanceUpdateFrequency + 3);

        Debug.Log("dist update: " + viewerDistanceUpdate);
        viewer.position = new Vector3(0, 0, 0);

        MapChunk chunk = new MapChunk(0, 0, chunkSize);

        TerrainChunks = new Dictionary<Vector2, MapChunk>();
        TerrainChunks.Add(new Vector2(0, 0), chunk);

        this.GetComponent<MapGenerator>().GenerateMap(0, 0, chunk);
    }


    /* ----------------------------------------------------------------------------------------- */
    void Update()
    {
        float distance = Vector3.Distance(latestViewerRecordedPosition, viewer.position);

        if(distance >= viewerDistanceUpdate)
        {
            // update terrain chunks
            float x = viewer.position.x / chunkSize;
            float y = viewer.position.y / chunkSize;

            int chunkX = Mathf.RoundToInt(x);
            int chunkY = Mathf.RoundToInt(y);
            Debug.Log("chunkX = " + chunkX + "; chunkY = " + chunkY);

            updateChunks(chunkX, chunkY);
            latestViewerRecordedPosition = viewer.position;
        }
    }


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- MY FUNCTIONS ------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    private void updateChunks(int x, int y)
    {
        Vector2 chunkPosition = new Vector2(x, y);

        if (!TerrainChunks.ContainsKey(chunkPosition))
        {
            MapChunk newChunk = new MapChunk(x, y, chunkSize);
            TerrainChunks.Add(chunkPosition, newChunk);
            this.GetComponent<MapGenerator>().GenerateMap(x, y, newChunk);
        }
    }
}
