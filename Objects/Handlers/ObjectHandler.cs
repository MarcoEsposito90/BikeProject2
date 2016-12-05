using UnityEngine;
using System.Collections;

public class ObjectHandler : MonoBehaviour
{

    public string objectName;
    public GameObject[] lods;
    private Vector2 latestViewerUpdate;
    private float[] LODDistances;
    private int currentLOD;

    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- UNITY -------------------------------------------------------- */
    /* ----------------------------------------------------------------------------------------- */

    #region UNITY

    void Start()
    {
    }


    /* ----------------------------------------------------------------------------------------- */
    void Update()
    {
        Transform viewer = (Transform)GlobalInformation.Instance.getData(EndlessTerrainGenerator.VIEWER);
        float viewerDistanceUpdate = (float)GlobalInformation.Instance.getData(EndlessTerrainGenerator.VIEWER_DIST_UPDATE);

        Vector2 pos = new Vector2(viewer.position.x, viewer.position.z);
        if (Vector2.Distance(latestViewerUpdate, pos) > viewerDistanceUpdate)
        {
            updateLOD();
            latestViewerUpdate = pos;
        }
    }

    #endregion


    /* ----------------------------------------------------------------------------------------- */
    /* -------------------------- METHODS ------------------------------------------------------ */
    /* ----------------------------------------------------------------------------------------- */

    #region METHODS

    public void initialize(Vector3 position, Vector3 rotation, float[] LODDistances)
    {
        this.LODDistances = LODDistances;
        transform.position = position;
        transform.Rotate(rotation);
        gameObject.SetActive(true);
        gameObject.name = objectName + " " + position;
        currentLOD = -1;
        updateLOD();
    }


    /* ----------------------------------------------------------------------------------------- */
    public void reset()
    {
        gameObject.name = objectName + " (available)";
        gameObject.transform.position = Vector3.zero;
        gameObject.transform.rotation = Quaternion.identity;
        gameObject.SetActive(false);
    }


    /* ----------------------------------------------------------------------------------------- */
    private void updateLOD()
    {
        Transform viewer = (Transform)GlobalInformation.Instance.getData(EndlessTerrainGenerator.VIEWER);
        float dist = Vector3.Distance(viewer.position, this.transform.position);

        for (int i = 0; i < LODDistances.Length; i++)
            if (dist < LODDistances[i])
            {
                //Debug.Log("LOD! " + i);
                int j = Mathf.Min(i, lods.Length - 1);
                if (j != currentLOD)
                {
                    if(currentLOD >= 0)
                        lods[currentLOD].SetActive(false);

                    lods[j].SetActive(true);
                    currentLOD = j;
                }
                break;
            }

        /* activate collider only if close to player */
        if(currentLOD == 0)
        {
            BoxCollider bc = lods[0].GetComponent<BoxCollider>();
            if (bc != null)
                bc.isTrigger = dist <= 2;
        }

    }

    #endregion
}
