using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

[RequireComponent(typeof(CharacterController))]
public class HeadBobMoveProvider : LocomotionProvider
{
    [Header("References")]
    [SerializeField] XROrigin xrOrigin;              // If null, will auto-find
    [SerializeField] Camera playerCamera;            // Only used for forward direction
    [SerializeField] CharacterController controller; // Required (auto-fills)

    [Header("Input")]
    [Tooltip("Vector2 move input (e.g., left joystick).")]
    [SerializeField] InputActionProperty moveAction;

    [Header("Auto-Binding (optional)")]
    [SerializeField] bool autoBindLeftThumbstick = true;

    InputAction runtimeMoveAction;
    bool UsingRuntimeAction => runtimeMoveAction != null;

    [Header("Movement")]
    [Tooltip("Meters per second walking speed.")]
    [SerializeField, Min(0f)] float moveSpeed = 2.0f;

    [Tooltip("If true, ignore strafing and always move where the head is looking.")]
    [SerializeField] bool forwardOnly = true;

    [Tooltip("Apply gravity while moving.")]
    [SerializeField] bool useGravity = true;
    [SerializeField] float gravity = -9.81f;

    [Header("Head Bobbing (XR Origin offset)")]
    [Tooltip("Vertical bob amplitude in meters, applied to XR Origin's CameraFloorOffset Y.")]
    [SerializeField, Min(0f)] float bobAmplitude = 0.03f;

    [Tooltip("Minimum horizontal speed to start bobbing.")]
    [SerializeField, Min(0f)] float bobStartSpeed = 0.05f;

    [Tooltip("How quickly the bob returns to neutral when stopping.")]
    [SerializeField, Range(1f, 20f)] float bobDamp = 8f;

    [Tooltip("Phase shift (0..1) so step hits align with bob trough/peak.")]
    [SerializeField, Range(-1f, 1f)] float bobPhaseOffset = -0.25f;

    [Header("Footsteps")]
    [SerializeField] AudioSource footstepSource; // One-shot AudioSource
    [SerializeField] AudioClip[] footstepClips;

    [Tooltip("Footstep cadence at full speed (steps/sec, each foot).")]
    [SerializeField, Min(0.1f)] float stepFrequency = 1.8f;

    // Internal
    float verticalVelocity;
    float stepPhase;
    float lastStepPhase;

    Transform floorOffset;
    float offsetLocalStartY;
    bool haveFloorOffset;

    protected void Reset()
    {
        controller = GetComponent<CharacterController>();
        xrOrigin = GetComponentInParent<XROrigin>();
    }

    protected void Awake()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        if (xrOrigin == null)
            xrOrigin = GetComponentInParent<XROrigin>();

        if (xrOrigin != null && xrOrigin.Camera != null)
            playerCamera = xrOrigin.Camera;

        var floorObj = xrOrigin != null ? xrOrigin.CameraFloorOffsetObject : null;
        floorOffset = floorObj ? floorObj.transform : null;

        if (floorOffset != null)
        {
            offsetLocalStartY = floorOffset.localPosition.y;
            haveFloorOffset = true;
        }

