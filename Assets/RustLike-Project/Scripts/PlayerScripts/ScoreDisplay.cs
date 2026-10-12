using UnityEngine;
using TMPro; 

public class ScoreDisplay : MonoBehaviour
{
    // The slot where we will link our TextMeshPro object
    [SerializeField] private TextMeshProUGUI scoreTextElement;

    void Start()
    {
        // Automatically resets the global score back to 0 whenever you press Play
        EnemyStats.currentScore = 0;
    }

    void Update()
    {
        // TESTING SHORTCUT: Press T to manually give yourself 10 points
        if (Input.GetKeyDown(KeyCode.T)) 
        {
            EnemyStats.currentScore += 10;
        }

        // 1. Read the global score from the EnemyStats script
        int liveScore = EnemyStats.currentScore;

        // 2. Update the text display dynamically on the screen
        // ToString("D3") turns numbers like '10' into '010' for an arcade CRT look!
        scoreTextElement.text = "Score : " + liveScore.ToString("D3");
    }
}
