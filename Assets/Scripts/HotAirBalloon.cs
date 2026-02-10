using UnityEngine;

public class HotAirBalloon : MonoBehaviour
{
    public string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        Debug.Log("Player entered balloon");

        // Parent XR Origin root
        other.transform.SetParent(transform, true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        Debug.Log("Player exited balloon");

        other.transform.SetParent(null, true);
    }
}
