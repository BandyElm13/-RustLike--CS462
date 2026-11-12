using UnityEngine;

/// <summary>
/// PARENT enemy: shared health/score/death + spherical Rigidbody/GravityBody locomotion.
/// Attach exactly ONE subclass on the root (GunEnemyStats OR SwordEnemyStats), never this
/// base alone and never base+subclass together. Keep GravityBody, Rigidbody, root collider.
///
/// Old EnemyAi.cs in the repo still compiles on its own; remove the EnemyAi *component*
/// from prefabs (and optionally delete the script later) so locomotion is not doubled.
/// </summary>
[RequireComponent(typeof(GravityBody))]
[RequireComponent(typeof(Rigidbody))]
public class EnemyStats : MonoBehaviour
{
    public enum MoveState { Wander, Chase }

    [Header("Shared health (override defaults on subclasses via Reset / inspector)")]
    [SerializeField] protected int maxHealth = 100;

    // Global team score (ScoreDisplay reads this). Reset per run by PressPlay.
    public static int currentScore = 0;

    // Start every Play session at 0, even with Domain Reload off (e.g. Play pressed in a planet scene).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetScoreOnPlay() => currentScore = 0;

    [Header("Target")]
    public Transform player;
    [Tooltip("Start chasing when the player is closer than this.")]
    public float detectRange = 20f;
    [Tooltip("Stop chasing when farther than this (keep above Detect Range).")]
    public float loseRange = 28f;

    [Header("Shared movement")]
    [Tooltip("Used when subclass does not force a stop. Subclasses may override via HaltDistance.")]
    public float stopDistance = 1.5f;
    public float wanderSpeed = 3f;
    public float chaseSpeed = 5f;
    public float turnSpeed = 360f;
    public float groundStick = 2f;
    public float groundCheckDistance = 1.1f;
    public LayerMask groundMask = ~0;

    [Header("Wander")]
    public Vector2 wanderInterval = new Vector2(2f, 5f);
    [Range(0f, 1f)] public float idleChance = 0.25f;

    public MoveState CurrentMoveState { get; private set; } = MoveState.Wander;
    public bool IsGrounded { get; private set; }
    public bool IsDead => isDead;
    public int CurrentHealth => currentEnemyHealth;
    public int MaxHealth => maxHealth;

    protected PlayerStats playerStats;
    protected GravityBody gravityBody;
    protected Rigidbody rb;
    protected EnemyAnimations enemyAnimations;
    private EnemyHitFlash hitFlash;

    private int currentEnemyHealth;
    private WaveManager waveManager;
    private bool isDead;
    private Vector3 wanderDir;
    private bool wanderIdle;
    private float nextWanderPick;

    /// <summary>Subclass: lock tangential walk (gun aim/hold). Turning may still happen.</summary>
    protected bool movementLocked;

    /// <summary>Subclass: prefer facing the player this frame.</summary>
    protected bool facePlayer;

    private static PhysicsMaterial noFriction;
    private static PhysicsMaterial NoFriction => noFriction != null ? noFriction : (noFriction = new PhysicsMaterial("EnemyNoFriction")
    {
        dynamicFriction = 0f,
        staticFriction = 0f,
        frictionCombine = PhysicsMaterialCombine.Minimum,
        bounciness = 0f,
        bounceCombine = PhysicsMaterialCombine.Minimum
    });

    protected virtual void Awake()
    {
        gravityBody = GetComponent<GravityBody>();
        gravityBody.alignToSurface = false;
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Velocity is set directly each step; friction would just snag enemies on hills and rocks.
        Collider col = GetComponent<Collider>();
        if (col != null && col.sharedMaterial == null)
            col.sharedMaterial = NoFriction;

        enemyAnimations = GetComponent<EnemyAnimations>();
        if (enemyAnimations == null)
            enemyAnimations = GetComponentInChildren<EnemyAnimations>();

        // Red overlay when shot (added here so every enemy prefab gets it).
        hitFlash = GetComponent<EnemyHitFlash>();
        if (hitFlash == null) hitFlash = gameObject.AddComponent<EnemyHitFlash>();
    }

