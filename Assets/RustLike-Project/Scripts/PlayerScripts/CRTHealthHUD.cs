using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProceduralHealthBarHUD : MonoBehaviour
{
    public int maximumHealth = 100;
    public float initialBootDelay = 0.2f;
    public float segmentBootDelay = 0.1f;
    public AudioClip bootSound;
    [Range(0f, 1f)] public float bootVolume = 0.8f;
    public Color orange = new Color32(255, 121, 0, 255);
    public Color inactiveOrange = new Color32(255, 121, 0, 128);
    public Color housingColor = new Color32(25, 25, 27, 255);
    public TMP_FontAsset customFont;

    private const int SegmentCount = 12;
    private readonly Image[] segments = new Image[SegmentCount];
    private int lastHealth = -1;

    private void Start()
    {
        CreateHousing();
        CreateTitle();
        CreateSegments();
        StartCoroutine(BootUp());
    }

    private void Update()
    {
        int health = Mathf.Clamp(PlayerStats.currentPlayerHealth, 0, maximumHealth);
        if (health == lastHealth) return;
        lastHealth = health;

        for (int i = 0; i < SegmentCount; i++)
            segments[i].color = health >= (i + 1) * maximumHealth / SegmentCount ? orange : inactiveOrange;
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

    private void CreateHousing()
    {
        Image image = CreateElement<Image>("Housing", new Vector2(12f, -12f), new Vector2(256f, 80f));
        image.color = new Color(housingColor.r, housingColor.g, housingColor.b, 0.9f);
        image.sprite = CreateSprite(256, 80, (x, y) => Mathf.Min(
            RoundedDistance(x, y, 40f, 66f, 40f, 14f, 6.75f),
            RoundedDistance(x, y, 128f, 30.5f, 128f, 28.5f, 6.75f)));
    }

    private void CreateTitle()
    {
        TextMeshProUGUI text = CreateElement<TextMeshProUGUI>("Health_Title", new Vector2(12f, -12f), new Vector2(80f, 28f));
        if (customFont != null) text.font = customFont;
        text.text = "Health";
        text.color = orange;
        text.fontSize = 18f;
        text.alignment = TextAlignmentOptions.Center;
    }

    private void CreateSegments()
    {
        Sprite sprite = CreateSprite(60, 168, (x, y) => RoundedDistance(x, y, 30f, 84f, 30f, 84f, 22f));

        for (int i = 0; i < SegmentCount; i++)
        {
            Image image = CreateElement<Image>("Health_Segment_" + (i + 1), new Vector2(22f + i * 20f, -40f), new Vector2(15f, 42f));
            image.sprite = sprite;
            image.color = inactiveOrange;
            image.gameObject.SetActive(false);
            segments[i] = image;
        }
    }

    private T CreateElement<T>(string name, Vector2 position, Vector2 size) where T : Graphic
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(T));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        T graphic = obj.GetComponent<T>();
        graphic.raycastTarget = false;
        return graphic;
    }

    private static Sprite CreateSprite(int width, int height, Func<float, float, float> distance)
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

    private static float RoundedDistance(float x, float y, float centerX, float centerY, float halfWidth, float halfHeight, float radius)
    {
        float qx = Mathf.Abs(x - centerX) - (halfWidth - radius);
        float qy = Mathf.Abs(y - centerY) - (halfHeight - radius);
        float outside = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude;
        return outside + Mathf.Min(Mathf.Max(qx, qy), 0) - radius;
    }
}
