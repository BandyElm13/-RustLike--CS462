using UnityEngine;
using UnityEngine.UI; // Allows us to control the Image colors

public class CRTHealthHUD : MonoBehaviour
{
    [Header("UI Reference Array")]
    [Tooltip("Drag your 10 health segments into this list from left to right.")]
    public Image[] healthSegments;

    [Header("CRT Color Styling")]
    public Color brightPhosphorGreen = new Color(0.2f, 1f, 0.6f, 1f);
    public Color darkDimGreen = new Color(0.05f, 0.15f, 0.08f, 0.4f); // Transparent dark green

    void Update()
    {
        // 1. Read the global health value from your team's script
        int currentHP = PlayerStats.currentPlayerHealth;

        // 2. Loop through all 10 bars to decide their color state
        for (int i = 0; i < healthSegments.Length; i++)
        {
            // Each segment index represents 10 health points (e.g. index 0 = 10HP, index 1 = 20HP...)
            int segmentHealthValue = (i + 1) * 10;

            if (currentHP >= segmentHealthValue)
            {
                // Health is active -> turn bar bright phosphor green
                healthSegments[i].color = brightPhosphorGreen;
            }
            else
            {
                // Health is lost -> dim the bar down to a dark CRT shadow
                healthSegments[i].color = darkDimGreen;
            }
        }
    }
}