    protected virtual void Start()
    {
        currentEnemyHealth = maxHealth;
        waveManager = FindAnyObjectByType<WaveManager>();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (player != null)
            playerStats = player.GetComponentInParent<PlayerStats>();

        if (enemyAnimations != null)
            enemyAnimations.Bind(this);

        PickWanderDirection();
        OnEnemyStart();
    }

    protected virtual void OnEnemyStart() { }

    protected virtual void OnEnable() { }

    protected virtual void OnDisable()
    {
        if (enemyAnimations != null)
            enemyAnimations.ForceReleaseHold();
    }

    protected virtual void Update()
    {
        if (isDead) return;
        TickCombat();
    }

    /// <summary>Subclass combat / sight / melee logic.</summary>
    protected virtual void TickCombat() { }

    protected virtual void FixedUpdate()
    {
        if (isDead) return;

        Vector3 pos = rb.position;
        Vector3 up = gravityBody.UpDirection;
        IsGrounded = Physics.Raycast(pos, -up, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);

        UpdateMoveState(pos);
        RunLocomotion(pos, up);
    }

    private void UpdateMoveState(Vector3 pos)
    {
        if (player == null)
        {
            CurrentMoveState = MoveState.Wander;
            return;
        }

        float dist = Vector3.Distance(pos, player.position);
        if (CurrentMoveState == MoveState.Wander && dist <= detectRange)
            CurrentMoveState = MoveState.Chase;
        else if (CurrentMoveState == MoveState.Chase && dist > loseRange)
        {
            CurrentMoveState = MoveState.Wander;
            PickWanderDirection();
        }
    }

    private void RunLocomotion(Vector3 pos, Vector3 up)
    {
        Vector3 moveDir = Vector3.zero;
        float speed = 0f;

        bool wantFace = facePlayer && player != null;
        bool locked = movementLocked;

        if (locked)
        {
            speed = 0f;
            if (wantFace && player != null)
            {
                Vector3 toPlayer = Vector3.ProjectOnPlane(player.position - pos, up);
                if (toPlayer.sqrMagnitude > 0.0001f)
                    moveDir = toPlayer.normalized;
            }
        }
        else if (CurrentMoveState == MoveState.Chase && player != null)
        {
            Vector3 toPlayer = Vector3.ProjectOnPlane(player.position - pos, up);
            float halt = GetHaltDistance();
            float dist3 = Vector3.Distance(pos, player.position);

            if (dist3 > halt)
            {
                if (toPlayer.sqrMagnitude > 0.0001f)
                {
                    moveDir = toPlayer.normalized;
                    speed = chaseSpeed;
                }
            }
            else if (toPlayer.sqrMagnitude > 0.0001f)
            {
                moveDir = toPlayer.normalized;
                speed = 0f;
            }
        }
        else
        {
            if (Time.time >= nextWanderPick) PickWanderDirection();
            Vector3 w = Vector3.ProjectOnPlane(wanderDir, up);
            if (w.sqrMagnitude > 0.0001f)
            {
                moveDir = w.normalized;
                wanderDir = moveDir;
                speed = wanderIdle ? 0f : wanderSpeed;
            }
        }

        Quaternion rot = rb.rotation;
        Quaternion aligned = Quaternion.FromToRotation(rot * Vector3.up, up) * rot;
        if (moveDir != Vector3.zero)
        {
            Quaternion look = Quaternion.LookRotation(moveDir, up);
            aligned = Quaternion.RotateTowards(aligned, look, turnSpeed * Time.fixedDeltaTime);
        }
        rb.MoveRotation(aligned);

        Vector3 fwd = Vector3.ProjectOnPlane(aligned * Vector3.forward, up).normalized;
        float fallSpeed = Vector3.Dot(rb.linearVelocity, up);
        if (IsGrounded && fallSpeed <= 0f) fallSpeed = -groundStick;
        rb.linearVelocity = fwd * speed + up * fallSpeed;
    }

