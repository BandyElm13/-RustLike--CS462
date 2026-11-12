using UnityEngine;

/// <summary>
/// Red see-through overlay on an enemy when it's shot: adds a transparent material on top of
/// each of its renderers, fades it out over Duration (fast at first), then removes it.
/// Added automatically by EnemyStats; uses Resources/HitFlash.mat unless another is assigned.
/// </summary>
public class EnemyHitFlash : MonoBehaviour
{
    [Tooltip("Transparent overlay material. Empty = Resources/HitFlash.")]
    [SerializeField] private Material flashMaterial;
    [SerializeField] private Color flashColor = new Color(1f, 0.05f, 0.05f, 0.55f);
    [Tooltip("Seconds until the overlay has fully faded.")]
    [SerializeField] private float duration = 1f;
    [Tooltip("Higher = drops off faster right after the hit (1 = linear).")]
    [SerializeField] private float fadeSharpness = 2.5f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private Renderer[] renderers;
    private Material[][] originalMaterials;
    private Material overlay;
    private float timer;
    private bool applied;

    private void Awake()
    {
        if (flashMaterial == null) flashMaterial = Resources.Load<Material>("HitFlash");
        if (flashMaterial == null) return;

        overlay = new Material(flashMaterial);
        MakeTransparent(overlay);

        // Meshes only; skip particles, lines and trails.
        Renderer[] all = GetComponentsInChildren<Renderer>(true);
        renderers = System.Array.FindAll(all, r => r is MeshRenderer || r is SkinnedMeshRenderer);
        originalMaterials = new Material[renderers.Length][];
    }

    public void Flash()
    {
        if (overlay == null) return;
        timer = duration;
        if (!applied) Apply(true);
    }

    private void Update()
    {
        if (!applied) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Apply(false);
            return;
        }

        float t = Mathf.Clamp01(timer / Mathf.Max(0.01f, duration));
        SetOverlayAlpha(Mathf.Pow(t, fadeSharpness));
    }

    private void Apply(bool on)
    {
        applied = on;
        if (on) SetOverlayAlpha(1f);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null) continue;

            if (on)
            {
                Material[] current = r.sharedMaterials;
                originalMaterials[i] = current;
                Material[] withOverlay = new Material[current.Length + 1];
                current.CopyTo(withOverlay, 0);
                withOverlay[current.Length] = overlay; // extra slot redraws the mesh on top
                r.sharedMaterials = withOverlay;
            }
            else if (originalMaterials[i] != null)
            {
                r.sharedMaterials = originalMaterials[i];
            }
        }
    }

    private void SetOverlayAlpha(float alpha)
    {
        Color c = flashColor;
        c.a *= alpha;
        if (overlay.HasProperty(BaseColorId)) overlay.SetColor(BaseColorId, c);
        if (overlay.HasProperty(ColorId)) overlay.SetColor(ColorId, c);
    }

    // Force URP Unlit transparent/alpha-blended settings, in case the asset is changed.
    private static void MakeTransparent(Material m)
    {
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
        m.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.SetOverrideTag("RenderType", "Transparent");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private void OnDisable()
    {
        if (applied) Apply(false);
    }

    private void OnDestroy()
    {
        if (overlay != null) Destroy(overlay);
    }
}
