using UnityEngine;

/// <summary>
/// Slow / tanky melee enemy (EXAMPLE defaults — tune in inspector).
/// Chases the player; swing only inside attack range, once per range entry.
/// Rearms after leaving range (+ exit slop). Pending entry kept if swing is busy.
/// Hit applies once when EnemyAnimations reports the hit fraction (no anim events).
/// </summary>
public class SwordEnemyStats : EnemyStats
{
    [Header("Sword — EXAMPLE defaults (tunable)")]
    [Tooltip("EXAMPLE high damage per successful swing.")]
    [SerializeField] private int exampleDamage = 20;
    [Tooltip("Melee reach. Halt uses this so the sword can enter range.")]
    [SerializeField] private float attackRange = 2.2f;
    [Tooltip("Must leave attackRange + this to rearm.")]
    [SerializeField] private float exitSlop = 0.35f;
    [Tooltip("Extra reach allowed at the hit frame.")]
    [SerializeField] private float hitRangeSlop = 0.4f;
    [Tooltip("Fallback if attack clip never completes (unlock only; hit still needs fraction or mid-timeout).")]
    [SerializeField] private float swingTimeout = 2.0f;

    private bool inRange;
    private bool attackArmed = true;
    private bool pendingSlash;
    private bool swinging;
    private bool hitApplied;
    private float swingStartedTime;

    private void Reset()
    {
        maxHealth = 150;         // High Health example
        wanderSpeed = 2.0f;      // Slow example
        chaseSpeed = 3.5f;       // Slow example
        detectRange = 20f;
        loseRange = 28f;
        stopDistance = 1.5f;
        exampleDamage = 20;      // High Damage example
        attackRange = 2.2f;
        exitSlop = 0.35f;
        hitRangeSlop = 0.4f;
        swingTimeout = 2.0f;
    }

    protected override float GetHaltDistance()
    {
        return Mathf.Max(0.05f, attackRange);
    }

    protected override void TickCombat()
    {
        if (!PlayerAlive())
        {
            ClearCombatFlags();
            return;
        }

        float dist = DistanceToPlayer();
        facePlayer = CurrentMoveState == MoveState.Chase || swinging;
        movementLocked = swinging;

        if (!inRange)
        {
            if (dist <= attackRange)
            {
                inRange = true;
                OnEnterRange();
            }
        }
        else if (dist > attackRange + exitSlop)
        {
            inRange = false;
            attackArmed = true;
            pendingSlash = false;
        }

        if (inRange)
            TryStartPending();

        if (swinging)
            TickSwing(dist);
    }

    private void OnEnterRange()
    {
        if (!attackArmed) return;
        attackArmed = false;

        if (TryBeginSwing())
            pendingSlash = false;
        else
            pendingSlash = true;
    }

    private void TryStartPending()
    {
        if (!pendingSlash || swinging) return;
        if (!inRange) { pendingSlash = false; return; }
        if (TryBeginSwing())
            pendingSlash = false;
    }

    private bool TryBeginSwing()
    {
        if (swinging) return false;
        if (!PlayerAlive()) return false;

        swinging = true;
        hitApplied = false;
        swingStartedTime = Time.time;

        if (enemyAnimations != null)
            enemyAnimations.PlayAttackOnceThenIdle();
        else
            Debug.LogWarning(name + ": SwordEnemyStats missing EnemyAnimations — timeout swing fallback.", this);

        return true;
    }

    private void TickSwing(float dist)
    {
        bool pastHit;
        bool done;

        if (enemyAnimations != null && enemyAnimations.HasAnimator)
        {
            pastHit = enemyAnimations.HasPassedHitFraction();
            done = enemyAnimations.HasAttackCompletedOrTimedOut(swingTimeout, swingStartedTime);
            // Timeout completes the swing lock only — it does not invent a hit if fraction never passed.
        }
        else
        {
            pastHit = Time.time >= swingStartedTime + swingTimeout * 0.45f;
            done = Time.time >= swingStartedTime + swingTimeout;
        }

        if (!hitApplied && pastHit)
        {
            hitApplied = true; // one opportunity per swing
            if (dist <= attackRange + hitRangeSlop && PlayerAlive())
                playerStats.takeDamage(exampleDamage);
        }

        if (done)
        {
            swinging = false;
            movementLocked = false;
            if (enemyAnimations != null)
                enemyAnimations.EnsureIdle();
            TryStartPending();
        }
    }

    private void ClearCombatFlags()
    {
        inRange = false;
        attackArmed = true;
        pendingSlash = false;
        swinging = false;
        movementLocked = false;
        facePlayer = false;
        if (enemyAnimations != null)
            enemyAnimations.EnsureIdle();
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
