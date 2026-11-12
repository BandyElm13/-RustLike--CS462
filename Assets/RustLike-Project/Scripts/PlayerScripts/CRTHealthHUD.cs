using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProceduralHealthBarHUD : MonoBehaviour
{
    [Header("Health")]
    public int maximumHealth = 100;

    [Header("Boot Animation")]
    [Tooltip("Delay in seconds before the first bar starts appearing.")]
    public float initialBootDelay = 0.2f;
    [Tooltip("Delay in seconds between each individual health segment popping in.")]
    public float segmentBootDelay = 0.1f;

    [Header("Boot Audio")]
    [Tooltip("Drag your MP3/WAV sound effect here.")]
    public AudioClip bootSound;
    [Range(0f, 1f)]
    public float bootVolume = 0.8f;
    [Tooltip("Check this if the MP3 is a short blip that should play for EACH bar. Uncheck for one single continuous sound.")]
    public bool playSoundPerSegment = true;
    [Tooltip("Slightly raises the pitch with each bar for a charging effect (only applies if Play Sound Per Segment is checked).")]
    public bool pitchShiftUp = true;

    [Header("Colors")]
    public Color orange = new Color32(255, 121, 0, 255);
    public Color inactiveOrange = new Color32(255, 121, 0, 128); 
    public Color housingColor = new Color32(25, 25, 27, 255);
    public Color housingEdge = new Color32(55, 55, 57, 255);

    [Header("Font Styling Asset")]
    [Tooltip("Drag your LazenbyCompLiquid SDF Font Asset here from your project folders.")]
    public TMP_FontAsset customFont;

    private const int SEGMENT_COUNT = 12;
    private Image[] segments;
    private Sprite segmentSprite;
    private int lastHealth = -1;
    private bool isBootingUp = true;
    private AudioSource audioSource;

    private void Start()
    {
        SetupAudio();
        BuildHUD();
        StartCoroutine(BootUpSequence());
    }

    private void SetupAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    private void Update()
    {
        if (isBootingUp) return;

        int health = Mathf.Clamp(
            PlayerStats.currentPlayerHealth, 0, maximumHealth);

        if (health != lastHealth)
        {
            UpdateSegments(health);
            lastHealth = health;
        }
    }

    private IEnumerator BootUpSequence()
    {
        isBootingUp = true;

        if (initialBootDelay > 0f)
        {
            yield return new WaitForSeconds(initialBootDelay);
        }

        if (bootSound != null && !playSoundPerSegment)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(bootSound, bootVolume);
        }

        for (int i = 0; i < SEGMENT_COUNT; i++)
        {
            int currentHealth = Mathf.Clamp(PlayerStats.currentPlayerHealth, 0, maximumHealth);
            int threshold = (i + 1) * maximumHealth / SEGMENT_COUNT;

            segments[i].color = currentHealth >= threshold ? orange : inactiveOrange;
            segments[i].gameObject.SetActive(true);

            if (bootSound != null && playSoundPerSegment)
            {
                if (pitchShiftUp)
                {
                    audioSource.pitch = Mathf.Lerp(0.9f, 1.3f, (float)i / (SEGMENT_COUNT - 1));
                }
                else
                {
                    audioSource.pitch = 1f;
                }

                audioSource.PlayOneShot(bootSound, bootVolume);
            }

            yield return new WaitForSeconds(segmentBootDelay);
        }

        audioSource.pitch = 1f;
        lastHealth = Mathf.Clamp(PlayerStats.currentPlayerHealth, 0, maximumHealth);
        isBootingUp = false;
    }

    private void BuildHUD()
    {
        Transform previous = transform.Find("GeneratedHealthBar");

        if (previous != null)
            Destroy(previous.gameObject);

        GameObject root = new GameObject(
            "GeneratedHealthBar",
            typeof(RectTransform));

        root.transform.SetParent(transform, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0, 1);
        rootRect.anchorMax = new Vector2(0, 1);
        rootRect.pivot = new Vector2(0, 1);
        
        rootRect.anchoredPosition = new Vector2(12f, -12f);
        rootRect.sizeDelta = new Vector2(256f, 80f); 

        CreateHousing(root.transform);
        CreateTitle(root.transform);
        CreateSegments(root.transform);
    }

    private void CreateHousing(Transform parent)
    {
        Texture2D texture = new Texture2D(1024, 320);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < 320; y++)
        {
            for (int x = 0; x < 1024; x++)
            {
                float tab = RoundedDistance(
                    x, y, 160f, 56f, 160f, 56f, 27f);

                float body = RoundedDistance(
                    x, y, 512f, 198f, 512f, 114f, 27f);

                float distance = Mathf.Min(tab, body);
                float outerAlpha = Mathf.Clamp01(0.5f - distance);

                if (outerAlpha <= 0)
                {
                    texture.SetPixel(x, 319 - y, Color.clear);
                    continue;
                }

                Color color = Color.Lerp(housingColor, housingColor, 0f);
                color.a = outerAlpha * 0.90f;

                texture.SetPixel(x, 319 - y, color);
            }
        }

        texture.Apply();

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, 1024, 320),
            new Vector2(0, 1),
            1);

        GameObject housing = new GameObject(
            "Housing", typeof(RectTransform), typeof(Image));

        housing.transform.SetParent(parent, false);

        RectTransform rect = housing.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.sizeDelta = new Vector2(256f, 80f);

        Image image = housing.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
    }

    private void CreateTitle(Transform parent)
    {
        GameObject titleObject = new GameObject(
            "Health_Title",
            typeof(RectTransform),
            typeof(TextMeshProUGUI));

        titleObject.transform.SetParent(parent, false);

        RectTransform rect = titleObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        
        rect.anchoredPosition = new Vector2(0f, -0.25f); 
        rect.sizeDelta = new Vector2(80f, 28f); 

        TextMeshProUGUI text = titleObject.GetComponent<TextMeshProUGUI>();

        if (customFont != null)
        {
            text.font = customFont;
        }

        text.text = "Health";
        text.color = orange;
        text.fontSize = 18f; 
        text.alignment = TextAlignmentOptions.Center;
        text.verticalAlignment = VerticalAlignmentOptions.Middle;
        text.raycastTarget = false;
        
        text.margin = new Vector4(0, 0, 0, 0);
        text.extraPadding = true;
        text.isOrthographic = true; 
        
        if (text.fontSharedMaterial != null)
        {
            text.fontSharedMaterial.EnableKeyword("SHADER_OPTION_SHARPNESS");
            text.fontSharedMaterial.SetFloat("_Sharpness", 1f); 
            text.fontSharedMaterial.SetFloat("_OutlineWidth", 0f);
            text.fontSharedMaterial.SetFloat("_GradientScale", 10f); 
        }
        text.UpdateMeshPadding();
    }

    private void CreateSegments(Transform parent)
    {
        segments = new Image[SEGMENT_COUNT];

        if (segmentSprite == null)
        {
            segmentSprite = CreateHighResPillarTemplate(240, 672, 88f);
        }

        for (int i = 0; i < SEGMENT_COUNT; i++)
        {
            GameObject bar = new GameObject(
                "Health_Segment_" + (i + 1),
                typeof(RectTransform),
                typeof(Image));

            bar.transform.SetParent(parent, false);

            RectTransform rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);

            rect.anchoredPosition = new Vector2(
                10f + i * 20f, 
                -28f); 

            rect.sizeDelta = new Vector2(15f, 42f);

            Image image = bar.GetComponent<Image>();
            image.sprite = segmentSprite;
            image.type = Image.Type.Simple;
            image.color = orange;
            image.raycastTarget = false;

            bar.SetActive(false);

            segments[i] = image;
        }
    }

    private void UpdateSegments(int health)
    {
        for (int i = 0; i < SEGMENT_COUNT; i++)
        {
            int threshold = (i + 1) * maximumHealth / SEGMENT_COUNT;
            segments[i].color = health >= threshold ? orange : inactiveOrange;
        }
    }

    private Sprite CreateHighResPillarTemplate(int width, int height, float radius)
    {
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Bilinear; 
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float distance = RoundedDistance(
                    x, y,
                    width / 2f,
                    height / 2f,
                    width / 2f,
                    height / 2f,
                    radius);

                float alpha = Mathf.Clamp01(0.5f - (distance * 2.5f));
                texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }

        texture.Apply();

return Sprite.Create(
texture,
new Rect(0, 0, width, height),
new Vector2(0.5f, 0.5f),
1);
}
private float RoundedDistance(
float x, float y,
float centerX, float centerY,
float halfWidth, float halfHeight,
float radius)
{
float qx = Mathf.Abs(x - centerX) - (halfWidth - radius);
float qy = Mathf.Abs(y - centerY) - (halfHeight - radius);
float outsideX = Mathf.Max(qx, 0);
float outsideY = Mathf.Max(qy, 0);
float outsideDist = Mathf.Sqrt(outsideX * outsideX + outsideY * outsideY);
float insideDist = Mathf.Min(Mathf.Max(qx, qy), 0);
return outsideDist + insideDist - radius;
}
}