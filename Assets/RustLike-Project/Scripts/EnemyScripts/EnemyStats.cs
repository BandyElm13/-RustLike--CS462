using UnityEngine;

public class EnemyStats : MonoBehaviour
{

    [SerializeField] private GameObject enemy;

    private static int currentEnemyHealth = 100;
    void Start()
    {
        
    }

    public void takedamage(int amount)
    {
        currentEnemyHealth -= amount;
    }

    private void enemyDeath()
    {
        if(currentEnemyHealth == 0)
        {
            enemy.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
