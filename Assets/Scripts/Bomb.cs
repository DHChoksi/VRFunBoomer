using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class Bomb : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private ExplosionPool explosionPool;

    [Header("Animation")]
    [SerializeField] private string tickStateName = "BombTick"; // exact state name in Animator

    [Header("Stick Settings")]
    [SerializeField] private LayerMask stickMask = ~0;          // floor + enemies layers
    [SerializeField] private bool parentToHitObject = true;     // stick to moving enemy
    [SerializeField] private bool alignToSurfaceNormal = false; // optional
    [SerializeField] private float surfaceOffset = 0.02f;       // small offset so it doesn’t clip

    private XRGrabInteractable grab;
    private Rigidbody rb;
    private Collider[] cols;

    private bool armed;   // becomes true after release
    private bool stuck;   // prevents multiple hits
    private Transform poolParent;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        cols = GetComponentsInChildren<Collider>();

        poolParent = transform.parent;

        grab.selectEntered.AddListener(_ => OnGrabbed());
        grab.selectExited.AddListener(_ => OnReleased());
    }

    void OnEnable()
    {
        // reset for pooling
        armed = false;
        stuck = false;

        if (grab) grab.enabled = true;

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        foreach (var c in cols)
            c.enabled = true;

        if (animator)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        // return to pool parent (in case it was parented to an enemy)
        if (poolParent != null)
            transform.SetParent(poolParent, true);
    }

    private void OnGrabbed()
    {
        armed = false;
        stuck = false;
    }

    private void OnReleased()
    {
        // bomb can only stick AFTER release
        armed = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!armed || stuck) return;

        // only stick to selected layers
        if (((1 << collision.gameObject.layer) & stickMask) == 0) return;

        Stick(collision);
    }

    private void Stick(Collision collision)
    {
        stuck = true;
        armed = false;

        // freeze physics immediately
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.isKinematic = true;

        // move bomb to the hit point
        var contact = collision.GetContact(0);
        Vector3 pos = contact.point + contact.normal * surfaceOffset;
        transform.position = pos;

        if (alignToSurfaceNormal)
            transform.rotation = Quaternion.LookRotation(-contact.normal);

        // optionally stick to enemy transform so it moves with them
        if (parentToHitObject)
            transform.SetParent(collision.transform, true);

        // prevent re-grab while ticking
        if (grab) grab.enabled = false;

        // play ticking animation
        if (animator)
            animator.Play(tickStateName, 0, 0f);
    }

    // ✅ CALL THIS FROM AN ANIMATION EVENT at the END of BombTick clip
    public void AnimationEvent_Explode()
    {
        if (explosionPool != null)
            explosionPool.Spawn(transform.position, Quaternion.identity);

        // return bomb to pool (disable)
        gameObject.SetActive(false);
    }

    void OnDisable()
    {
        // detach from enemy when pooled
        if (poolParent != null)
            transform.SetParent(poolParent, true);
    }
}
