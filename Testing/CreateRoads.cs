using UnityEngine;
using System.Collections;

public class CreateRoads : MonoBehaviour {

    public EndlessRoadsGenerator roadsGenerator;

	void Update () {

        if(Input.GetKeyDown(KeyCode.T))
        {
            Vector3 p = gameObject.transform.position;
            roadsGenerator.splitRequest(new Vector2(p.x, p.z));
        }
	}
}
