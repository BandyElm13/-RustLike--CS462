using UnityEngine;

/// <summary>
/// Spawns completed gun and sword enemy prefabs with a fixed independent 50/50 roll
/// each spawn (not a guaranteed equal split per wave).
///
/// Inspector: only two enemy slots — assign finished prefabs (root has GunEnemyStats or
/// SwordEnemyStats + EnemyAnimations + Rigidbody/GravityBody/collider), NOT raw FBX/models.
/// Preserves wave size formula, spawn points/interval, wavestart audio, and EnemyDied API.
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
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnInterval = 1f;

    private const float GunPickChance = 0.5f;

    private int currentWave = 0;
    private int enemiesAlive = 0;
    private int enemiesToSpawn = 0;
    private float waveTimer = 0f;
    private float spawnTimer = 0f;
    private bool waitingForNextWave = false;
    private bool spawningEnemies = false;
    private bool spawnConfigValid = false;

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
            Debug.LogError("WaveManager: invalid spawn config — waves will not start.");
            return;
        }

        StartWave();
    }

    private void ValidatePrefabs()
    {
        spawnConfigValid = false;

        if (swordEnemyPrefab == null || gunEnemyPrefab == null)
        {
            Debug.LogError(
                "WaveManager: assign BOTH swordEnemyPrefab and gunEnemyPrefab " +
                "(completed enemy prefabs, not raw models)."
            );
            return;
        }

        if (!PrefabHasExactSubclass(swordEnemyPrefab, typeof(SwordEnemyStats)))
        {
            Debug.LogError(
                "WaveManager: swordEnemyPrefab root must have SwordEnemyStats " +
                "(subclass of EnemyStats). Do not drag a raw mesh/FBX here."
            );
            return;
        }

        if (!PrefabHasExactSubclass(gunEnemyPrefab, typeof(GunEnemyStats)))
        {
            Debug.LogError(
                "WaveManager: gunEnemyPrefab root must have GunEnemyStats " +
                "(subclass of EnemyStats). Do not drag a raw mesh/FBX here."
            );
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("WaveManager: No spawn points assigned.");
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
            if (spawnTimer <= 0f)
            {
                bool spawned = SpawnEnemy();
                enemiesToSpawn--;
                spawnTimer = spawnInterval;

                if (!spawned)
                    Debug.LogError("WaveManager: spawn failed — enemiesAlive not incremented.");

                if (enemiesToSpawn <= 0)
                {
                    spawningEnemies = false;
                    if (enemiesAlive <= 0)
                        WaveCompleted();
                }
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
        Debug.Log("Starting Wave " + currentWave + " with " + enemiesToSpawn + " enemies.");
    }

    private bool SpawnEnemy()
    {
        GameObject prefab = ChooseEnemyPrefab();
        if (prefab == null) return false;
        if (spawnPoints == null || spawnPoints.Length == 0) return false;

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject instance = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        if (instance == null) return false;

        enemiesAlive++;
        Debug.Log("Enemy spawned (" + prefab.name + "). Enemies alive: " + enemiesAlive);
        return true;
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
        enemiesAlive--;
        Debug.Log("Enemy died. Enemies alive: " + enemiesAlive);

        if (enemiesAlive <= 0 && !spawningEnemies)
            WaveCompleted();
    }

    private void WaveCompleted()
    {
        Debug.Log("Wave " + currentWave + " completed!");
        waitingForNextWave = true;
        waveTimer = timeBetweenWaves;
    }
}
