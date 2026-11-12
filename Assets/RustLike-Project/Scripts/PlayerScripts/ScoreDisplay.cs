using UnityEngine;
using TMPro; 

public class ScoreDisplay : MonoBehaviour
{
    private TextMeshProUGUI scoreTextElement;

    void Start()
    {
        // 🛠️ AUTOMATIC LINK: Unity finds its own text component instantly on startup
        scoreTextElement = GetComponent<TextMeshProUGUI>();

        // Resets the score to 0 on Play
        EnemyStats.currentScore = 0;   //WHY IS THIS NOT PLAYERSTATS????

        ApplySharpnessSettings();
    }

    void Update()
    {
        // TESTING SHORTCUT: Press T to manually give yourself 10 points
        if (Input.GetKeyDown(KeyCode.T)) 
        {
            EnemyStats.currentScore += 10;
        }

        if (scoreTextElement == null) return;

        int liveScore = EnemyStats.currentScore;

        // Formats "Score : " to your orange hex, and the numbers stay white
        scoreTextElement.text = "<color=#FF7F00>Score : </color>" + liveScore.ToString("D3");
    }

    private void ApplySharpnessSettings()
    {
        if (scoreTextElement != null)
        {
            scoreTextElement.isOrthographic = true;
            scoreTextElement.extraPadding = true;

            if (scoreTextElement.fontSharedMaterial != null)
            {
                scoreTextElement.fontSharedMaterial.EnableKeyword("SHADER_OPTION_SHARPNESS");
                scoreTextElement.fontSharedMaterial.SetFloat("_Sharpness", 1f);
                scoreTextElement.fontSharedMaterial.SetFloat("_OutlineWidth", 0f);
                scoreTextElement.fontSharedMaterial.SetFloat("_GradientScale", 10f);
            }
            scoreTextElement.UpdateMeshPadding();
        }
    }
}
