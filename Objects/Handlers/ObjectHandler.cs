using UnityEngine;
using System.Collections;

public class ObjectHandler : MonoBehaviour {

    public string name;

	void Start()
    {
    }
	
	void Update () {
	
	}


    public void computePosition(Vector2 gridPos, float seedX, float seedY, float areaSize, int scale)
    {
        float randomX = Mathf.PerlinNoise(gridPos.x + seedX, gridPos.y + seedX);
        float randomY = Mathf.PerlinNoise(gridPos.x + seedY, gridPos.y + seedY);
        float X = (gridPos.x + randomX) * areaSize;
        float Y = (gridPos.y + randomY) * areaSize;
        float height = GlobalInformation.Instance.getHeight(new Vector2(X, Y));
        this.transform.position = new Vector3(X * scale, height * scale, Y * scale);

        System.Random r = new System.Random();
        float y = (1.0f / r.Next(100)) * 100 * 360;
        Vector3 rot = new Vector3(0, y, 0);
        this.transform.Rotate(rot);

        this.gameObject.SetActive(true);
        this.gameObject.name = name + " " + gridPos;
    }


    public void reset()
    {
        this.gameObject.name = name + " (available)";
        this.gameObject.transform.position = Vector3.zero;
        this.gameObject.transform.rotation = Quaternion.identity;
        this.gameObject.SetActive(false);
    }
}
