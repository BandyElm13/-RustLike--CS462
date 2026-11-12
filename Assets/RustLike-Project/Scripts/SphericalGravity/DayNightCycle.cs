using UnityEngine;

/// <summary>
/// Day/night cycle: put this on the star object (StarLight: a sphere with a Directional Light
/// inside). It orbits the planet center at an adjustable speed and keeps the light pointed at
/// the planet, so the side facing the star is day and the far side is night.
///
/// The star's sphere is switched to a material that ignores fog (Resources/StarNoFog), so it
/// stays bright from far away instead of fading into the fog color. It glows: the sphere is HDR
/// bright, a soft camera-facing halo (Resources/StarGlow) surrounds it, and a scene-only Bloom
/// volume is added at runtime (post-processing is switched on for the player camera).
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    [Header("Orbit")]
    [Tooltip("What to orbit. Empty = the GravityAttractor in the scene (the planet).")]
    [SerializeField] private Transform planetCenter;
    [Tooltip("Seconds for one full day + night.")]
    [SerializeField] private float dayLengthSeconds = 300f;
    [Tooltip("Multiplier on the speed; change at runtime to speed up / slow down time (0 = paused).")]
    [SerializeField] private float timeScale = 1f;
    [Tooltip("Axis the star orbits around (world space). Tilt it for seasons-like angles.")]
    [SerializeField] private Vector3 orbitAxis = new Vector3(0f, 0.2f, 1f);
    [Tooltip("Keep the star at its current distance from the planet (else use Orbit Radius).")]
    [SerializeField] private bool keepStartDistance = true;
    [SerializeField] private float orbitRadius = 600f;

    [Header("Night speed")]
    [Tooltip("Optional: make nights pass faster/slower than days for the player. 1 = constant speed.")]
    [SerializeField] private float nightSpeedMultiplier = 1f;
    [Tooltip("Player used to judge day/night for Night Speed Multiplier. Empty = object tagged Player.")]
    [SerializeField] private Transform player;

    [Header("Visible through fog")]
    [Tooltip("Material for the star sphere that ignores fog. Empty = Resources/StarNoFog.")]
    [SerializeField] private Material noFogMaterial;
    [SerializeField, ColorUsage(false, true)] private Color starColor = new Color(1f, 0.95f, 0.85f, 1f);
    [Tooltip("HDR brightness of the sphere. Above 1 makes it glow with bloom.")]
    [SerializeField] private float starIntensity = 6f;

    [Header("Glow")]
    [Tooltip("Soft halo billboard around the star (ignores fog). Empty = Resources/StarGlow.")]
    [SerializeField] private Material glowMaterial;
    [SerializeField, ColorUsage(false, true)] private Color glowColor = new Color(1f, 0.75f, 0.45f, 1f);
    [SerializeField] private float glowIntensity = 1.5f;
    [Tooltip("Halo diameter as a multiple of the sphere's size.")]
    [SerializeField] private float glowSize = 6f;
    [Tooltip("Add a Bloom post-process (planet scene only) and turn on post-processing for the player camera.")]
    [SerializeField] private bool addBloom = true;
    [SerializeField] private float bloomIntensity = 1.2f;
    [SerializeField] private float bloomThreshold = 1.1f;
    [SerializeField, Range(0f, 1f)] private float bloomScatter = 0.75f;

    private Light starLight;
    private Transform glow;
    private Camera viewCamera;

    /// <summary>0..1 through the current cycle (useful for UI / sky tweaks).</summary>
    public float CycleProgress { get; private set; }

    private void Start()
    {
        if (planetCenter == null)
        {
            GravityAttractor attractor = GravityAttractor.Main != null
                ? GravityAttractor.Main
                : FindAnyObjectByType<GravityAttractor>();
            if (attractor != null) planetCenter = attractor.transform;
        }

        starLight = GetComponentInChildren<Light>();

        if (planetCenter != null && !keepStartDistance)
        {
            Vector3 dir = transform.position - planetCenter.position;
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.up;
            transform.position = planetCenter.position + dir.normalized * orbitRadius;
        }

        SetUpVisuals();
        CreateGlow();
        if (addBloom) SetUpBloom();
        AimLight();
    }

    // Halo always faces the camera.
    private void LateUpdate()
    {
        if (glow == null) return;
        if (viewCamera == null) viewCamera = FindViewCamera();
        if (viewCamera != null)
            glow.rotation = Quaternion.LookRotation(glow.position - viewCamera.transform.position, viewCamera.transform.up);
    }

    private static Camera FindViewCamera()
    {
        FirstPersonController fpc = FindAnyObjectByType<FirstPersonController>();
        if (fpc != null && fpc.Camera != null) return fpc.Camera;
        return Camera.main;
    }

    private void CreateGlow()
    {
        if (glowMaterial == null) glowMaterial = Resources.Load<Material>("StarGlow");
        if (glowMaterial == null || glowSize <= 0f) return;

        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "StarGlow";
        Destroy(quad.GetComponent<Collider>());
        glow = quad.transform;
        glow.SetParent(transform, false);
        glow.localPosition = Vector3.zero;
        glow.localScale = Vector3.one * glowSize; // relative to the sphere (default sphere = 1 unit)

        Material m = new Material(glowMaterial);
        m.SetTexture("_MainTex", CreateGlowTexture(128));
        m.SetColor("_Color", glowColor * glowIntensity);
        MeshRenderer r = quad.GetComponent<MeshRenderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    // Radial falloff: bright core fading smoothly to nothing at the edge.
    private static Texture2D CreateGlowTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        float half = size / 2f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half)) / half; // 0 center, 1 edge
            float a = Mathf.Clamp01(1f - d);
            a = a * a * a; // soft falloff
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return tex;
    }

    // Bloom only for this scene: a runtime global Volume, plus post-processing on the player camera.
    private void SetUpBloom()
    {
        Camera cam = FindViewCamera();
        if (cam != null)
        {
            var data = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (data != null) data.renderPostProcessing = true;
        }

        GameObject volumeObject = new GameObject("StarBloomVolume");
        volumeObject.transform.SetParent(transform, false);
        var volume = volumeObject.AddComponent<UnityEngine.Rendering.Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;

        var profile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
        var bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>(true);
        bloom.intensity.Override(bloomIntensity);
        bloom.threshold.Override(bloomThreshold);
        bloom.scatter.Override(bloomScatter);
        volume.profile = profile;
    }

    private void Update()
    {
        if (planetCenter == null || dayLengthSeconds <= 0f) return;

        float speed = 360f / dayLengthSeconds * timeScale;
        if (!Mathf.Approximately(nightSpeedMultiplier, 1f) && IsNightForPlayer()) speed *= nightSpeedMultiplier;

        Vector3 axis = orbitAxis.sqrMagnitude > 0.0001f ? orbitAxis.normalized : Vector3.forward;
        float step = speed * Time.deltaTime;
        transform.RotateAround(planetCenter.position, axis, step);
        CycleProgress = Mathf.Repeat(CycleProgress + step / 360f, 1f);

        AimLight();
    }

    // Directional light shines from the star toward the planet.
    private void AimLight()
    {
        if (planetCenter == null || starLight == null) return;
        Vector3 toPlanet = planetCenter.position - transform.position;
        if (toPlanet.sqrMagnitude > 0.0001f)
            starLight.transform.rotation = Quaternion.LookRotation(toPlanet.normalized);
    }

    private bool IsNightForPlayer()
    {
        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found == null) return false;
            player = found.transform;
        }
        Vector3 up = (player.position - planetCenter.position).normalized;
        Vector3 toStar = (transform.position - planetCenter.position).normalized;
        return Vector3.Dot(up, toStar) < 0f; // star is below the player's horizon
    }

    private void SetUpVisuals()
    {
        if (noFogMaterial == null) noFogMaterial = Resources.Load<Material>("StarNoFog");

        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            // A light inside a sphere: the sphere must never block or shadow its own light.
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;

            if (noFogMaterial != null)
            {
                Material m = new Material(noFogMaterial);
                m.SetColor("_Color", starColor * starIntensity);
                r.sharedMaterial = m;
            }
        }
    }
}
