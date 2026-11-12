using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreHUD : MonoBehaviour
{
    public Vector2 position = new Vector2(-12f, -12f);
    public Color orange = new Color32(255, 121, 0, 255);
    public Color housingColor = new Color32(25, 25, 27, 255);
    public TMP_FontAsset customFont;

    private TextMeshProUGUI text;
    private int lastScore = -1;

    private void Start()
    {
        Vector2 size = new Vector2(128f, 28f);
        Image housing = HealthHUD.CreateElement<Image>(transform, "Housing", position, size);
        housing.color = new Color(housingColor.r, housingColor.g, housingColor.b, 0.9f);
        housing.sprite = HealthHUD.CreateSprite(128, 28, (x, y) => HealthHUD.RoundedDistance(x, y, 64f, 14f, 64f, 14f, 6.75f));
        housing.rectTransform.anchorMin = housing.rectTransform.anchorMax = housing.rectTransform.pivot = Vector2.one;
        text = HealthHUD.CreateText(housing.transform, "Score", Vector2.zero, size, orange, customFont);
    }

    private void Update()
    {
        if (EnemyStats.currentScore == lastScore) return;
        lastScore = EnemyStats.currentScore;
        text.text = "Score " + lastScore.ToString("D3");
    }
}
