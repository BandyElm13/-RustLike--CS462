using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns completed gun and sword enemy prefabs with a fixed independent 50/50 roll
/// each spawn (not a guaranteed equal split per wave).
///
/// Enemies spawn at a random, clear spot on the planet surface a set distance from the
/// player (works on spherical planets via GravityAttractor, or flat ground if none), and
/// only where the player's camera can't see them (off-screen or hidden behind the planet
/// or scenery). Enemies that drift too far away are quietly moved back near the player,
/// again only while they're out of view and only to a spot that's out of view.
/// Spawn Points are optional: they're only used if no safe spot near the player is found.
///
/// Inspector: only two enemy slots — assign finished prefabs (root has GunEnemyStats or
/// SwordEnemyStats + EnemyAnimations + Rigidbody/GravityBody/collider), NOT raw FBX/models.
/// </summary>
public class WaveManager : MonoBehaviour
{
    [Header("Wave Settings")]
    [SerializeField] private int startingEnemies = 3;
    [SerializeField] private int enemiesAddedPerWave = 1;
    [SerializeField] private float timeBetweenWaves = 10f;

    [Header("Enemy Prefabs (completed roots only — not raw models)")]
    [Tooltip("Finished sword prefab: root SwordEnemyStats + EnemyAnimations + physics.")]
    [SerializeField] private GameObject swordEnemyPrefab;
    [Tooltip("Finished gun prefab: root GunEnemyStats + EnemyAnimations + physics.")]
    [SerializeField] private GameObject gunEnemyPrefab;

    [Header("Spawning")]
    [SerializeField] private float spawnInterval = 1f;

    [Header("Spawn Near Player")]
    [Tooltip("Leave empty to find the object tagged Player.")]
    [SerializeField] private Transform player;
    [Tooltip("Enemies never spawn closer to the player than this (distance walked along the surface).")]
    [SerializeField] private float spawnMinDistance = 40f;
    [Tooltip("Enemies spawn at most this far from the player (distance walked along the surface).")]
    [SerializeField] private float spawnMaxDistance = 90f;
    [Tooltip("How far above the ground the enemy is placed; gravity drops it the rest of the way.")]
    [SerializeField] private float spawnHeight = 1f;
    [Tooltip("Spawn spot must be free of other colliders within this radius.")]
    [SerializeField] private float clearanceRadius = 0.75f;
    [Tooltip("Random spots tried per enemy before falling back to Spawn Points.")]
    [SerializeField] private int placementAttempts = 20;
    [Tooltip("Layers that count as ground for placing enemies (and that can hide enemies from view).")]
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Stay Out Of View")]
    [Tooltip("Only spawn / move enemies where the player's camera can't see them.")]
    [SerializeField] private bool keepOutOfView = true;
    [Tooltip("Camera to hide from. Leave empty to use the player's camera (or Camera.main).")]
    [SerializeField] private Camera viewCamera;
    [Tooltip("Extra border around the screen that still counts as visible (0.1 = 10% of the screen).")]
    [SerializeField] private float viewportMargin = 0.1f;
    [Tooltip("A spot blocked from the camera by the planet, hills or buildings counts as out of view.")]
    [SerializeField] private bool blockedCountsAsHidden = true;
    [Tooltip("If no hidden spot is found this many times in a row, spawn anyway so the wave can't stall.")]
    [SerializeField] private int hiddenSpawnRetries = 10;

    [Header("Relocate Far Enemies")]
    [Tooltip("Move enemies that end up very far away (e.g. the other side of the planet) back toward the player, only while out of view.")]
    [SerializeField] private bool relocateFarEnemies = true;
    [Tooltip("Enemies farther than this (distance along the surface) get moved. Keep well above Spawn Max Distance.")]
    [SerializeField] private float relocateThreshold = 150f;
    [Tooltip("Relocated enemies are placed at least this far from the player (a bit closer than normal spawns).")]
    [SerializeField] private float relocateMinDistance = 25f;
    [Tooltip("Relocated enemies are placed at most this far from the player.")]
    [SerializeField] private float relocateMaxDistance = 50f;
    [Tooltip("Seconds between distance checks.")]
    [SerializeField] private float relocateCheckInterval = 1f;
    [Tooltip("Most enemies moved per check, so they trickle back instead of all at once.")]
    [SerializeField] private int maxRelocationsPerCheck = 3;

