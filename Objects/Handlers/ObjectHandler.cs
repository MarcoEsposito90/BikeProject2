using UnityEngine;
using System.Collections;

public class ObjectHandler
{
    private GameObject obj;


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- METHODS ------------------------------------------------------ */
    /* ----------------------------------------------------------------------------------------- */

    #region METHODS

    public void initialize(Vector3 position, Vector3 rotation)
    {
        obj.transform.position = position;
        obj.transform.Rotate(rotation);

        if (!obj.activeInHierarchy)
            obj.SetActive(true);
    }


    /* ----------------------------------------------------------------------------------------- */
    public void reset()
    {
        obj.transform.position = Vector3.zero;
        obj.transform.rotation = Quaternion.identity;
        obj.SetActive(false);
    }

    #endregion
}
