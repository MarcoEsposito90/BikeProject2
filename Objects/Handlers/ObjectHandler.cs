using UnityEngine;
using System.Collections;

public class ObjectHandler : MonoBehaviour {

    public Material material;
    public Texture2D texture;

	void Start()
    {
        Material m = new Material(material);
        m.mainTexture = texture;
        this.GetComponent<MeshRenderer>().material = m;
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

        this.gameObject.SetActive(true);
        Debug.Log("position = " + this.transform.position);
    }
}
