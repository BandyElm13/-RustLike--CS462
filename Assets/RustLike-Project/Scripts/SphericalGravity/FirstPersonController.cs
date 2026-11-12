using UnityEngine;

// A first-person controller that works with spherical gravity. It walks around
// the surface of the sphere, with "down" always pointing at the sphere's center.
//
// Setup on the Player object:
//   - Rigidbody         (added automatically via GravityBody's requirement)
//   - a Collider        (Capsule recommended)
//   - GravityBody       (this script forces its "Align To Surface" OFF automatically)
//   - this script
//   - a child Camera positioned at head height
//   - (optional) an empty child under the camera, assigned to "Weapon Socket"
//
// Input note: this uses the legacy Input Manager (Input.GetAxis / GetKey). If your
// project's Active Input Handling is "Input System Package (New)" only, switch it to
// "Both" (Project Settings > Player > Active Input Handling).
[RequireComponent(typeof(GravityBody))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Walking speed.")]
    public float walkSpeed = 6f;
    [Tooltip("Speed while holding Sprint (hold Left or Right Shift).")]
    public float sprintSpeed = 10f;

    [Header("Jump")]
    [Tooltip("Jump height in world units. The launch speed is derived from the sphere's gravity, so height stays consistent if you retune gravity.")]
    public float jumpHeight = 2f;
    [Tooltip("How far 'down' (toward the sphere) to check for ground. Roughly half the capsule height plus a little.")]
    public float groundCheckDistance = 1.1f;
    [Tooltip("Which layers count as ground. Leave as Everything, or restrict it to your level geometry.")]
    public LayerMask groundMask = ~0;

    [Header("Slopes")]
    [Tooltip("Steepest hill (degrees from the planet's up) you can walk up. Steeper surfaces make you slide off.")]
    public float maxSlopeAngle = 50f;
    [Tooltip("Small push into the ground while walking, so you follow hills down instead of launching off them.")]
    public float groundStick = 2f;

    [Header("Look")]
    [Tooltip("Mouse look sensitivity.")]
    public float lookSensitivity = 2f;
    [Tooltip("How far up/down you can look, in degrees.")]
    public float maxPitch = 85f;

    [Header("Strafe camera tilt")]
    [Tooltip("Degrees the camera rolls when strafing left/right.")]
    public float strafeTiltAngle = 2.5f;
    [Tooltip("How quickly the tilt eases in and out.")]
    public float tiltSpeed = 8f;

    [Header("Sprint FOV")]
    [Tooltip("Extra field of view added while sprinting forward.")]
    public float sprintFovBoost = 10f;
    [Tooltip("How quickly the FOV eases toward its target.")]
    public float fovLerpSpeed = 8f;

    [Header("Aim Down Sights (set by PlayerShooting)")]
    [Tooltip("FOV while aiming = base FOV x this.")]
    public float aimFovMultiplier = 0.7f;
    [Tooltip("Mouse sensitivity while aiming = normal x this.")]
    public float aimSensitivityMultiplier = 0.6f;

    [Header("Weapon hook (for a later gun component)")]
    [Tooltip("Empty child under the camera where a gun model/component will attach. Optional for now.")]
    public Transform weaponSocket;

    // ---- Public state a future gun script can read ----
    public Camera Camera => cam;
    public Transform CameraTransform => cameraTransform;
    public bool IsSprinting { get; private set; }
    public bool IsMoving { get; private set; }
    public bool IsGrounded { get; private set; }
    // Set by the gun while right mouse is held: zooms, slows look, and blocks sprinting.
    public bool IsAiming { get; set; }
    // A ray straight out of the camera - use this for aiming/shooting later.
    public Ray AimRay => new Ray(cameraTransform.position, cameraTransform.forward);

    private GravityBody gravityBody;
    private Rigidbody rb;
    private Camera cam;
    private Transform cameraTransform;
    private CapsuleCollider bodyCollider;
    private Vector3 groundNormal = Vector3.up;

    private Vector2 moveInput;   // x = strafe, y = forward
    private float yawDelta;      // accumulated mouse-X, applied to the body in FixedUpdate
    private float pitch;         // accumulated camera pitch
    private float currentRoll;   // smoothed strafe tilt
    private float baseFov;
    private bool jumpRequested;  // set on jump press, consumed in FixedUpdate

    private void Awake()
    {
        gravityBody = GetComponent<GravityBody>();
        gravityBody.alignToSurface = false; // we handle rotation (align + yaw) ourselves

        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate; // smooth out physics-rate motion

        cam = GetComponentInChildren<Camera>();
        if (cam != null) cameraTransform = cam.transform;

        bodyCollider = GetComponent<CapsuleCollider>();

        // We set velocity directly every physics step, so friction only makes the player
        // stick to walls and slopes. Use a frictionless material unless one is assigned.
        Collider col = GetComponent<Collider>();
        if (col != null && col.sharedMaterial == null)
        {
            col.sharedMaterial = new PhysicsMaterial("PlayerNoFriction")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounciness = 0f,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
        }
    }

    private void Start()
    {
        if (cam != null) baseFov = cam.fieldOfView;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // --- Gather input ---
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        IsSprinting = !IsAiming && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        IsMoving = moveInput.sqrMagnitude > 0.01f;

        if (Input.GetKeyDown(KeyCode.Space)) jumpRequested = true;

        float sensitivity = lookSensitivity * (IsAiming ? aimSensitivityMultiplier : 1f);
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        // Yaw turns the whole body; applied in FixedUpdate (physics rotation), just banked here.
        yawDelta += mouseX;

        if (cameraTransform != null)
        {
            // Pitch (look up/down), clamped.
            pitch = Mathf.Clamp(pitch - mouseY, -maxPitch, maxPitch);

            // Strafe tilt: roll the camera slightly, opposite the strafe direction.
            float targetRoll = -moveInput.x * strafeTiltAngle;
            currentRoll = Mathf.Lerp(currentRoll, targetRoll, tiltSpeed * Time.deltaTime);

            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, currentRoll);
        }

        // Sprint FOV only when actually sprinting forward; aiming zooms in.
        if (cam != null)
        {
            bool sprintingForward = IsSprinting && moveInput.y > 0.1f;
            float targetFov = IsAiming
                ? baseFov * aimFovMultiplier
                : baseFov + (sprintingForward ? sprintFovBoost : 0f);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, fovLerpSpeed * Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        Vector3 up = gravityBody.UpDirection;

        IsGrounded = CheckGround(up);

        // --- Rotation: keep feet on the surface, then apply the player's yaw ---
        Quaternion align = Quaternion.FromToRotation(transform.up, up) * rb.rotation;
        Quaternion yaw = Quaternion.AngleAxis(yawDelta, up);
        rb.MoveRotation(yaw * align);
        yawDelta = 0f;

        // --- Movement: direct control in the tangent plane, keeping gravity's pull ---
        float speed = IsSprinting ? sprintSpeed : walkSpeed;
        Vector3 wish = transform.forward * moveInput.y + transform.right * moveInput.x;
        wish = Vector3.ClampMagnitude(wish, 1f) * speed;

        float fallSpeed = Vector3.Dot(rb.linearVelocity, up); // preserve the vertical (gravity/fall) component

        bool jump = jumpRequested && IsGrounded;
        jumpRequested = false;

        if (jump)
        {
            // Launch along "up" with the speed needed to reach jumpHeight under current gravity.
            fallSpeed = Mathf.Sqrt(2f * gravityBody.CurrentGravity * jumpHeight);
            rb.linearVelocity = wish + up * fallSpeed;
        }
        else if (IsGrounded && fallSpeed <= 0.1f)
        {
            // Walk along the ground's surface: up hills instead of into them,
            // and down hills instead of flying off the top.
            Vector3 along = Vector3.ProjectOnPlane(wish, groundNormal);
            if (along.sqrMagnitude > 0.0001f) along = along.normalized * wish.magnitude;
            rb.linearVelocity = along - groundNormal * groundStick;
        }
        else
        {
            // Airborne (or rising from a jump): keep gravity's vertical component.
            rb.linearVelocity = wish + up * fallSpeed;
        }
    }

    // Sphere-cast "down" (toward the planet) from the body center. A sphere catches the
    // ground on slopes and edges where a single thin ray would miss. Casts ignore the
    // player's own collider because they start inside it.
    private bool CheckGround(Vector3 up)
    {
        groundNormal = up;

        float scale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
        float radius = bodyCollider != null ? bodyCollider.radius * scale * 0.9f : 0.45f;
        Vector3 origin = bodyCollider != null ? transform.TransformPoint(bodyCollider.center) : transform.position;
        float distance = Mathf.Max(0.01f, groundCheckDistance - radius);

        if (!Physics.SphereCast(origin, radius, -up, out RaycastHit hit, distance, groundMask, QueryTriggerInteraction.Ignore))
            return false;

        // Too steep to stand on: treat as a wall and let gravity slide us off.
        if (Vector3.Angle(hit.normal, up) > maxSlopeAngle)
            return false;

        groundNormal = hit.normal;
        return true;
    }
}
