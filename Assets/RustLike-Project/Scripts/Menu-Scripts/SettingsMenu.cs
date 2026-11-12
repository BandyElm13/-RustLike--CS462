using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main Menu settings screen. Lives on its own always-active object ("SettingsMenu") in Main Menu:
///   Options button → hides the main menu canvas and shows SettingsCanvas.
///   Back button (or Esc) → returns to the main menu.
/// On first open it builds the content inside SettingsCanvas, centered on the CRT screen:
/// Master / Music / SFX volume sliders (saved via GameAudioSettings) and a read-only controls list.
/// Text style (font, color, material) is copied from Label Template (SettingsText1).
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    [Header("Screens")]
    [SerializeField] private GameObject mainMenuCanvas;
    [SerializeField] private GameObject settingsCanvas;
    [SerializeField] private Button openButton;   // "Options"
    [SerializeField] private Button backButton;   // "Back" inside SettingsCanvas

    [Header("Look")]
    [Tooltip("Existing text whose font/color/material the generated text copies (hidden at runtime).")]
    [SerializeField] private TMP_Text labelTemplate;
    [SerializeField] private TMP_FontAsset fallbackFont;
    [SerializeField] private Color orange = new Color(1f, 0.5372549f, 0f, 1f);
    [SerializeField] private Color dimOrange = new Color(1f, 0.5372549f, 0f, 0.35f);
    [SerializeField] private Color trackColor = new Color32(25, 25, 27, 230);
    [Tooltip("Center of the content area (the big CRT screen in mainmenubase.png sits ~200px right of center).")]
    [SerializeField] private Vector2 contentCenter = new Vector2(200f, 0f);

    [Header("Sound")]
    [SerializeField] private AudioClip clickSound;
    [Tooltip("CRT power-on sound: played when the Main Menu loads and when switching menu <-> settings.")]
    [SerializeField] private AudioClip tvOnSound;
    [SerializeField, Range(0f, 1f)] private float tvOnVolume = 0.8f;
    [SerializeField] private AudioSource audioSource;

    private static readonly string[,] Controls =
    {
        { "W A S D",      "Move" },
        { "Mouse",        "Look" },
        { "Left Shift",   "Sprint" },
        { "Space",        "Jump" },
        { "Left Click",   "Shoot (hold for full auto)" },
        { "Right Click",  "Aim down sights (hold)" },
    };

    private bool built;
    private Sprite roundedSprite;

    private void Awake()
    {
        if (openButton != null) openButton.onClick.AddListener(Open);
        if (backButton != null) backButton.onClick.AddListener(Close);
        if (settingsCanvas != null) settingsCanvas.SetActive(false);
    }

    private void Start()
    {
        TvOn(); // Main Menu screen "turns on"
    }

    private void Update()
    {
        if (settingsCanvas != null && settingsCanvas.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    public void Open()
    {
        if (settingsCanvas == null) return;
        if (!built) Build();
        Click();
        TvOn();
        settingsCanvas.SetActive(true);
        if (mainMenuCanvas != null) mainMenuCanvas.SetActive(false);
    }

    public void Close()
    {
        Click();
        TvOn();
        if (settingsCanvas != null) settingsCanvas.SetActive(false);
        if (mainMenuCanvas != null) mainMenuCanvas.SetActive(true);
    }

    private void TvOn()
    {
        if (tvOnSound != null && audioSource != null)
            audioSource.PlayOneShot(tvOnSound, tvOnVolume * GameAudioSettings.Sfx);
    }

    private void Click()
    {
        if (clickSound != null && audioSource != null)
            audioSource.PlayOneShot(clickSound, GameAudioSettings.Sfx);
    }

    // ---------------------------------------------------------------- building

    private void Build()
    {
        built = true;
        roundedSprite = CreateRoundedSprite(32, 9f);

        RectTransform root = new GameObject("Settings_Content", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(settingsCanvas.transform, false);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(1200f, 900f);
        root.anchoredPosition = contentCenter;

        if (labelTemplate != null) labelTemplate.gameObject.SetActive(false);

        const float left = -520f;
        float y = 360f;

        Text(root, "Audio", new Vector2(left, y), new Vector2(600f, 60f), 52f, TextAlignmentOptions.Left, orange);
        y -= 95f;
        SliderRow(root, "Master Volume", GameAudioSettings.Master, GameAudioSettings.SetMaster, left, y); y -= 80f;
        SliderRow(root, "Music Volume", GameAudioSettings.Music, GameAudioSettings.SetMusic, left, y); y -= 80f;
        SliderRow(root, "SFX Volume", GameAudioSettings.Sfx, GameAudioSettings.SetSfx, left, y); y -= 115f;

        Text(root, "Controls", new Vector2(left, y), new Vector2(600f, 60f), 52f, TextAlignmentOptions.Left, orange);
        y -= 80f;
        for (int i = 0; i < Controls.GetLength(0); i++)
        {
            Text(root, Controls[i, 0], new Vector2(left, y), new Vector2(380f, 50f), 34f, TextAlignmentOptions.Left, orange);
            Text(root, Controls[i, 1], new Vector2(left + 400f, y), new Vector2(640f, 50f), 34f, TextAlignmentOptions.Left,
                 new Color(orange.r, orange.g, orange.b, 0.8f));
            y -= 55f;
        }
    }

    private void SliderRow(RectTransform parent, string label, float value, System.Action<float> onChange, float left, float y)
    {
        Text(parent, label, new Vector2(left, y), new Vector2(380f, 50f), 36f, TextAlignmentOptions.Left, orange);
        TextMeshProUGUI percent = Text(parent, "", new Vector2(left + 960f, y), new Vector2(140f, 50f), 36f, TextAlignmentOptions.Right, orange);

        Slider slider = CreateSlider(parent, new Vector2(left + 400f, y), new Vector2(520f, 26f));
        slider.SetValueWithoutNotify(value);
        percent.text = Mathf.RoundToInt(value * 100f) + "%";
        slider.onValueChanged.AddListener(v =>
        {
            onChange(v);
            percent.text = Mathf.RoundToInt(v * 100f) + "%";
        });
    }

    private TextMeshProUGUI Text(RectTransform parent, string content, Vector2 leftCenter, Vector2 size, float fontSize,
                                 TextAlignmentOptions align, Color color)
    {
        RectTransform rect = Rect(parent, "Text_" + content, leftCenter, size, new Vector2(0f, 0.5f));
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (labelTemplate != null)
        {
            text.font = labelTemplate.font;
            text.fontSharedMaterial = labelTemplate.fontSharedMaterial;
        }
        else if (fallbackFont != null) text.font = fallbackFont;
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = align;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    private Slider CreateSlider(RectTransform parent, Vector2 leftCenter, Vector2 size)
    {
        RectTransform root = Rect(parent, "Slider", leftCenter, size, new Vector2(0f, 0.5f));
        Slider slider = root.gameObject.AddComponent<Slider>();

        // Track: dark housing like the HUD, with a dim orange edge.
        Image border = Stretch(root, "Border", -3f).gameObject.AddComponent<Image>();
        border.sprite = roundedSprite; border.type = Image.Type.Sliced; border.color = dimOrange;
        Image track = Stretch(root, "Background", 0f).gameObject.AddComponent<Image>();
        track.sprite = roundedSprite; track.type = Image.Type.Sliced; track.color = trackColor;

        RectTransform fillArea = Stretch(root, "Fill Area", -5f);
        RectTransform fill = Stretch(fillArea, "Fill", 0f);
        Image fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.sprite = roundedSprite; fillImage.type = Image.Type.Sliced; fillImage.color = orange;

        RectTransform handleArea = Stretch(root, "Handle Slide Area", 0f);
        handleArea.offsetMin = new Vector2(10f, 0f); handleArea.offsetMax = new Vector2(-10f, 0f);
        RectTransform handle = Stretch(handleArea, "Handle", 0f);
        handle.sizeDelta = new Vector2(20f, 18f); // width; height = track + 18
        Image handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.sprite = roundedSprite; handleImage.type = Image.Type.Sliced; handleImage.color = Color.white;

        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f; slider.maxValue = 1f;

        ColorBlock colors = slider.colors;
        colors.normalColor = orange;
        colors.highlightedColor = Color.Lerp(orange, Color.white, 0.35f);
        colors.pressedColor = Color.Lerp(orange, Color.white, 0.6f);
        colors.selectedColor = Color.Lerp(orange, Color.white, 0.2f);
        colors.colorMultiplier = 1f;
        slider.colors = colors;
        Navigation nav = slider.navigation; nav.mode = Navigation.Mode.None; slider.navigation = nav;

        return slider;
    }

    private static RectTransform Rect(RectTransform parent, string name, Vector2 position, Vector2 size, Vector2 pivot)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    // Child filling its parent, inset by 'inset' (negative = grows outward).
    private static RectTransform Stretch(RectTransform parent, string name, float inset)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
        return rect;
    }

    // White rounded square, 9-sliced so it stretches cleanly (tinted per use).
    private static Sprite CreateRoundedSprite(int size, float radius)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        float half = size / 2f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = HealthHUD.RoundedDistance(x + 0.5f, y + 0.5f, half, half, half, half, radius);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d)));
        }
        tex.Apply();
        float border = radius + 1f;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                             SpriteMeshType.FullRect, new Vector4(border, border, border, border));
    }
}
