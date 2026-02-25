using Fusion;
using UnityEngine;

/// <summary>
/// 1v1 High Noon style network controller tuned for Fusion.
/// - Input authority sends movement/gyro intent.
/// - State authority simulates and replicates movement.
/// - Local player can feel stationary while remote sees replicated movement.
/// </summary>
public class HighNoonDuelController : NetworkBehaviour
{
    [Header("Networked State")]
    [Networked] private Vector3 NetPosition { get; set; }
    [Networked] private float NetYaw { get; set; }

    [Header("Scene References")]
    [SerializeField] private Transform networkBodyRoot;
    [SerializeField] private Transform localVisualRoot;
    [SerializeField] private Camera playerCamera;

    [Header("Duel Setup")]
    [SerializeField] private float hostYaw = 90f;
    [SerializeField] private float clientYaw = -90f;
    [SerializeField] private bool keepLocalPlayerVisuallyStationary = true;
    [SerializeField] private float arenaHalfWidth = 18f;
    [SerializeField] private float arenaHalfDepth = 8f;

    [Header("Motion")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float lookSpeed = 120f;

    [Header("Vision Restriction")]
    [Tooltip("How far the player can look away from duel facing direction.")]
    [SerializeField] private float maxYawOffsetFromFacing = 30f;

    [Header("Gyro")]
    [SerializeField] private bool allowGyro = true;
    [SerializeField] private float gyroYawMultiplier = 0.08f;
    [SerializeField] private float gyroSmoothing = 10f;

    private Quaternion _gyroSmoothedRotation = Quaternion.identity;
    private bool _gyroInitialized;
    private Vector3 _localAnchorPosition;

    public override void Spawned()
    {
        float facingYaw = Object.HasStateAuthority ? hostYaw : clientYaw;
        NetYaw = facingYaw;
        NetPosition = transform.position;

        transform.SetPositionAndRotation(NetPosition, Quaternion.Euler(0f, NetYaw, 0f));

        if (HasInputAuthority)
        {
            _localAnchorPosition = transform.position;

            if (allowGyro && SystemInfo.supportsGyroscope)
            {
                Input.gyro.enabled = true;
                _gyroInitialized = true;
                _gyroSmoothedRotation = Input.gyro.attitude;
            }

            ApplyVisionRestriction(NetYaw);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out HighNoonNetworkInput input))
        {
            Simulate(input);
        }

        transform.SetPositionAndRotation(NetPosition, Quaternion.Euler(0f, NetYaw, 0f));
        UpdatePresentation();
    }

    private void Simulate(HighNoonNetworkInput input)
    {
        float dt = Runner.DeltaTime;

        float desiredYaw = NetYaw + input.LookDelta * lookSpeed * dt;
        float duelFacingYaw = Object.HasStateAuthority ? hostYaw : clientYaw;
        NetYaw = ClampYaw(desiredYaw, duelFacingYaw, maxYawOffsetFromFacing);

        Vector3 forward = Quaternion.Euler(0f, NetYaw, 0f) * Vector3.forward;
        Vector3 right = Quaternion.Euler(0f, NetYaw, 0f) * Vector3.right;

        Vector3 moveWorld = (forward * input.Move.y + right * input.Move.x) * (moveSpeed * dt);
        Vector3 nextPosition = NetPosition + moveWorld;

        nextPosition.x = Mathf.Clamp(nextPosition.x, -arenaHalfWidth, arenaHalfWidth);
        nextPosition.z = Mathf.Clamp(nextPosition.z, -arenaHalfDepth, arenaHalfDepth);

        NetPosition = nextPosition;
    }

    private void UpdatePresentation()
    {
        if (localVisualRoot == null)
        {
            return;
        }

        bool isLocal = HasInputAuthority;

        if (isLocal && keepLocalPlayerVisuallyStationary)
        {
            localVisualRoot.position = _localAnchorPosition;
            localVisualRoot.rotation = Quaternion.Euler(0f, NetYaw, 0f);
        }
        else
        {
            localVisualRoot.position = NetPosition;
            localVisualRoot.rotation = Quaternion.Euler(0f, NetYaw, 0f);
        }

        if (isLocal)
        {
            ApplyVisionRestriction(NetYaw);
        }
    }

    private void ApplyVisionRestriction(float yaw)
    {
        if (playerCamera == null)
        {
            return;
        }

        // Narrow FOV gives the “small area in front” feel (not full 360 awareness).
        playerCamera.fieldOfView = 42f;
        playerCamera.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    public HighNoonNetworkInput BuildLocalInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        float lookDelta = Input.GetAxis("Mouse X");
        bool usingGyro = false;

        if (allowGyro && _gyroInitialized)
        {
            Quaternion current = Input.gyro.attitude;
            _gyroSmoothedRotation = Quaternion.Slerp(_gyroSmoothedRotation, current, Time.deltaTime * gyroSmoothing);

            float gyroYaw = _gyroSmoothedRotation.eulerAngles.y;
            float normalizedYaw = Mathf.DeltaAngle(0f, gyroYaw);
            lookDelta += normalizedYaw * gyroYawMultiplier;
            usingGyro = true;
        }

        return new HighNoonNetworkInput
        {
            Move = new Vector2(horizontal, vertical),
            LookDelta = lookDelta,
            Fire = Input.GetMouseButton(0),
            UseGyro = usingGyro
        };
    }

    private static float ClampYaw(float yaw, float centerYaw, float maxOffset)
    {
        float delta = Mathf.DeltaAngle(centerYaw, yaw);
        delta = Mathf.Clamp(delta, -maxOffset, maxOffset);
        return centerYaw + delta;
    }
}
