using UnityEngine;
using System.Collections;

public class VideoCapture : MonoBehaviour
{

    private bool record;
    private int currentFrame;

    void Start()
    {
        record = false;
        currentFrame = 0;
        //StartCoroutine(videoCaptureCoroutine());
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            record = !record;

        if (record)
        {
            Application.CaptureScreenshot("C:\\Users\\Unieuro\\Documents\\Poli\\Tesi\\BikeProject2\\BikeProject2Demo\\frames\\frame" + currentFrame.ToString("D4") + ".png", 2);
            currentFrame++;
        }
    }


    IEnumerator videoCaptureCoroutine()
    {
        while (true)
        {

            

            yield return new WaitForSeconds(1.0f / 30.0f);
        }
    }
}
