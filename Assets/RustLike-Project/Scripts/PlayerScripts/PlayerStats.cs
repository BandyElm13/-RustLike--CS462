using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerStats : MonoBehaviour
{
    public static int currentPlayerHealth = 100;

    void Start()
    {
        Debug.Log("Player Health = " + currentPlayerHealth);
    }
    public void takeDamage(int amount)
    {
        currentPlayerHealth -= amount;
    }
    
    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.E))
        {
            takeDamage(20);
            Debug.Log("new damage" + currentPlayerHealth);
        }
    }
}
