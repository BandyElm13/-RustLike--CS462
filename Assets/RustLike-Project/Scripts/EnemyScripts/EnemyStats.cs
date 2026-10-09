using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;

    private int currentEnemyHealth;
    private WaveManager waveManager;

    private void Start()
    {
        currentEnemyHealth = maxHealth;

        waveManager = FindAnyObjectByType<WaveManager>();
    }

    public void TakeDamage(int amount)
    {
        currentEnemyHealth -= amount;

        if (currentEnemyHealth <= 0)
        {
            EnemyDeath();
        }
    }

    private void EnemyDeath()
    {
        if (waveManager != null)
        {
            waveManager.EnemyDied();
        }

        Destroy(gameObject);
    }
}