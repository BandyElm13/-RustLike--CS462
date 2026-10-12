using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerStats : MonoBehaviour
{
    public static int currentPlayerHealth = 100;

    void Start()
    {
        // Automatically resets player health to full (100) whenever you press Start/Play
        currentPlayerHealth = 100;
        Debug.Log("Player Health Initialized = " + currentPlayerHealth);
    }

    public void takeDamage(int amount)
    {
        currentPlayerHealth -= amount;
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
