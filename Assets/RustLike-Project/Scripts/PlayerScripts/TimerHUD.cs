using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Run timer box at the top-center of the screen, drawn in the same style as the
/// Health/Energy HUD (dark rounded housing with a title tab, orange text).
/// Shows RunTimer as 00:00:00 and drives it each frame.
/// </summary>
public class TimerHUD : MonoBehaviour
{
    [Tooltip("Offset from the top-center of the screen.")]
    public Vector2 position = new Vector2(0f, -12f);
    [Tooltip("Width of the housing.")]
    public float width = 180f;
    [Tooltip("Height of the body under the title tab (Energy uses 21).")]
    public float bodyHeight = 21f;
    public string title = "Time";
    public float timeFontSize = 22f;
    public Color orange = new Color32(255, 121, 0, 255);
    public Color housingColor = new Color32(25, 25, 27, 255);
    public TMP_FontAsset customFont;

    private TextMeshProUGUI timeText;
    private int lastShownSecond = -1;

    private void Start()
    {
        int w = Mathf.RoundToInt(width);
        int h = Mathf.RoundToInt(bodyHeight) + 38;
        float cx = w / 2f;

        Image housing = HealthHUD.CreateElement<Image>(transform, "Timer_Housing", Vector2.zero, new Vector2(w, h));
        RectTransform rect = housing.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;

        housing.color = new Color(housingColor.r, housingColor.g, housingColor.b, 0.9f);
        // Same shape as the Health/Energy housing (title tab + body), with the tab centered.
        housing.sprite = HealthHUD.CreateSprite(w, h, (x, y) => Mathf.Min(
            HealthHUD.RoundedDistance(x, y, cx, h - 14f, 40f, 14f, 6.75f),
            HealthHUD.RoundedDistance(x, y, cx, (h - 19f) / 2f, cx, (h - 23f) / 2f, 6.75f)));

        HealthHUD.CreateText(housing.transform, title, new Vector2(cx - 40f, 0f), new Vector2(80f, 28f), orange, customFont);

        timeText = HealthHUD.CreateText(housing.transform, "Timer", new Vector2(0f, -21f), new Vector2(w, h - 23f), orange, customFont);
        timeText.fontSize = timeFontSize;

        Refresh(true);
    }

    private void Update()
    {
        RunTimer.Tick(Time.deltaTime);
        Refresh(false);
    }

    private void Refresh(bool force)
    {
        if (timeText == null) return;

        int second = Mathf.FloorToInt(RunTimer.Elapsed);
        if (!force && second == lastShownSecond) return;
        lastShownSecond = second;

        // Monospaced digits so the text doesn't wiggle as numbers change.
        timeText.text = "<mspace=0.6em>" + RunTimer.Format(RunTimer.Elapsed) + "</mspace>";
    }
}
