using UnityEngine;

// Simple enemy for spherical gravity: wanders randomly until the player is in
// range, then chases. Falls toward the planet via GravityBody.
//
// Setup on the Enemy object:
//   - Rigidbody + Collider (Capsule recommended)
//   - GravityBody (this script turns its Align To Surface OFF and rotates itself)
//   - this script
// Tag your player "Player" or drag it into the Player field.
[RequireComponent(typeof(GravityBody))]
public class EnemyAi : MonoBehaviour
{
    public enum State { Wander, Chase }

    [Header("Target")]
    public Transform player;
    [Tooltip("Start chasing when the player is closer than this.")]
    public float detectRange = 20f;
    [Tooltip("Give up the chase when the player gets farther than this (keep it above Detect Range so the enemy doesn't flicker between states).")]
    public float loseRange = 28f;
    [Tooltip("Stop moving forward when this close (attack range later).")]
    public float stopDistance = 1.5f;

    [Header("Movement")]
    public float wanderSpeed = 3f;
    public float chaseSpeed = 7f;
    [Tooltip("Degrees per second the enemy can turn.")]
    public float turnSpeed = 360f;
    [Tooltip("Small push into the ground while grounded, so it doesn't hop at speed.")]
    public float groundStick = 2f;
    public float groundCheckDistance = 1.1f;
    public LayerMask groundMask = ~0;

    [Header("Wander")]
    [Tooltip("Seconds between picking a new random direction (random within this range).")]
    public Vector2 wanderInterval = new Vector2(2f, 5f);
    [Tooltip("Chance (0-1) each pick that the enemy just stands still for a moment.")]
    [Range(0f, 1f)] public float idleChance = 0.25f;

    public State CurrentState { get; private set; } = State.Wander;
    public bool IsGrounded { get; private set; }

    private GravityBody gravityBody;
    private Rigidbody rb;
    private Vector3 wanderDir;   // world-space direction, re-projected onto the surface each step
    private bool wanderIdle;
    private float nextWanderPick;

    private void Awake()
    {
        gravityBody = GetComponent<GravityBody>();
        gravityBody.alignToSurface = false; // we handle rotation ourselves
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        PickWanderDirection();
    }

    private void FixedUpdate()
    {
        Vector3 pos = rb.position;
        Vector3 up = gravityBody.UpDirection;
        IsGrounded = Physics.Raycast(pos, -up, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);

        UpdateState(pos);

        // --- Decide which way to go (tangent to the surface) ---
        Vector3 moveDir = Vector3.zero;
        float speed = 0f;

        if (CurrentState == State.Chase && player != null)
        {
            Vector3 toPlayer = Vector3.ProjectOnPlane(player.position - pos, up);
            if (toPlayer.magnitude > stopDistance)
            {
                moveDir = toPlayer.normalized;
                speed = chaseSpeed;
            }
            else if (toPlayer.sqrMagnitude > 0.0001f)
            {
                moveDir = toPlayer.normalized; // face the player, don't push into them
            }
        }
        else
        {
            if (Time.time >= nextWanderPick) PickWanderDirection();
            Vector3 w = Vector3.ProjectOnPlane(wanderDir, up);
            if (w.sqrMagnitude > 0.0001f)
            {
                moveDir = w.normalized;
                wanderDir = moveDir; // keep it on the surface as we travel around the sphere
                speed = wanderIdle ? 0f : wanderSpeed;
            }
        }

        // --- Rotate: feet on the surface, then turn toward moveDir ---
        Quaternion rot = rb.rotation;
        Quaternion aligned = Quaternion.FromToRotation(rot * Vector3.up, up) * rot;
        if (moveDir != Vector3.zero)
        {
            Quaternion look = Quaternion.LookRotation(moveDir, up);
            aligned = Quaternion.RotateTowards(aligned, look, turnSpeed * Time.fixedDeltaTime);
        }
        rb.MoveRotation(aligned);

        // --- Move: walk where we're facing, keep gravity's vertical component ---
        Vector3 fwd = Vector3.ProjectOnPlane(aligned * Vector3.forward, up).normalized;
        float fallSpeed = Vector3.Dot(rb.linearVelocity, up);
        if (IsGrounded && fallSpeed <= 0f) fallSpeed = -groundStick;

        rb.linearVelocity = fwd * speed + up * fallSpeed;
    }

    private void UpdateState(Vector3 pos)
    {
        if (player == null) { CurrentState = State.Wander; return; }

        float dist = Vector3.Distance(pos, player.position);
        if (CurrentState == State.Wander && dist <= detectRange)
            CurrentState = State.Chase;
        else if (CurrentState == State.Chase && dist > loseRange)
        {
            CurrentState = State.Wander;
            PickWanderDirection();
        }
    }

    private void PickWanderDirection()
    {
        Vector3 up = gravityBody != null ? gravityBody.UpDirection : transform.up;
        Vector3 rand = Random.onUnitSphere;
        wanderDir = Vector3.ProjectOnPlane(rand, up).normalized;
        wanderIdle = Random.value < idleChance;
        nextWanderPick = Time.time + Random.Range(wanderInterval.x, wanderInterval.y);
    }

    // Bump into a wall while wandering? Pick a new direction.
    private void OnCollisionEnter(Collision c)
    {
        if (CurrentState != State.Wander) return;
        Vector3 up = gravityBody.UpDirection;
        foreach (ContactPoint cp in c.contacts)
        {
            if (Vector3.Dot(cp.normal, up) < 0.5f) // a wall, not the ground
            {
                wanderDir = Vector3.Reflect(wanderDir, cp.normal);
                break;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;    Gizmos.DrawWireSphere(transform.position, loseRange);
    }
}