        EnsureMoveActionBound();
    }

    protected void OnEnable()
    {
        if (UsingRuntimeAction)
            runtimeMoveAction.Enable();
        else
            moveAction.action?.Enable();
    }

    protected void OnDisable()
    {
        if (UsingRuntimeAction)
            runtimeMoveAction.Disable();
        else
            moveAction.action?.Disable();

        RestoreYOffsetImmediate();
    }

    void OnDestroy()
    {
        if (UsingRuntimeAction)
            runtimeMoveAction.Dispose();
    }

    void Update()
    {
        if (xrOrigin == null || controller == null)
            return;

        if (playerCamera == null && xrOrigin != null)
            playerCamera = xrOrigin.Camera;

        if (!haveFloorOffset && xrOrigin?.CameraFloorOffsetObject != null)
        {
            floorOffset = xrOrigin.CameraFloorOffsetObject.transform;
            offsetLocalStartY = floorOffset.localPosition.y;
            haveFloorOffset = true;
        }

        Vector2 input = moveAction.action?.ReadValue<Vector2>() ?? Vector2.zero;

        Vector3 forwardRef = playerCamera ? playerCamera.transform.forward : transform.forward;
        Vector3 fwd = Vector3.ProjectOnPlane(forwardRef, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, fwd);

        Vector3 moveDir;
        float inputMag;

        if (forwardOnly)
        {
            inputMag = input.magnitude;
            moveDir = fwd;
        }
        else
        {
            inputMag = Mathf.Clamp01(input.magnitude);
            moveDir = (fwd * input.y + right * input.x).normalized;
        }

        bool wantsToMove = inputMag > 0.01f;

        if (wantsToMove &&
            system != null &&
            system.RequestExclusiveOperation(this) == RequestResult.Success)
        {
            ApplyMovement(moveDir, inputMag);
            system.FinishExclusiveOperation(this);
        }
        else
        {
            DampenYOffsetToNeutral();
        }
    }

    void EnsureMoveActionBound()
    {
        if (!autoBindLeftThumbstick)
            return;

        var assigned = moveAction.action;
        if (assigned != null && assigned.bindings.Count > 0)
            return;

        runtimeMoveAction = new InputAction(
            "Runtime Move",
            InputActionType.Value,
            "<XRController>{LeftHand}/primary2DAxis"
        );

        runtimeMoveAction.AddBinding("<XRController>{LeftHand}/thumbstick");
        runtimeMoveAction.AddBinding("<OculusTouchController>{LeftHand}/thumbstick");
        runtimeMoveAction.AddBinding("<Gamepad>/leftStick");

        runtimeMoveAction.Enable();
        moveAction = new InputActionProperty(runtimeMoveAction);
    }

    void ApplyMovement(Vector3 moveDir, float inputMag)
    {
        Vector3 horiz = moveDir * (moveSpeed * inputMag);

        if (useGravity)
        {
            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -0.5f;
            else
                verticalVelocity += gravity * Time.deltaTime;
        }
        else
        {
            verticalVelocity = 0f;
        }

        Vector3 motion =
            horiz * Time.deltaTime +
            Vector3.up * verticalVelocity * Time.deltaTime;

        controller.Move(motion);

        UpdateHeadBobAndSteps(horiz.magnitude);
    }

    void UpdateHeadBobAndSteps(float horizSpeed)
    {
        float speed01 = Mathf.Clamp01(horizSpeed / Mathf.Max(0.01f, moveSpeed));
        float stepAdv = speed01 * stepFrequency * Time.deltaTime;

        float prev = stepPhase;
        stepPhase = (stepPhase + stepAdv) % 1f;

        if (CrossedPhase(prev, stepPhase, 0.0f)) PlayFootstep();
        if (CrossedPhase(prev, stepPhase, 0.5f)) PlayFootstep();

        lastStepPhase = stepPhase;

        if (!haveFloorOffset)
            return;

        if (horizSpeed > bobStartSpeed)
        {
            float phase = Wrap01(stepPhase + bobPhaseOffset);
            float bobOffset = Mathf.Sin(phase * Mathf.PI * 2f) * bobAmplitude;

            float targetY = offsetLocalStartY + bobOffset;
            var local = floorOffset.localPosition;
            local.y = Mathf.Lerp(local.y, targetY, 1f - Mathf.Exp(-bobDamp * Time.deltaTime));
            floorOffset.localPosition = local;
        }
        else
        {
            DampenYOffsetToNeutral();
        }
    }

    static bool CrossedPhase(float prev, float curr, float target)
    {
        if (curr < prev) curr += 1f;
        if (target < prev) target += 1f;
        return prev < target && curr >= target;
    }

    static float Wrap01(float x)
    {
        x %= 1f;
        if (x < 0f) x += 1f;
        return x;
    }

    void PlayFootstep()
    {
        if (footstepSource && footstepClips != null && footstepClips.Length > 0)
        {
            if (!footstepSource.isPlaying)
            {
                var clip = footstepClips[Random.Range(0, footstepClips.Length)];
                footstepSource.PlayOneShot(clip);
            }
        }
    }

    void DampenYOffsetToNeutral()
    {
        if (!haveFloorOffset)
            return;

        var local = floorOffset.localPosition;
        local.y = Mathf.Lerp(local.y, offsetLocalStartY, 1f - Mathf.Exp(-bobDamp * Time.deltaTime));
        floorOffset.localPosition = local;
    }

    void RestoreYOffsetImmediate()
    {
        if (!haveFloorOffset)
            return;

        var local = floorOffset.localPosition;
        local.y = offsetLocalStartY;
        floorOffset.localPosition = local;
    }
}
