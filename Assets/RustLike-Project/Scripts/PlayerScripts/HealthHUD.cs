using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;

public class HealthHUD : MonoBehaviour
{
    [FormerlySerializedAs("maximumHealth")] public float maximum = 100f;
    public Vector2 position = new Vector2(12f, -12f);
    public float segmentHeight = 42f;
    public float initialBootDelay = 0.2f;
    public float segmentBootDelay = 0.1f;
    public AudioClip bootSound;
    [Range(0f, 1f)] public float bootVolume = 0.8f;
    public Color orange = new Color32(255, 121, 0, 255);
    public Color inactiveOrange = new Color32(255, 121, 0, 128);
    public Color housingColor = new Color32(25, 25, 27, 255);
    public TMP_FontAsset customFont;

    protected virtual int SegmentCount => 12;
    private Image[] segments;
    private int lastLit = -1;

    protected virtual string Title => "Health";
    protected virtual float Value => PlayerStats.currentPlayerHealth;

    private void Start()
    {
        int height = Mathf.RoundToInt(segmentHeight) + 38;
        Image housing = CreateElement<Image>(transform, "Housing", position, new Vector2(256f, height));
        housing.color = new Color(housingColor.r, housingColor.g, housingColor.b, 0.9f);
        housing.sprite = CreateSprite(256, height, (x, y) => Mathf.Min(
            RoundedDistance(x, y, 40f, height - 14f, 40f, 14f, 6.75f),
            RoundedDistance(x, y, 128f, (height - 19f) / 2f, 128f, (height - 23f) / 2f, 6.75f)));

        CreateText(transform, Title, position, new Vector2(80f, 28f), orange, customFont);

        int spriteHeight = Mathf.RoundToInt(segmentHeight * 4f);
        Sprite sprite = CreateSprite(60, spriteHeight, (x, y) => RoundedDistance(x, y, 30f, spriteHeight / 2f, 30f, spriteHeight / 2f, 22f));
        float spacing = 240f / SegmentCount;
        segments = new Image[SegmentCount];
        for (int i = 0; i < SegmentCount; i++)
        {
            segments[i] = CreateElement<Image>(transform, Title + "_Segment_" + (i + 1), position + new Vector2(10f + i * spacing, -28f), new Vector2(spacing * 0.75f, segmentHeight));
            segments[i].sprite = sprite;
            segments[i].gameObject.SetActive(false);
        }

        StartCoroutine(BootUp());
    }

    private void Update()
    {
        int lit = Mathf.FloorToInt(Mathf.Clamp01(Value / maximum) * SegmentCount);
        if (lit == lastLit) return;
        lastLit = lit;

        for (int i = 0; i < SegmentCount; i++)
            segments[i].color = i < lit ? orange : inactiveOrange;
    }

    private IEnumerator BootUp()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        yield return new WaitForSeconds(initialBootDelay);

        foreach (Image segment in segments)
        {
            segment.gameObject.SetActive(true);
            if (bootSound != null) audioSource.PlayOneShot(bootSound, bootVolume);
            yield return new WaitForSeconds(segmentBootDelay);
        }
    }

    public static T CreateElement<T>(Transform parent, string name, Vector2 position, Vector2 size) where T : Graphic
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(T));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        T graphic = obj.GetComponent<T>();
        graphic.raycastTarget = false;
        return graphic;
    }

    public static TextMeshProUGUI CreateText(Transform parent, string content, Vector2 position, Vector2 size, Color color, TMP_FontAsset font)
    {
        TextMeshProUGUI text = CreateElement<TextMeshProUGUI>(parent, content + "_Text", position, size);
        if (font != null) text.font = font;
        text.text = content;
        text.color = color;
        text.fontSize = 18f;
        text.alignment = TextAlignmentOptions.Center;
        return text;
    }

    public static Sprite CreateSprite(int width, int height, Func<float, float, float> distance)
    {
        Texture2D texture = new Texture2D(width, height) { wrapMode = TextureWrapMode.Clamp };
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                pixels[y * width + x] = new Color(1, 1, 1, Mathf.Clamp01(0.5f - distance(x + 0.5f, y + 0.5f)));

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 1);
    }

    public static float RoundedDistance(float x, float y, float centerX, float centerY, float halfWidth, float halfHeight, float radius)
    {
        float qx = Mathf.Abs(x - centerX) - (halfWidth - radius);
        float qy = Mathf.Abs(y - centerY) - (halfHeight - radius);
        float outside = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude;
        return outside + Mathf.Min(Mathf.Max(qx, qy), 0) - radius;
    }
}
