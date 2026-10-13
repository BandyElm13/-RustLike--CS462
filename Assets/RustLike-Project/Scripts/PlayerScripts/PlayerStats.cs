using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerStats : MonoBehaviour
{
    public static int currentPlayerHealth = 100;

    // Must match a scene name added to File > Build Profiles (Scene List)
    [SerializeField] private string gameOverScene = "Main Menu";

    private bool isDead;

    void Start()
    {
        // Automatically resets player health to full (100) whenever you press Start/Play
        currentPlayerHealth = 100;
        isDead = false;
        Debug.Log("Player Health Initialized = " + currentPlayerHealth);
    }

    public void takeDamage(int amount)
    {
        if (isDead) return;

        currentPlayerHealth -= amount;
        if (currentPlayerHealth < 0) currentPlayerHealth = 0;

        if (currentPlayerHealth <= 0)
        {
            PlayerDeath();
        }
    }

    private void PlayerDeath()
    {
        isDead = true;
        Debug.Log("Player died! Returning to " + gameOverScene);

        // FirstPersonController locks the cursor; free it so the menu buttons can be clicked
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(gameOverScene);
    }

    void Update()
    {
        // Press E to take 10 damage (1 segment)
        if(Input.GetKeyDown(KeyCode.E))
        {
            takeDamage(10);
            Debug.Log("New damage: " + currentPlayerHealth);
        }

        // Press R to add 10 health back (1 segment)
        if(Input.GetKeyDown(KeyCode.R))
        {
            if (currentPlayerHealth < 100)
            {
                currentPlayerHealth += 10;
                if (currentPlayerHealth > 100) currentPlayerHealth = 100;
                Debug.Log("Healed! Current Health: " + currentPlayerHealth);
            }
        }
    }
}
