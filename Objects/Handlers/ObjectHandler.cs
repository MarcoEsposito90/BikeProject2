using UnityEngine;
using System.Collections;

public class ObjectHandler
{
    public Vector2 gridPosition;
    public Vector2 position;
    public float area;
    private int scale;
    public bool feasible;
    private bool hasCollider;
    private int priority;
    private bool acceptsSelfIntersection;
    private bool flatteningRequested;
    private EndlessObjectGenerator parent;

    public BoxCollider collider { get; private set; }
    public GameObject obj { get; private set; }


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
        height *= scale;
        obj.transform.position = new Vector3(position.x, height, position.y);

        /* calculate rotation */
        System.Random r = new System.Random((int)(position.x * position.y));
        float y = (float)(r.NextDouble() * 360);
        obj.transform.rotation = Quaternion.identity;
        obj.transform.Rotate(new Vector3(0, y, 0));

        /* calculate scale */
        float s = (float)(r.NextDouble() * 2.0 - 1.0);
        s = s * scaleRandomness;
        obj.transform.localScale += new Vector3(s, s, s);

        /* get collider */
        collider = obj.GetComponent<BoxCollider>();

        if (collider != null)
        {
            Debug.Log("collider. position = " + collider.center + "; sizes = " + collider.size);
            priority = GlobalInformation.getPriority(obj.tag);

            // collisions check
            if (checkOverlaps())
            {
                feasible = false;
                return false;
            }

            // flattening
            if (flatteningRequested)
                requestFlattening();
        }


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
    public bool checkOverlaps()
    {
        if (collider == null)
            return false;

        Vector3 center = obj.transform.position + (collider.center * obj.transform.localScale.x);
        Vector3 sizes = collider.size * 0.5f * obj.transform.localScale.x;
        Collider[] intersects = Physics.OverlapBox(center, sizes, obj.transform.rotation);

        Debug.Log("checking overlaps " + gridPosition + ": " + center + "; " + sizes);
        foreach (Collider overlap in intersects)
        {
            Debug.Log("overlap " + overlap);    
            if (overlap.Equals(obj.GetComponent<BoxCollider>()))
                continue;

            if (!overlap.gameObject.activeInHierarchy)
                continue;

            string tag = overlap.gameObject.tag;
            int p = GlobalInformation.getPriority(tag);
            Debug.Log("priority = " + p);

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
        Vector3 pos = obj.transform.position + (collider.center * obj.transform.localScale.x);
        Vector2 sizes = new Vector2(collider.size.x, collider.size.z) * obj.transform.localScale.x * 0.5f;
        float radius = Mathf.Max(sizes.x, sizes.y) * 1.5f;

        EndlessTerrainGenerator.RedrawRequest r = new EndlessTerrainGenerator.RedrawRequest(
            new Vector2(pos.x, pos.z),
            radius);

        EndlessTerrainGenerator.Instance.sectorRedrawRequests.Enqueue(r);
    }

    #endregion

}
