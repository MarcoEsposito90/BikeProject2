using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class SpeedometerUpdate : MonoBehaviour
{
    public Transform player;
    private Vector3 oldPosition;

    [Range(0.1f, 1.0f)]
    public float intervalInSeconds;

    void Start()
    {
        oldPosition = Vector3.zero;
        StartCoroutine(updateCoroutine());
    }

    IEnumerator updateCoroutine()
    {
        while (true)
        {
            if (player.position.Equals(Vector3.zero))
                yield return new WaitForSeconds(intervalInSeconds);

            Vector3 currentPosition = player.position;
            float dist = Vector3.Distance(oldPosition, currentPosition);
            int velocity = (int)(dist / (intervalInSeconds * 3.6f) * 8);
            GetComponent<Text>().text = velocity.ToString("D3");
            oldPosition = currentPosition;
            yield return new WaitForSeconds(intervalInSeconds);
        }
    }
}
