using UnityEngine;
using System.Collections;

public class VillageHandler : MonoBehaviour {

    public Transform link;

    

    void OnEnable () {

        Debug.Log("Village " + gameObject.name + " OnEnable");

        float cpArea = (float)GlobalInformation.Instance.getData(EndlessRoadsGenerator.CP_AREA);
        int scale = (int)GlobalInformation.Instance.getData(EndlessTerrainGenerator.SCALE);

        Vector3 p = link.position;
        float X = p.x / (float)scale;
        float Y = p.z / (float)scale;

        Vector2 gridPos = new Vector2(X / cpArea, Y / cpArea);
        Vector2 pos = new Vector2(X, Y);
        //EndlessRoadsGenerator.Instance.createControlPoint(gridPos, pos);


        //Transform objectsContainer = transform.Find("Objects");
        //foreach(Transform o in objectsContainer)
        //{
        //    Debug.Log("found object: " + o.gameObject.name + "in position " + o.position);
        //    float h = GlobalInformation.Instance.getHeight(new Vector2(o.position.x, o.position.z));
        //    h -= this.transform.position.y;
        //    o.position = new Vector3(o.position.x, h, o.position.z);
        //}

        //Transform streetsContainer = transform.Find("Streets");
        //foreach(Transform s in streetsContainer)
        //{
        //    Debug.Log("Found street: " + s.gameObject.name);
        //    foreach (Transform point in s)
        //        Debug.Log("     found control point " + point);
        //}
    }
}
