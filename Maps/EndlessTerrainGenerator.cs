using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EndlessTerrainGenerator : MonoBehaviour {

    [Range(1, 7)]
    public int chunkDimension;

    public Transform viewer;
    public float viewerDistanceUpdate;

    private List<MapChunk> TerrainChunks;

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- MY CLASSES --------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    public class MapChunk
    {
        public int x;
        public int y;
        public int size;
        public GameObject mapChunkObject;

        public MapChunk(int x, int y, int size)
        {
            this.x = x;
            this.y = y;
            this.size = size;
            mapChunkObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            mapChunkObject.transform.position = new Vector3(x * size, 0, y * size);
        }
    }

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- MY FUNCTIONS ------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    void Start()
    {
        viewer.position = new Vector3(0, 0, 0);

        MapChunk chunk = new MapChunk(0, 0, chunkDimension * 32 + 1);

        TerrainChunks = new List<MapChunk>();
        TerrainChunks.Add(chunk);

        this.GetComponent<MapGenerator>().GenerateMap(0, 0, chunk);
    }



}
