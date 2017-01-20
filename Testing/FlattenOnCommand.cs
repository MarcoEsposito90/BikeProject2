using UnityEngine;
using System.Collections;

public class FlattenOnCommand : MonoBehaviour {

    [Range(0.0f, 500.0f)]
    public float radius;

	void Update () {

        if (Input.GetKeyDown(KeyCode.T))
        {
            Vector2 worldPos = new Vector2(gameObject.transform.position.x, gameObject.transform.position.z);
            NoiseGenerator.Instance.redrawRequest(worldPos, radius);
        }
	}
}
