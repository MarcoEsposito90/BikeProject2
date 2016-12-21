using UnityEngine;
using System.Collections;

public class CreateRoads : MonoBehaviour {

    public EndlessRoadsGenerator roadsGenerator;
    public static readonly string ROADS_DEBUG = "CreateRoads.Debug";

    void Awake()
    {
        GlobalInformation.Instance.addData(ROADS_DEBUG, false);
    }

    void Update () {

        if(Input.GetKeyDown(KeyCode.T))
        {
            GlobalInformation.Instance.addData(ROADS_DEBUG, true);

            Vector3 p = gameObject.transform.position;
            float cpArea = (float)GlobalInformation.Instance.getData(EndlessRoadsGenerator.CP_AREA);
            int scale = (int)GlobalInformation.Instance.getData(EndlessTerrainGenerator.SCALE);

            Debug.Log("position " + p + "; area = " + cpArea + ", scale = " + scale);

            float X = p.x / (float)scale;
            float Y = p.z / (float)scale;

            Vector2 gridPos = new Vector2(X / cpArea, Y / cpArea);
            Vector2 pos = new Vector2(X, Y);
            Debug.Log("new cp : " + gridPos);
            roadsGenerator.createControlPoint(gridPos, pos);
        }
	}
}
