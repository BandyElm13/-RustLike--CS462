using UnityEngine;

/// <summary>
/// Fourth enemy file: drives a child Animator with code CrossFades.
/// No Animation Events and no child relay script.
///
/// Animator setup (recommended):
///   - Apply Root Motion OFF
///   - Short state names Idle and Attack on layer 0 (override below)
///   - NO automatic transitions between Idle and Attack (code owns transitions)
///   - Attack clip Loop Time OFF
///
/// Completion / hit timing is per PlayAttack* request:
///   - Ignores Idle that is still current during an outgoing CrossFade
///   - Ignores stale Attack normalizedTime from a previous swing while transitioning
///   - Requires observing the Attack state for THIS request before natural completion or hit
/// </summary>
[DisallowMultipleComponent]
public class EnemyAnimations : MonoBehaviour
{
    [Header("Animator")]
    [Tooltip("Leave empty to find in children.")]
    [SerializeField] private Animator animator;
    [SerializeField] private int layerIndex = 0;
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string attackStateName = "Attack";
    [SerializeField] private float crossFadeDuration = 0.05f;

    [Header("Hit timing (sword)")]
    [Tooltip("Normalized time [0-1] inside Attack when melee damage should apply.")]
    [Range(0.05f, 0.95f)]
    [SerializeField] private float hitNormalizedFraction = 0.45f;

    [Header("Hold (gun)")]
    [Tooltip("Normalized time to freeze at for the held aim/fire pose.")]
    [Range(0.85f, 0.999f)]
    [SerializeField] private float holdNormalizedTime = 0.99f;

    private EnemyStats owner;
    private float defaultAnimatorSpeed = 1f;
    private bool holding;
    private bool loggedMissingAnimator;
    private int attackStateHash;
    private int idleStateHash;

    // Per PlayAttack* request
    private bool attackPlayRequested;
    private bool attackObservedThisRequest;
    private int attackRequestFrame = -1;
    private float attackRequestTime = -1f;

    public bool HasAnimator => animator != null;
    public bool IsHolding => holding;

    public void Bind(EnemyStats stats)
    {
        owner = stats;
        EnsureAnimator();
        CacheHashes();
        CaptureDefaultSpeed();
    }

    private void Awake()
    {
        EnsureAnimator();
        CacheHashes();
        CaptureDefaultSpeed();
    }

    private void OnDisable()
    {
        ForceReleaseHold();
    }

    private void OnDestroy()
    {
        ForceReleaseHold();
    }

    private void EnsureAnimator()
    {
        if (animator != null) return;
        animator = GetComponentInChildren<Animator>();
        if (animator == null && !loggedMissingAnimator)
        {
            loggedMissingAnimator = true;
            Debug.LogWarning(name + ": EnemyAnimations found no Animator in children. Combat will use timeouts only.", this);
        }
    }

    private void CacheHashes()
    {
        attackStateHash = Animator.StringToHash(attackStateName);
        idleStateHash = Animator.StringToHash(idleStateName);
    }

    private void CaptureDefaultSpeed()
    {
        if (animator == null) return;
        animator.applyRootMotion = false;
        defaultAnimatorSpeed = animator.speed > 0.01f ? animator.speed : 1f;
    }

    private void BeginAttackRequest()
    {
        attackPlayRequested = true;
        attackObservedThisRequest = false;
        attackRequestFrame = Time.frameCount;
        attackRequestTime = Time.time;
        holding = false;
    }

    /// <summary>Gun: start Attack once; caller waits then EnterHoldAtEndOfAttack.</summary>
    public void PlayAttackOnceForHold()
    {
        EnsureAnimator();
        if (animator == null)
        {
            BeginAttackRequest();
            return;
        }

        RestoreSpeed();
        BeginAttackRequest();
        animator.CrossFadeInFixedTime(attackStateHash, crossFadeDuration, layerIndex, 0f);
    }

    /// <summary>Gun: freeze this Animator at the end of Attack (pose hold).</summary>
    public void EnterHoldAtEndOfAttack()
    {
        EnsureAnimator();
        if (animator == null) return;

        holding = true;
        attackPlayRequested = false;
        attackObservedThisRequest = false;

        RestoreSpeed();
        animator.Play(attackStateHash, layerIndex, holdNormalizedTime);
        // Sample the held pose immediately so we do not keep a frozen mid-clip frame
        animator.Update(0f);
        animator.speed = 0f;
    }

