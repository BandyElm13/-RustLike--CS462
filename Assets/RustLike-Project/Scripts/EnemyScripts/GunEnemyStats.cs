using UnityEngine;

/// <summary>
/// Fast / fragile ranged enemy (EXAMPLE defaults — tune in inspector).
/// Sight (range + LOS): stop, play aim/attack anim ONCE, hold end pose, then fire
/// hitscan on an interval while still visible. Do not fire during the aim clip.
/// Lose sight: release pose, resume wander/chase. Re-acquire plays aim once again.
/// Weapon is part of the model — no separate weapon scripts.
/// </summary>
public class GunEnemyStats : EnemyStats
{
    public enum GunPhase { Idle, Aiming, Holding }

    [Header("Gun — EXAMPLE defaults (tunable)")]
    [Tooltip("EXAMPLE low damage per hitscan tick (10 = one HUD segment).")]
    [SerializeField] private int exampleDamage = 5;
    [Tooltip("How far the gunner can see/engage the player.")]
    [SerializeField] private float sightRange = 18f;
    [Tooltip("Seconds between shots while holding and the player stays visible.")]
    [SerializeField] private float fireInterval = 1.0f;
    [Tooltip("Eye height along surface-up for LOS when muzzle is unset (keeps ray off planet mesh).")]
    [SerializeField] private float eyeHeight = 1.2f;
    [Tooltip("Optional muzzle on the attached weapon mesh. Empty = eye-height origin.")]
    [SerializeField] private Transform muzzle;
    [Tooltip("Include player AND environment so walls block. Default Everything — not player-only.")]
    [SerializeField] private LayerMask losAndHitscanMask = ~0;
    [Tooltip("Max seconds to wait for aim clip before forcing hold (unlock fallback only).")]
    [SerializeField] private float aimTimeout = 2.5f;

    public GunPhase Phase { get; private set; } = GunPhase.Idle;

    private float nextFireTime;
    private float aimStartedTime;

    private void Reset()
    {
        // EXAMPLE gunner defaults — inspector edits after add are kept on load.
        maxHealth = 40;          // Low Health example
        wanderSpeed = 4.5f;      // Fast example
        chaseSpeed = 6.5f;       // Fast example
        detectRange = 20f;
        loseRange = 30f;
        stopDistance = 1.5f;
        exampleDamage = 5;       // Low Damage example
        sightRange = 18f;
        fireInterval = 1.0f;
        eyeHeight = 1.2f;
        aimTimeout = 2.5f;
        losAndHitscanMask = ~0;
    }

    protected override void TickCombat()
    {
        if (!PlayerAlive())
        {
            ReleaseSight();
            return;
        }

        float dist = DistanceToPlayer();
        bool inSightRange = dist <= sightRange;
        bool seesPlayer = inSightRange && HasLineOfSight(eyeHeight, losAndHitscanMask, muzzle);

        facePlayer = seesPlayer || Phase != GunPhase.Idle;

        if (seesPlayer)
        {
            movementLocked = true;

            if (Phase == GunPhase.Idle)
            {
                BeginAim();
            }
            else if (Phase == GunPhase.Aiming)
            {
                bool aimDone = enemyAnimations != null
                    ? enemyAnimations.HasAttackCompletedOrTimedOut(aimTimeout, aimStartedTime)
                    : Time.time >= aimStartedTime + aimTimeout;

                if (aimDone)
                    EnterHold();
            }
            else if (Phase == GunPhase.Holding)
            {
                if (enemyAnimations != null)
                    enemyAnimations.MaintainHoldPose();

                if (Time.time >= nextFireTime)
                {
                    TryHitscan();
                    nextFireTime = Time.time + fireInterval;
                }
            }
        }
        else
        {
            ReleaseSight();
        }
    }

    private void BeginAim()
    {
        Phase = GunPhase.Aiming;
        aimStartedTime = Time.time;
        nextFireTime = float.PositiveInfinity;

        if (enemyAnimations != null)
            enemyAnimations.PlayAttackOnceForHold();
        else
            Debug.LogWarning(name + ": GunEnemyStats missing EnemyAnimations — aim uses timeout only.", this);
    }

    private void EnterHold()
    {
        Phase = GunPhase.Holding;
        if (enemyAnimations != null)
            enemyAnimations.EnterHoldAtEndOfAttack();
        nextFireTime = Time.time; // first shot when hold begins (after aim)
    }

    private void ReleaseSight()
    {
        if (Phase == GunPhase.Idle && !movementLocked)
            return;

        Phase = GunPhase.Idle;
        movementLocked = false;
        facePlayer = false;
        if (enemyAnimations != null)
            enemyAnimations.ReleaseHoldToIdle();
    }

    private void TryHitscan()
    {
        if (!PlayerAlive()) return;
        // Live LOS required at fire time (occlusion / out of range cancels the shot)
        if (!HasLineOfSight(eyeHeight, losAndHitscanMask, muzzle)) return;
        playerStats.takeDamage(exampleDamage);
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, sightRange);
    }
}
