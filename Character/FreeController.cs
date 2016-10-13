using UnityEngine;
using System.Collections;

public class FreeController : MonoBehaviour {

    Transform tr;

    [Range(1.0f, 100.0f)]
    public float speed;

    // Use this for initialization
    void Start()
    {

        print("Start function invoked");
        this.tr = this.GetComponent<Transform>();

        if (tr != null)
            print("transform component correctly found");
    }

    // Update is called once per frame
    void Update()
    {

        // translation ----------------------------------
        float localZ = Input.GetAxis("Vertical") * speed;
        float localX = Input.GetAxis("Horizontal") * speed;
        float localY = Input.GetAxis("Jump") * speed;
        Vector3 v = new Vector3(localX, localY, localZ);
        tr.Translate(v);

        // rotation -------------------------------------
        float rotY = Input.GetAxis("Mouse X");
        float rotZ = -Input.GetAxis("Mouse Y");
        tr.Rotate(0, rotY, rotZ, Space.World);
        tr.rotation = Quaternion.Euler(tr.rotation.eulerAngles.x, tr.rotation.eulerAngles.y, 0);
    }
}
