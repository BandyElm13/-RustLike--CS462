using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    [SerializeField] private GameObject enemy;

    // 1. REMOVED 'static' so every individual enemy gets its own unique health pool!
    [SerializeField] private int currentEnemyHealth = 100; 

    // 2. Added a global static score variable that any script can read
    public static int currentScore = 0;

    public void takedamage(int amount)
    {
        // Reduce this specific enemy's health
        currentEnemyHealth -= amount;
        
        // Check if they died as a result of taking damage
        CheckEnemyDeath();
    }

    private void CheckEnemyDeath()
    {
        // Checks if health dropped to or below 0
        if(currentEnemyHealth <= 0)
        {
            // Add 10 points to the global team score!
            currentScore += 10;
            Debug.Log("Enemy Defeated! Global Score: " + currentScore);

            // Turn off this enemy object in the game
            enemy.SetActive(false);
        }
    }
}
