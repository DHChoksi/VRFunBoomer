using System.Collections;
using UnityEngine;

public class PoolAutoDisable : MonoBehaviour
{
    private Coroutine _co;

    public void Begin(float seconds)
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(DisableAfter(seconds));
    }

    private IEnumerator DisableAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        gameObject.SetActive(false);
        _co = null;
    }

    private void OnDisable()
    {
        if (_co != null) { StopCoroutine(_co); _co = null; }
    }
}
