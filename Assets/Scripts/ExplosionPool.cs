using UnityEngine;

public class ExplosionPool : MonoBehaviour
{
    [SerializeField] private GameObject[] explosionFx; // drag your 5 FX objects here (disabled in scene)
    [SerializeField] private float autoDisableAfter = 1.5f; // optional

    private int _index;

    public void Spawn(Vector3 position, Quaternion rotation)
    {
        if (explosionFx == null || explosionFx.Length == 0) return;

        // find inactive first
        for (int i = 0; i < explosionFx.Length; i++)
        {
            int idx = (_index + i) % explosionFx.Length;
            if (!explosionFx[idx].activeInHierarchy)
            {
                _index = (idx + 1) % explosionFx.Length;
                Activate(explosionFx[idx], position, rotation);
                return;
            }
        }

        // if all active, reuse next
        var fx = explosionFx[_index];
        _index = (_index + 1) % explosionFx.Length;
        Activate(fx, position, rotation);
    }

    private void Activate(GameObject fx, Vector3 pos, Quaternion rot)
    {
        fx.transform.SetPositionAndRotation(pos, rot);
        fx.SetActive(true);

        var auto = fx.GetComponent<PoolAutoDisable>();
        if (auto != null)
            auto.Begin(autoDisableAfter);
    }
}
