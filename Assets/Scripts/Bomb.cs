using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class Bomb : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ExplosionPool explosionPool;
    [SerializeField] private Transform visual;
    [SerializeField] private Animator animator;

    [Header("Spark Effect")]
    [SerializeField] private GameObject sparkEffect; // <-- ADDED

    [Header("Stick Settings")]
    [SerializeField] private LayerMask stickMask = ~0;
    [SerializeField] private float surfaceOffset = 0.02f;

    [Header("Explosion Range")]
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private string enemyTag = "Enemy";

    public System.Action<GameObject> OnEnemyHit;

    private XRGrabInteractable grab;
    [SerializeField] private Rigidbody rb;
    private Collider[] cols;

    private bool armed;
    private bool stuck;
    private Transform poolParent;
    private Vector3 visualOriginalScale;
    private Coroutine pulseRoutine;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        cols = GetComponentsInChildren<Collider>();
        poolParent = transform.parent;

        if (visual == null)
        {
            Debug.LogError("Bomb: Visual reference NOT assigned.", this);
            enabled = false;
            return;
        }

        visualOriginalScale = visual.localScale;

        grab.selectEntered.AddListener(_ => OnGrabbed());
        grab.selectExited.AddListener(_ => OnReleased());
    }

    void OnEnable()
    {
        armed = false;
        stuck = false;

        if (grab) grab.enabled = true;

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        visual.localScale = visualOriginalScale;

        if (sparkEffect != null)
            sparkEffect.SetActive(false); // <-- ADDED

        if (poolParent != null)
            transform.SetParent(poolParent, true);
    }

    private void OnGrabbed()
    {
        armed = false;
        stuck = false;

        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);

        visual.localScale = visualOriginalScale;

        if (sparkEffect != null)
            sparkEffect.SetActive(false); // <-- ADDED
    }

    private void OnReleased()
    {
        armed = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!armed || stuck) return;
        if (((1 << collision.gameObject.layer) & stickMask) == 0) return;

        Stick(collision);
    }

    private void Stick(Collision collision)
    {
        stuck = true;
        armed = false;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;

        rb.constraints = RigidbodyConstraints.FreezeAll;
        rb.isKinematic = true;

        var contact = collision.GetContact(0);
        transform.position = contact.point + contact.normal * surfaceOffset;
        transform.rotation = Quaternion.identity;

        if (grab) grab.enabled = false;

        if (sparkEffect != null)
            sparkEffect.SetActive(true); // <-- ADDED (ONLY ENABLE HERE)

        pulseRoutine = StartCoroutine(PulseThenExplode());
    }

    private IEnumerator PulseThenExplode()
    {
        animator.Play("BombTick", 0, 0f);
        yield return null;

        float animationTime = animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(animationTime);

        Explode();
    }

    private void Explode()
    {
        if (explosionPool != null)
            explosionPool.Spawn(transform.position, Quaternion.identity);

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (var hit in hits)
        {
            // --------------------
            // ENEMY DAMAGE
            // --------------------
            if (hit.CompareTag(enemyTag))
            {
                BombEvents.BombHitEnemy(hit.gameObject, transform.position);
            }

            // --------------------
            // PLAYER DAMAGE (XR SAFE)
            // --------------------
            PlayerHealth playerHealth = hit.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                Debug.Log("💥 Player hit by explosion");
                playerHealth.TakeExplosionDamage(50f);
            }
        }

        gameObject.SetActive(false);
    }


    void OnDisable()
    {
        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);

        if (visual != null)
            visual.localScale = visualOriginalScale;

        if (sparkEffect != null)
            sparkEffect.SetActive(false); // <-- ADDED

        if (poolParent != null)
            transform.SetParent(poolParent, true);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
#endif
}
