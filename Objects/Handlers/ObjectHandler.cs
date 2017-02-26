using UnityEngine;
using System.Collections;
using System.Threading;

public class ObjectHandler
{
    public Vector2 gridPosition;
    public Vector2 position;
    public float area;
    private int scale;
    public bool feasible;
    //private int priority;
    //private bool acceptsSelfIntersection;
    private bool flatteningRequested;
    private float flatteningRadius;
    private EndlessObjectGenerator parent;

    private float height;
    private System.Random random;

    //public BoxCollider collider { get; private set; }
    public GameObject obj { get; private set; }


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- CONSTRUCTOR -------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region CONSTRUCTOR

    public ObjectHandler(
        Vector2 gridPosition,
        Vector2 position,
        float area,
        int scale,
        bool feasible,
        bool flatteningRequested,
        float flatteningRadius,
        EndlessObjectGenerator parent)
    {
        this.gridPosition = gridPosition;
        this.position = position;
        this.area = area;
        this.scale = scale;
        this.feasible = feasible;
        this.flatteningRequested = flatteningRequested;
        this.flatteningRadius = flatteningRadius;
        this.parent = parent;
        random = new System.Random((int)(position.x * position.y));
    }

    #endregion

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- PREFAB ------------------------------------------------------ */
    /* ----------------------------------------------------------------------------------------- */

    #region PREFAB

    /* ----------------------------------------------------------------------------------------- */
    public void computeHeight()
    {
        float n = NoiseGenerator.Instance.getNoiseValue(1, position.x / scale, position.y / scale);
        height = GlobalInformation.Instance.getHeight(new Vector2(position.x / scale, position.y / scale));
        height *= scale;
    }


    /* ----------------------------------------------------------------------------------------- */
    public void initializePrefab(GameObject obj, float scaleRandomness)
    {
        computeHeight();

        this.obj = obj;
        obj.transform.position = new Vector3(position.x, height, position.y);

        /* calculate rotation */
        float y = (float)(random.NextDouble() * 360);
        obj.transform.rotation = Quaternion.identity;
        obj.transform.Rotate(new Vector3(0, y, 0));

        /* calculate scale */
        float s = (float)(random.NextDouble() * 2.0 - 1.0);
        s = s * scaleRandomness;
        obj.transform.localScale += new Vector3(s, s, s);

        if (flatteningRequested)
            requestFlattening();

        Debug.Log(obj.name + " setting active");
        if (!obj.activeInHierarchy)
            obj.SetActive(true);
    }


    /* ----------------------------------------------------------------------------------------- */
    public void updatePrefab()
    {
        computeHeight();
        obj.transform.position = new Vector3(position.x, height, position.y);
    }



    /* ----------------------------------------------------------------------------------------- */
    public void resetPrefab()
    {
        obj.transform.position = Vector3.zero;
        obj.transform.rotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
        obj.SetActive(false);
        obj = null;
    }

    #endregion


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- FLATTENING --------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region FLATTENING

    private void requestFlattening()
    {
        Vector3 pos = obj.transform.position;
        Vector2 worldPos = new Vector2(pos.x, pos.z);

        NoiseGenerator.Instance.redrawRequest(worldPos, flatteningRadius);
    }

    #endregion

}