    /// <summary>Gun: keep hold if something tried to advance the state.</summary>
    public void MaintainHoldPose()
    {
        if (!holding || animator == null) return;
        animator.speed = 0f;
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layerIndex);
        if (info.shortNameHash != attackStateHash)
        {
            animator.Play(attackStateHash, layerIndex, holdNormalizedTime);
            animator.Update(0f);
            animator.speed = 0f;
        }
    }

    /// <summary>Gun: leave hold, restore speed, return to Idle.</summary>
    public void ReleaseHoldToIdle()
    {
        holding = false;
        ClearAttackRequest();
        EnsureAnimator();
        if (animator == null) return;

        RestoreSpeed();
        animator.CrossFadeInFixedTime(idleStateHash, crossFadeDuration, layerIndex, 0f);
    }

    public void ForceReleaseHold()
    {
        holding = false;
        ClearAttackRequest();
        if (animator != null)
            RestoreSpeed();
    }

    /// <summary>Sword: play Attack once; caller monitors hit/completion then EnsureIdle.</summary>
    public void PlayAttackOnceThenIdle()
    {
        EnsureAnimator();
        if (animator == null)
        {
            BeginAttackRequest();
            return;
        }

        RestoreSpeed();
        BeginAttackRequest();
        animator.CrossFadeInFixedTime(attackStateHash, crossFadeDuration, layerIndex, 0f);
    }

    public void EnsureIdle()
    {
        if (holding) return;
        EnsureAnimator();
        ClearAttackRequest();
        if (animator == null) return;

        RestoreSpeed();
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layerIndex);
        if (info.shortNameHash != idleStateHash)
            animator.CrossFadeInFixedTime(idleStateHash, crossFadeDuration, layerIndex, 0f);
    }

    public bool IsAttackStateActive()
    {
        if (animator == null) return false;
        if (animator.IsInTransition(layerIndex)) return false;
        return animator.GetCurrentAnimatorStateInfo(layerIndex).shortNameHash == attackStateHash;
    }

    public float AttackNormalizedTime
    {
        get
        {
            if (animator == null) return attackPlayRequested ? 0f : 1f;
            UpdateObservation();
            if (!attackObservedThisRequest) return 0f;
            if (animator.IsInTransition(layerIndex)) return 0f;

            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layerIndex);
            if (info.shortNameHash != attackStateHash) return 0f;
            // normalizedTime can exceed 1 if the state kept ticking; clamp for comparisons
            return Mathf.Clamp01(info.normalizedTime);
        }
    }

    public bool HasPassedHitFraction()
    {
        if (animator == null) return false;
        if (!attackPlayRequested) return false;
        // Same-frame / transitioning: never hit from a stale prior Attack sample
        if (Time.frameCount <= attackRequestFrame) return false;

        UpdateObservation();
        if (!attackObservedThisRequest) return false;
        if (animator.IsInTransition(layerIndex)) return false;

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layerIndex);
        if (info.shortNameHash != attackStateHash) return false;
        return info.normalizedTime >= hitNormalizedFraction;
    }

    /// <summary>
    /// True when THIS request's Attack has reached ~end after being observed,
    /// or wall-clock timeout elapsed. Timeout does not deal damage.
    /// Never treats "still Idle during CrossFade into Attack" as complete.
    /// </summary>
    public bool HasAttackCompletedOrTimedOut(float timeoutSeconds, float startedTime)
    {
        if (Time.time >= startedTime + timeoutSeconds)
            return true;

        if (animator == null)
            return false; // wait for caller timeout only

        // Never complete on the request frame (CrossFade has not entered Attack yet)
        if (Time.frameCount <= attackRequestFrame)
            return false;

        // Incoming/outgoing transition: current state may still be Idle or a stale Attack
        if (animator.IsInTransition(layerIndex))
            return false;

        UpdateObservation();
        if (!attackObservedThisRequest)
            return false;

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layerIndex);
        if (info.shortNameHash == attackStateHash)
            return info.normalizedTime >= 0.98f;

        // Observed Attack for this request, then left Attack (and not transitioning) → done
        if (attackPlayRequested && info.shortNameHash != attackStateHash)
            return true;

        return false;
    }

    /// <summary>
    /// Mark that the Attack state for the current request has actually become current
    /// (not merely requested). Safe against Idle-during-CrossFade and stale prior Attack.
    /// </summary>
    private void UpdateObservation()
    {
        if (!attackPlayRequested || animator == null) return;
        if (attackObservedThisRequest) return;
        if (Time.frameCount <= attackRequestFrame) return;
        if (animator.IsInTransition(layerIndex)) return;

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layerIndex);
        if (info.shortNameHash != attackStateHash) return;

        // Reject a leftover end-of-clip sample from a previous swing that somehow
        // remained current without a fresh transition (normalized near 1 right away).
        // A brand-new Attack after CrossFade starts near 0.
        if (info.normalizedTime >= 0.98f && Time.time < attackRequestTime + 0.05f)
            return;

        attackObservedThisRequest = true;
    }

    private void ClearAttackRequest()
    {
        attackPlayRequested = false;
        attackObservedThisRequest = false;
        attackRequestFrame = -1;
        attackRequestTime = -1f;
    }

    private void RestoreSpeed()
    {
        if (animator == null) return;
        animator.speed = defaultAnimatorSpeed > 0.01f ? defaultAnimatorSpeed : 1f;
    }
}
