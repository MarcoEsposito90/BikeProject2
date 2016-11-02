using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PoolManager<Key>
{

    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- ATTRIBUTES ----------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #region ATTRIBUTES

    private int startSize;
    private bool canGrow;
    private GameObject prefab;
    private GameObject parent;
    private Dictionary<Key, GameObject> objectsMap;
    private Queue<GameObject> freeObjectsList;

    #endregion


    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- CONSTRUCTOR ---------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #region CONTRUCTOR

    public PoolManager(int startSize, bool canGrow, GameObject prefab, GameObject parent)
    {
        this.startSize = startSize;
        this.canGrow = canGrow;
        this.prefab = prefab;
        this.parent = parent;

        objectsMap = new Dictionary<Key, GameObject>();
        freeObjectsList = new Queue<GameObject>();
        for (int i = 0; i < startSize; i++)
            addObject();
    }

    #endregion


    /* ------------------------------------------------------------------------------------------------- */
    /* -------------------------------- METHODS -------------------------------------------------------- */
    /* ------------------------------------------------------------------------------------------------- */

    #region METHODS

    /* ------------------------------------------------------------------------------------------------- */
    private void addObject()
    {
        GameObject newObj = (GameObject)Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
        newObj.SetActive(false);
        newObj.transform.parent = this.parent.transform;
        freeObjectsList.Enqueue(newObj);
    }


    /* ------------------------------------------------------------------------------------------------- */
    public GameObject acquireObject(Key key)
    {
        if (objectsMap.ContainsKey(key))
        {
            Debug.Log("PoolManager.acquireObject - warning! Trying to acquire object not free");
            return null;
        }

        if(freeObjectsList.Count == 0)
        {
            if (!canGrow) return null;
            addObject();
        }

        GameObject newObj = freeObjectsList.Dequeue();
        objectsMap.Add(key, newObj);
        return newObj;
    }


    /* ------------------------------------------------------------------------------------------------- */
    public void releaseObject(Key key)
    {
        if (!objectsMap.ContainsKey(key))
        {
            Debug.Log("PoolManager.releaseObject - warning! Trying to release object not in list");
            return;
        }

        GameObject oldObj = objectsMap[key];
        objectsMap.Remove(key);

        if (oldObj.activeInHierarchy)
            oldObj.SetActive(false);

        freeObjectsList.Enqueue(oldObj);
    }

    #endregion
}
