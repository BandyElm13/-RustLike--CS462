using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Score box at the top-right of the screen, in the same style as the Health/Energy/Time HUD
/// (dark rounded housing with a title tab, orange text). Shows EnemyStats.currentScore.
/// </summary>
public class ScoreHUD : MonoBehaviour
{
    [Tooltip("Offset from the top-right corner of the screen.")]
    public Vector2 position = new Vector2(-12f, -12f);
    [Tooltip("Width of the housing.")]
    public float width = 180f;
    [Tooltip("Height of the body under the title tab (Energy uses 21).")]
    public float bodyHeight = 21f;
    public string title = "Score";
    public float scoreFontSize = 22f;
    [Tooltip("Digits shown, zero-padded (3 = 007).")]
    public int digits = 3;
    public Color orange = new Color32(255, 121, 0, 255);
    public Color housingColor = new Color32(25, 25, 27, 255);
    public TMP_FontAsset customFont;

    private TextMeshProUGUI scoreText;
    private int lastScore = -1;

    private void Start()
    {
        int w = Mathf.RoundToInt(width);
        int h = Mathf.RoundToInt(bodyHeight) + 38;
        float tabX = w - 40f; // title tab on the right, mirroring Health's tab on the left

        Image housing = HealthHUD.CreateElement<Image>(transform, "Score_Housing", Vector2.zero, new Vector2(w, h));
        RectTransform rect = housing.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
        rect.anchoredPosition = position;

        housing.color = new Color(housingColor.r, housingColor.g, housingColor.b, 0.9f);
        housing.sprite = HealthHUD.CreateSprite(w, h, (x, y) => Mathf.Min(
            HealthHUD.RoundedDistance(x, y, tabX, h - 14f, 40f, 14f, 6.75f),
            HealthHUD.RoundedDistance(x, y, w / 2f, (h - 19f) / 2f, w / 2f, (h - 23f) / 2f, 6.75f)));

        HealthHUD.CreateText(housing.transform, title, new Vector2(tabX - 40f, 0f), new Vector2(80f, 28f), orange, customFont);

        scoreText = HealthHUD.CreateText(housing.transform, "Score", new Vector2(0f, -21f), new Vector2(w, h - 23f), orange, customFont);
        scoreText.fontSize = scoreFontSize;
    }

    private void Update()
    {
        if (scoreText == null || EnemyStats.currentScore == lastScore) return;
        lastScore = EnemyStats.currentScore;
        // Monospaced digits so the text doesn't wiggle as numbers change.
        scoreText.text = "<mspace=0.6em>" + lastScore.ToString("D" + Mathf.Max(1, digits)) + "</mspace>";
    }
}
