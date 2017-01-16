using UnityEngine;
using System.Collections;

public class ObjectHandler
{
    public Vector2 gridPosition;
    public Vector2 position;
    public float area;
    private int scale;
    public bool feasible;
    private Vector3 colliderLocalPosition;
    private Vector3 colliderSizes;
    private bool hasCollider;
    private int priority;
    private bool acceptsSelfIntersection;
    private bool flatteningRequested;
    private EndlessObjectGenerator parent;


    public GameObject obj;


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- CONSTRUCTOR -------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region CONSTRUCTOR

    public ObjectHandler(
        Vector2 gridPosition,
        Vector3 position,
        float area,
        int scale,
        bool feasible,
        bool acceptsSelfIntersection,
        bool flatteningRequested,
        EndlessObjectGenerator parent)
    {
        this.gridPosition = gridPosition;
        this.position = position;
        this.area = area;
        this.scale = scale;
        this.feasible = feasible;
        this.acceptsSelfIntersection = acceptsSelfIntersection;
        this.flatteningRequested = flatteningRequested;
        this.parent = parent;
    }

    #endregion

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- METHODS ------------------------------------------------------ */
    /* ----------------------------------------------------------------------------------------- */

    #region METHODS

    /* ----------------------------------------------------------------------------------------- */
    public bool initialize(GameObject obj, float scaleRandomness)
    {
        this.obj = obj;
        float n = NoiseGenerator.Instance.getNoiseValue(1, position.x / scale, position.y / scale);

        /* calculate height */
        float height = GlobalInformation.Instance.getHeight(new Vector2(position.x / scale, position.y / scale));
        obj.transform.position = new Vector3(position.x, height, position.y);
        //position = obj.transform.position;

        /* calculate rotation */
        System.Random r = new System.Random((int)(position.x * position.y));
        float y = (float)(r.NextDouble() * 360);
        obj.transform.rotation = Quaternion.identity;
        obj.transform.Rotate(new Vector3(0, y, 0));

        /* calculate scale */
        float s = (float)(r.NextDouble() * 2.0 - 1.0);
        s = s * scaleRandomness;
        obj.transform.localScale += new Vector3(s, s, s);

        if (checkOverlaps())
        {
            feasible = false;
            return false;
        }

        if (flatteningRequested)
            requestFlattening();

        if (!obj.activeInHierarchy)
            obj.SetActive(true);

        return true;
    }


    /* ----------------------------------------------------------------------------------------- */
    public void reset()
    {
        obj.transform.position = Vector3.zero;
        obj.transform.rotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
        obj.SetActive(false);
        obj = null;
    }


    /* ----------------------------------------------------------------------------------------- */
    private bool checkOverlaps()
    {
        Collider[] intersects = Physics.OverlapBox(
                obj.transform.position + (colliderLocalPosition * obj.transform.localScale.x),
                colliderSizes * 0.5f * obj.transform.localScale.x);

        foreach (Collider overlap in intersects)
        {
            if (overlap.Equals(obj.GetComponent<BoxCollider>()))
                continue;

            if (!overlap.gameObject.activeInHierarchy)
                continue;

            string tag = overlap.gameObject.tag;
            int p = GlobalInformation.getPriority(tag);

            if (priority <= p)
            {
                if (priority == p && acceptsSelfIntersection)
                    continue;

                return true;
            }
        }

        return false;
    }


    /* ----------------------------------------------------------------------------------------- */
    private void requestFlattening()
    {
        Vector3 pos = obj.transform.position + (colliderLocalPosition * obj.transform.localScale.x);
        Vector2 sizes = new Vector2(colliderSizes.x, colliderSizes.z) * obj.transform.localScale.x * 0.5f;
        float radius = Mathf.Max(sizes.x, sizes.y) * 1.5f;

        EndlessTerrainGenerator.RedrawRequest r = new EndlessTerrainGenerator.RedrawRequest(
            new Vector2(pos.x, pos.z),
            radius);

        EndlessTerrainGenerator.Instance.sectorRedrawRequests.Enqueue(r);
    }

    #endregion

}
