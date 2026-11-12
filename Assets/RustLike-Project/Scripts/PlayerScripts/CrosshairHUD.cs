using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tiny dot crosshair at the exact screen center (where PlayerShooting's hitscan goes).
/// Drawn in code with a soft dark outline so it reads on bright and dark backgrounds.
/// </summary>
public class CrosshairHUD : MonoBehaviour
{
    [Tooltip("Dot diameter in reference pixels (the canvas scales with screen size).")]
    public float size = 6f;
    public Color color = new Color(1f, 1f, 1f, 0.9f);
    [Tooltip("Thin dark ring around the dot for contrast. Alpha 0 = none.")]
    public Color outlineColor = new Color(0f, 0f, 0f, 0.6f);

    private void Start()
    {
        const int res = 32;
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        float r = res / 2f;
        float inner = r * 0.62f; // dot radius; the rest is outline

        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
            Color c = Color.clear;
            if (d <= r)
            {
                float dotEdge = Mathf.Clamp01(inner + 0.75f - d);   // anti-aliased dot edge
                float ringEdge = Mathf.Clamp01(r - d);              // anti-aliased outer edge
                c = Color.Lerp(new Color(outlineColor.r, outlineColor.g, outlineColor.b, outlineColor.a * ringEdge), color, dotEdge);
            }
            tex.SetPixel(x, y, c);
        }
        tex.Apply();

        Image dot = HealthHUD.CreateElement<Image>(transform, "Crosshair_Dot", Vector2.zero, new Vector2(size, size));
        RectTransform rect = dot.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        dot.sprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
        dot.color = Color.white;
        dot.raycastTarget = false;
    }
}
