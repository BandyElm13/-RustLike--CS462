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

    [Header("Weapon hook (for a later gun component)")]
    [Tooltip("Empty child under the camera where a gun model/component will attach. Optional for now.")]
    public Transform weaponSocket;

    // ---- Public state a future gun script can read ----
    public Camera Camera => cam;
    public Transform CameraTransform => cameraTransform;
    public bool IsSprinting { get; private set; }
    public bool IsMoving { get; private set; }
    public bool IsGrounded { get; private set; }
    // A ray straight out of the camera - use this for aiming/shooting later.
    public Ray AimRay => new Ray(cameraTransform.position, cameraTransform.forward);

    private GravityBody gravityBody;
    private Rigidbody rb;
    private Camera cam;
    private Transform cameraTransform;

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
        IsSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        IsMoving = moveInput.sqrMagnitude > 0.01f;

        if (Input.GetKeyDown(KeyCode.Space)) jumpRequested = true;

        float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

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

        // Sprint FOV: only when actually sprinting forward.
        if (cam != null)
        {
            bool sprintingForward = IsSprinting && moveInput.y > 0.1f;
            float targetFov = baseFov + (sprintingForward ? sprintFovBoost : 0f);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, fovLerpSpeed * Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        Vector3 up = gravityBody.UpDirection;

        // Ground check: cast "down" (toward the sphere) from the body center.
        // Starting inside the capsule means the ray ignores the player's own collider.
        IsGrounded = Physics.Raycast(transform.position, -up, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);

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

        // Jump: launch along "up" with the speed needed to reach jumpHeight under current gravity.
        if (jumpRequested && IsGrounded)
        {
            float gravity = gravityBody.ActiveAttractor != null ? gravityBody.ActiveAttractor.gravity : 9.81f;
            fallSpeed = Mathf.Sqrt(2f * gravity * jumpHeight);
        }
        jumpRequested = false;

        rb.linearVelocity = wish + up * fallSpeed;
    }
}
