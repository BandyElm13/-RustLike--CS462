using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Wave Settings")]
    [SerializeField] private int startingEnemies = 3;
    [SerializeField] private int enemiesAddedPerWave = 1;
    [SerializeField] private float timeBetweenWaves = 10f;

    [Header("Spawning")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnInterval = 1f;

    private int currentWave = 0;
    private int enemiesAlive = 0;
    private int enemiesToSpawn = 0;

    private float waveTimer = 0f;
    private float spawnTimer = 0f;

    private bool waitingForNextWave = false;
    private bool spawningEnemies = false;

    private void Start()
    {
        StartWave();
    }

    private void Update()
    {
        // Check if we are waiting for the next wave
        if (waitingForNextWave)
        {
            waveTimer -= Time.deltaTime;

            if (waveTimer <= 0f)
            {
                StartWave();
            }

            return;
        }

        // Check if we are currently spawning enemies
        if (spawningEnemies)
        {
            spawnTimer -= Time.deltaTime;

            if (spawnTimer <= 0f)
            {
                SpawnEnemy();

                enemiesToSpawn--;

                spawnTimer = spawnInterval;

                // All enemies for this wave have been spawned
                if (enemiesToSpawn <= 0)
                {
                    spawningEnemies = false;
                }
            }
        }
    }

    private void StartWave()
    {
        currentWave++;

        // Calculate how many enemies this wave should have
        enemiesToSpawn =
            startingEnemies +
            ((currentWave - 1) * enemiesAddedPerWave);

        spawningEnemies = true;
        waitingForNextWave = false;

        // Make the first enemy spawn immediately
        spawnTimer = 0f;

        Debug.Log(
            "Starting Wave " +
            currentWave +
            " with " +
            enemiesToSpawn +
            " enemies."
        );
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("WaveManager: Enemy Prefab has not been assigned!");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("WaveManager: No spawn points have been assigned!");
            return;
        }

        // Choose a random spawn point
        Transform spawnPoint =
            spawnPoints[Random.Range(0, spawnPoints.Length)];

        // Create the enemy
        Instantiate(
            enemyPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        enemiesAlive++;

        Debug.Log(
            "Enemy spawned. Enemies alive: " +
            enemiesAlive
        );
    }

    public void EnemyDied()
    {
        enemiesAlive--;

        Debug.Log(
            "Enemy died. Enemies alive: " +
            enemiesAlive
        );

        // Only finish the wave after:
        // 1. All enemies have been spawned
        // 2. All enemies have died
        if (enemiesAlive <= 0 && !spawningEnemies)
        {
            WaveCompleted();
        }
    }

    private void WaveCompleted()
    {
        Debug.Log(
            "Wave " +
            currentWave +
            " completed!"
        );

        // Start the break between waves
        waitingForNextWave = true;

        waveTimer = timeBetweenWaves;
    }
}