    [Header("Fallback (optional)")]
    [Tooltip("Only used if no safe spot near the player is found. Empty entries are ignored.")]
    [SerializeField] private Transform[] spawnPoints;

    private const float GunPickChance = 0.5f;
    private const float RayStartAbove = 50f;
    private const float DefaultEnemyHeight = 2f;

    private readonly List<EnemyStats> activeEnemies = new List<EnemyStats>();

    private int currentWave = 0;
    private int enemiesAlive = 0;
    private int enemiesToSpawn = 0;
    private float waveTimer = 0f;
    private float spawnTimer = 0f;
    private float relocateTimer = 0f;
    private int failedSpawnTries = 0;
    private bool waitingForNextWave = false;
    private bool spawningEnemies = false;
    private bool spawnConfigValid = false;

    public int CurrentWave => currentWave;
    public int EnemiesAlive => enemiesAlive;

    private void Awake()
    {
        ValidatePrefabs();
    }

    private void Start()
    {
        if (AudioManager.instance != null)
            AudioManager.instance.TriggerMusicState("wavestart");

        if (!spawnConfigValid)
        {
            Debug.LogError("WaveManager: invalid enemy prefab config — waves will not start.", this);
            return;
        }

        StartWave();
    }

    private void OnValidate()
    {
        spawnMinDistance = Mathf.Max(0f, spawnMinDistance);
        spawnMaxDistance = Mathf.Max(spawnMinDistance, spawnMaxDistance);
        relocateMinDistance = Mathf.Max(0f, relocateMinDistance);
        relocateMaxDistance = Mathf.Max(relocateMinDistance, relocateMaxDistance);
        relocateThreshold = Mathf.Max(relocateMaxDistance, spawnMaxDistance, relocateThreshold);
        placementAttempts = Mathf.Max(1, placementAttempts);
        relocateCheckInterval = Mathf.Max(0.1f, relocateCheckInterval);
        maxRelocationsPerCheck = Mathf.Max(1, maxRelocationsPerCheck);
        hiddenSpawnRetries = Mathf.Max(0, hiddenSpawnRetries);
    }

    private void ValidatePrefabs()
    {
        spawnConfigValid = false;

        if (swordEnemyPrefab == null || gunEnemyPrefab == null)
        {
            Debug.LogError(
                "WaveManager: assign BOTH swordEnemyPrefab and gunEnemyPrefab " +
                "(completed enemy prefabs, not raw models).", this
            );
            return;
        }

        if (!PrefabHasExactSubclass(swordEnemyPrefab, typeof(SwordEnemyStats)))
        {
            Debug.LogError(
                "WaveManager: swordEnemyPrefab root must have SwordEnemyStats " +
                "(subclass of EnemyStats). Do not drag a raw mesh/FBX here.", this
            );
            return;
        }

        if (!PrefabHasExactSubclass(gunEnemyPrefab, typeof(GunEnemyStats)))
        {
            Debug.LogError(
                "WaveManager: gunEnemyPrefab root must have GunEnemyStats " +
                "(subclass of EnemyStats). Do not drag a raw mesh/FBX here.", this
            );
            return;
        }

        spawnConfigValid = true;
    }

    private static bool PrefabHasExactSubclass(GameObject prefab, System.Type subtype)
    {
        if (prefab == null) return false;
        EnemyStats stats = prefab.GetComponent<EnemyStats>();
        if (stats == null) return false;
        return subtype.IsInstanceOfType(stats);
    }

