
using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;

    // Global team score that any script can read (shown by ScoreDisplay)
    public static int currentScore = 0;

    // Per-enemy health so every enemy gets its own health pool
    private int currentEnemyHealth;
    private WaveManager waveManager;
    private bool isDead;

    private void Start()
    {
        currentEnemyHealth = maxHealth;

        waveManager = FindAnyObjectByType<WaveManager>();
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        // Reduce this specific enemy's health
        currentEnemyHealth -= amount;

        if (currentEnemyHealth <= 0)
        {
            EnemyDeath();
        }
    }

    private void EnemyDeath()
    {
        // Guard so multiple hits in one frame don't count the kill twice
        isDead = true;

        // Add 10 points to the global team score!
        currentScore += 10;
        Debug.Log("Enemy Defeated! Global Score: " + currentScore);

        if (waveManager != null)
        {
            waveManager.EnemyDied();
        }

        Destroy(gameObject);
    }
}