    /// <summary>Distance at which chase speed becomes zero. Subclasses may tighten this.</summary>
    protected virtual float GetHaltDistance()
    {
        return Mathf.Max(0.05f, stopDistance);
    }

    private void PickWanderDirection()
    {
        Vector3 up = gravityBody != null ? gravityBody.UpDirection : transform.up;
        wanderDir = Vector3.ProjectOnPlane(Random.onUnitSphere, up).normalized;
        wanderIdle = Random.value < idleChance;
        nextWanderPick = Time.time + Random.Range(wanderInterval.x, wanderInterval.y);
    }

    protected virtual void OnCollisionEnter(Collision c)
    {
        if (CurrentMoveState != MoveState.Wander || movementLocked) return;
        Vector3 up = gravityBody.UpDirection;
        foreach (ContactPoint cp in c.contacts)
        {
            if (Vector3.Dot(cp.normal, up) < 0.5f)
            {
                wanderDir = Vector3.Reflect(wanderDir, cp.normal);
                break;
            }
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;
        if (amount <= 0) return;

        if (hitFlash != null) hitFlash.Flash();

        currentEnemyHealth -= amount;
        if (currentEnemyHealth <= 0)
        {
            currentEnemyHealth = 0;
            EnemyDeath();
        }
    }

    /// <summary>Kill outright (e.g. fell off the map). Counts as a normal death.</summary>
    public void Kill()
    {
        if (isDead) return;
        currentEnemyHealth = 0;
        EnemyDeath();
    }

    private void EnemyDeath()
    {
        if (isDead) return;
        isDead = true;

        movementLocked = false;
        if (enemyAnimations != null)
            enemyAnimations.ForceReleaseHold();

        currentScore += 10;
        Debug.Log("Enemy Defeated! Global Score: " + currentScore);

        if (waveManager != null)
            waveManager.EnemyDied();

        OnEnemyDeath();
        Destroy(gameObject);
    }

    protected virtual void OnEnemyDeath() { }

    protected float DistanceToPlayer()
    {
        if (player == null) return float.MaxValue;
        return Vector3.Distance(transform.position, player.position);
    }

    protected bool PlayerAlive()
    {
        return player != null && playerStats != null && !playerStats.isDead;
    }

    /// <summary>
    /// Line of sight from an elevated origin (avoids planet ground) to the player collider/pivot.
    /// Skips this enemy's hierarchy; nearest other collider must be the player.
    /// </summary>
    protected bool HasLineOfSight(float eyeHeight, LayerMask mask, Transform aimOrigin = null)
    {
        if (!PlayerAlive()) return false;

        Vector3 up = gravityBody != null ? gravityBody.UpDirection : transform.up;
        Vector3 origin = aimOrigin != null
            ? aimOrigin.position
            : transform.position + up * eyeHeight;

        Vector3 target = player.position;
        Collider pc = player.GetComponentInParent<Collider>();
        if (pc == null) pc = player.GetComponentInChildren<Collider>();
        if (pc != null) target = pc.bounds.center;

        Vector3 to = target - origin;
        float dist = to.magnitude;
        if (dist < 0.05f) return true;

        Vector3 dir = to / dist;
        int m = mask.value != 0 ? mask.value : ~0;
        RaycastHit[] hits = Physics.RaycastAll(origin, dir, dist + 0.05f, m, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
            return false;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null) continue;
            if (IsUnder(transform, hit.collider.transform)) continue;

            PlayerStats ps = hit.collider.GetComponentInParent<PlayerStats>();
            return ps != null && ps == playerStats;
        }
        return false;
    }

    protected static bool IsUnder(Transform root, Transform t)
    {
        while (t != null)
        {
            if (t == root) return true;
            t = t.parent;
        }
        return false;
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, loseRange);
    }
}
