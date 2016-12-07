using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class BillboardHandler : MonoBehaviour {

	void Update () {

        Camera c = Camera.main;
        transform.LookAt(
            transform.position + c.transform.rotation * Vector3.forward,
            c.transform.rotation * Vector3.up);
    }
}