    private void Update()
    {
        if (!spawnConfigValid) return;

        if (relocateFarEnemies)
        {
            relocateTimer -= Time.deltaTime;
            if (relocateTimer <= 0f)
            {
                relocateTimer = relocateCheckInterval;
                if (FindPlayer()) RelocateFarEnemies();
            }
        }

        if (waitingForNextWave)
        {
            waveTimer -= Time.deltaTime;
            if (waveTimer <= 0f)
                StartWave();
            return;
        }

        if (spawningEnemies)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f) return;
            spawnTimer = spawnInterval;

            // No player yet (or it was destroyed): wait instead of using up the wave.
            if (!FindPlayer()) return;

            // Give up on staying hidden after repeated failures so the wave can't stall forever.
            bool requireHidden = keepOutOfView && failedSpawnTries < hiddenSpawnRetries;

            if (!SpawnEnemy(requireHidden))
            {
                // Try again next interval without using up this enemy.
                failedSpawnTries++;
                return;
            }

            failedSpawnTries = 0;
            enemiesToSpawn--;

            if (enemiesToSpawn <= 0)
            {
                spawningEnemies = false;
                if (enemiesAlive <= 0)
                    WaveCompleted();
            }
        }
    }

    private void StartWave()
    {
        currentWave++;
        enemiesToSpawn = startingEnemies + ((currentWave - 1) * enemiesAddedPerWave);
        spawningEnemies = true;
        waitingForNextWave = false;
        spawnTimer = 0f;
        failedSpawnTries = 0;
        Debug.Log("Starting Wave " + currentWave + " with " + enemiesToSpawn + " enemies.");
    }

    private bool FindPlayer()
    {
        if (player != null) return true;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            viewCamera = null; // re-pick the camera for the new player
        }
        return player != null;
    }

    // ------------------------------------------------------------------ Spawning

    private bool SpawnEnemy(bool requireHidden)
    {
        GameObject prefab = ChooseEnemyPrefab();
        if (prefab == null) return false;

        float height = EnemyHeight(prefab);

        if (!TryFindSpotNearPlayer(spawnMinDistance, spawnMaxDistance, height, requireHidden, out Vector3 position, out Quaternion rotation) &&
            !TryFallbackSpawnPoint(height, requireHidden, out position, out rotation))
            return false;

        GameObject instance = Instantiate(prefab, position, rotation);
        enemiesAlive++;

        EnemyStats stats = instance.GetComponent<EnemyStats>();
        if (stats != null) activeEnemies.Add(stats);
        return true;
    }

    /// <summary>
    /// Picks a random point a ring-distance away from the player along the planet surface,
    /// raycasts down onto the ground, and checks the spot is unobstructed (and, if required,
    /// out of the camera's view).
    /// </summary>
    private bool TryFindSpotNearPlayer(float minDistance, float maxDistance, float enemyHeight, bool requireHidden,
                                       out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        if (player == null) return false;

        GravityAttractor attractor = FindAttractor();
        Vector3 playerPos = player.position;

        for (int i = 0; i < placementAttempts; i++)
        {
            float distance = Random.Range(minDistance, maxDistance);
            Vector3 up;
            Vector3 probe;

            if (attractor != null)
            {
                // Walk 'distance' along the sphere surface in a random direction.
                Vector3 center = attractor.Center;
                Vector3 fromCenter = playerPos - center;
                float radius = fromCenter.magnitude;
                if (radius < 0.01f) return false;

                Vector3 playerUp = fromCenter / radius;
                // Any direction — in front or behind. Far spots in front are usually
                // already hidden past the planet's curve; the visibility check decides.
                Vector3 tangent = RandomTangent(playerUp);
                float angle = Mathf.Min(distance / radius * Mathf.Rad2Deg, 170f);
                up = Quaternion.AngleAxis(angle, Vector3.Cross(playerUp, tangent)) * playerUp;
                probe = center + up * (radius + RayStartAbove);
            }
            else
            {
                // Flat world fallback.
                up = Vector3.up;
                probe = playerPos + RandomTangent(up) * distance + up * RayStartAbove;
            }

            if (!Physics.Raycast(probe, -up, out RaycastHit hit, RayStartAbove * 4f, groundMask, QueryTriggerInteraction.Ignore))
                continue;

            // Don't land on top of the player or another enemy.
            if (hit.collider.GetComponentInParent<PlayerStats>() != null) continue;
            if (hit.collider.GetComponentInParent<EnemyStats>() != null) continue;

            Vector3 candidate = hit.point + up * spawnHeight;
            if (SurfaceDistance(candidate, playerPos) < minDistance * 0.9f) continue;
            if (Physics.CheckSphere(candidate, clearanceRadius, ~0, QueryTriggerInteraction.Ignore)) continue;
            if (requireHidden && !IsHiddenFromCamera(candidate, up, enemyHeight)) continue;

            position = candidate;
            rotation = FacePlayer(candidate, up, playerPos);
            return true;
        }

        return false;
    }

    private bool TryFallbackSpawnPoint(float enemyHeight, bool requireHidden, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        if (spawnPoints == null) return false;

        List<Transform> usable = new List<Transform>();
        foreach (Transform t in spawnPoints)
        {
            if (t == null) continue;
            if (requireHidden && !IsHiddenFromCamera(t.position, UpAt(t.position), enemyHeight)) continue;
            usable.Add(t);
        }
        if (usable.Count == 0) return false;

        Transform pick = usable[Random.Range(0, usable.Count)];
        position = pick.position;
        rotation = pick.rotation;
        return true;
    }

    // ------------------------------------------------------------------ Relocating

    private void RelocateFarEnemies()
    {
        int moved = 0;

        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            EnemyStats enemy = activeEnemies[i];
            if (enemy == null || enemy.IsDead)
            {
                activeEnemies.RemoveAt(i);
                continue;
            }

            if (moved >= maxRelocationsPerCheck) continue;

            Vector3 current = enemy.transform.position;
            if (SurfaceDistance(current, player.position) < relocateThreshold) continue;

            float height = EnemyHeight(enemy.gameObject);

            // Never make an enemy vanish while the player is looking at it.
            if (keepOutOfView && !IsHiddenFromCamera(current, UpAt(current), height)) continue;

            // Lost track of the player: bring it back a little closer than a normal spawn.
            if (!TryFindSpotNearPlayer(relocateMinDistance, relocateMaxDistance, height, keepOutOfView,
                                       out Vector3 position, out Quaternion rotation))
                continue;

            Teleport(enemy, position, rotation);
            moved++;
        }
    }

    private static void Teleport(EnemyStats enemy, Vector3 position, Quaternion rotation)
    {
        enemy.transform.SetPositionAndRotation(position, rotation);

        Rigidbody body = enemy.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.position = position;
            body.rotation = rotation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }

    // ------------------------------------------------------------------ Visibility

    private Camera GetViewCamera()
    {
        if (viewCamera != null && viewCamera.isActiveAndEnabled) return viewCamera;

        FirstPersonController fpc = player != null ? player.GetComponentInParent<FirstPersonController>() : null;
        viewCamera = fpc != null && fpc.Camera != null ? fpc.Camera : Camera.main;
        return viewCamera;
    }

    /// <summary>
    /// True if no part of an enemy standing at 'basePos' (feet, middle, head) can be seen
    /// by the camera: off-screen, behind it, or blocked by the planet/scenery.
    /// </summary>
    private bool IsHiddenFromCamera(Vector3 basePos, Vector3 up, float enemyHeight)
    {
        Camera cam = GetViewCamera();
        if (cam == null) return true;

        for (int i = 0; i <= 2; i++)
        {
            Vector3 point = basePos + up * (enemyHeight * i * 0.5f);
            if (IsPointVisible(cam, point)) return false;
        }
        return true;
    }

    private bool IsPointVisible(Camera cam, Vector3 point)
    {
        Vector3 vp = cam.WorldToViewportPoint(point);
        if (vp.z <= 0f) return false; // behind the camera

        float m = viewportMargin;
        if (vp.x < -m || vp.x > 1f + m || vp.y < -m || vp.y > 1f + m) return false; // off-screen

        if (!blockedCountsAsHidden) return true;

        // On screen: hidden only if scenery (not the player or an enemy) is in the way.
        // The cast starts inside the player's own collider, so that collider is ignored.
        if (Physics.Linecast(cam.transform.position, point, out RaycastHit hit, groundMask, QueryTriggerInteraction.Ignore))
        {
            bool blockedByCharacter =
                hit.collider.GetComponentInParent<PlayerStats>() != null ||
                hit.collider.GetComponentInParent<EnemyStats>() != null;
            return blockedByCharacter;
        }

        return true;
    }

    // ------------------------------------------------------------------ Helpers

    /// <summary>Approximate standing height from the root CapsuleCollider (prefab or instance).</summary>
    private static float EnemyHeight(GameObject enemy)
    {
        CapsuleCollider capsule = enemy.GetComponent<CapsuleCollider>();
        if (capsule == null) return DefaultEnemyHeight;

        float top = capsule.center.y + capsule.height * 0.5f;
        return Mathf.Max(0.5f, top * Mathf.Abs(enemy.transform.lossyScale.y));
    }

    /// <summary>
    /// Distance you'd walk along the planet surface between two points (arc length).
    /// Straight-line distance on flat ground.
    /// </summary>
    private float SurfaceDistance(Vector3 a, Vector3 b)
    {
        GravityAttractor attractor = FindAttractor();
        if (attractor == null) return Vector3.Distance(a, b);

        Vector3 da = a - attractor.Center;
        Vector3 db = b - attractor.Center;
        if (da.sqrMagnitude < 0.0001f || db.sqrMagnitude < 0.0001f) return Vector3.Distance(a, b);

        float radius = (da.magnitude + db.magnitude) * 0.5f;
        return Vector3.Angle(da, db) * Mathf.Deg2Rad * radius;
    }

    private Vector3 UpAt(Vector3 position)
    {
        GravityAttractor attractor = FindAttractor();
        if (attractor == null) return Vector3.up;
        Vector3 d = position - attractor.Center;
        return d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.up;
    }

    private GravityAttractor FindAttractor()
    {
        GravityBody body = player != null ? player.GetComponentInParent<GravityBody>() : null;
        if (body != null)
            return body.useWorldGravity ? null : body.ActiveAttractor;
        return GravityAttractor.Main;
    }

    private static Vector3 RandomTangent(Vector3 up)
    {
        Vector3 t = Vector3.ProjectOnPlane(Random.onUnitSphere, up);
        return t.sqrMagnitude > 0.0001f ? t.normalized : Vector3.Cross(up, Vector3.right).normalized;
    }

    private static Quaternion FacePlayer(Vector3 from, Vector3 up, Vector3 playerPos)
    {
        Vector3 look = Vector3.ProjectOnPlane(playerPos - from, up);
        if (look.sqrMagnitude < 0.0001f) look = Vector3.ProjectOnPlane(Vector3.forward, up);
        if (look.sqrMagnitude < 0.0001f) look = Vector3.ProjectOnPlane(Vector3.right, up);
        return Quaternion.LookRotation(look.normalized, up);
    }

    private GameObject ChooseEnemyPrefab()
    {
        // Fixed independent 50/50 — not a guaranteed equal split within one wave.
        if (Random.value < GunPickChance)
            return gunEnemyPrefab;
        return swordEnemyPrefab;
    }

    public void EnemyDied()
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);

        if (enemiesAlive <= 0 && !spawningEnemies && !waitingForNextWave)
            WaveCompleted();
    }

    private void WaveCompleted()
    {
        Debug.Log("Wave " + currentWave + " completed!");
        waitingForNextWave = true;
        waveTimer = timeBetweenWaves;
    }

    private void OnDrawGizmosSelected()
    {
        if (player == null) return;
        // Straight-line spheres: a close approximation of the surface distances for small rings.
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(player.position, spawnMinDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(player.position, spawnMaxDistance);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(player.position, relocateMaxDistance);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(player.position, relocateThreshold);
    }
}
