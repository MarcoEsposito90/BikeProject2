using UnityEngine;
using System.Collections;

public class ObjectHandler : MonoBehaviour {

    public string objectName;

	void Start()
    {
    }
	
	void Update () {

        Transform v = (Transform)GlobalInformation.Instance.getData(EndlessTerrainGenerator.VIEWER);
        //if(Vector3.Distance(v.position, this.transform.position) > 10)
        
	}


    public void initialize(Vector3 position, Vector3 rotation)
    {
        //float randomX = Mathf.PerlinNoise((gridPos.x + seedX) * 200, (gridPos.y + seedX) * 200) * randomness;
        //float randomY = Mathf.PerlinNoise((gridPos.x + seedY) * 200, (gridPos.y + seedY) * 200) * randomness;
        ////Debug.Log(gridPos + " - random = " + randomX + ";" + randomY);
        //float X = (gridPos.x + randomX) * areaSize;
        //float Y = (gridPos.y + randomY) * areaSize;
        //float height = GlobalInformation.Instance.getHeight(new Vector2(X, Y));
        //this.transform.position = new Vector3(X * scale, height * scale, Y * scale);
        transform.position = position;

        //System.Random r = new System.Random();
        //float y = (1.0f / r.Next(100)) * 100 * 360;
        //Vector3 rot = new Vector3(0, y, 0);
        //this.transform.Rotate(rot);
        transform.Rotate(rotation);

        this.gameObject.SetActive(true);
        this.gameObject.name = objectName + " " + position;
    }


    public void reset()
    {
        this.gameObject.name = objectName + " (available)";
        this.gameObject.transform.position = Vector3.zero;
        this.gameObject.transform.rotation = Quaternion.identity;
        this.gameObject.SetActive(false);
    }
}
