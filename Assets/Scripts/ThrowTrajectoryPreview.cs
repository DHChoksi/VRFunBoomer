using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRGrabInteractable))]
public class ThrowTrajectoryPreview : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public int resolution = 30;
    public float timeStep = 0.05f;
    public float velocityMultiplier = 1.0f;

    private XRGrabInteractable grabInteractable;
    private XRBaseInteractor currentInteractor;
    private Rigidbody rb;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);

        lineRenderer.enabled = false;
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        currentInteractor = args.interactorObject as XRBaseInteractor;
        lineRenderer.enabled = true;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        lineRenderer.enabled = false;
        currentInteractor = null;
    }

    void Update()
    {
        if (currentInteractor == null) return;

        Vector3 startPos = transform.position;
        Vector3 velocity = GetInteractorVelocity(currentInteractor);

        velocity = Vector3.ClampMagnitude(velocity, 10f);

        DrawTrajectory(startPos, velocity);
    }

    Vector3 GetInteractorVelocity(XRBaseInteractor interactor)
    {
        Rigidbody interactorRb = interactor.GetComponent<Rigidbody>();
        return interactorRb ? interactorRb.velocity : Vector3.zero;
    }


    void DrawTrajectory(Vector3 startPos, Vector3 startVelocity)
    {
        lineRenderer.positionCount = resolution;

        Vector3 gravity = Physics.gravity;
        float time = 0f;

        for (int i = 0; i < resolution; i++)
        {
            Vector3 point =
                startPos +
                startVelocity * time +
                0.5f * gravity * time * time;

            lineRenderer.SetPosition(i, point);
            time += timeStep;
        }
    }
}
