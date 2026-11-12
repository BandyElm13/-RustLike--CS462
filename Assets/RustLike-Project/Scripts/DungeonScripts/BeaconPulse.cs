using UnityEngine;

/// <summary>
/// Dungeon beacon (BEACON pillar in the planet scene): pulses red so players can find the dungeon.
///  - The pillar is drawn with a no-fog material (Resources/StarNoFog) in HDR red, so it shows
///    through the fog from anywhere and glows with bloom.
///  - The existing child Light is made a Point light with a limited Range, so the red light only
///    colors the area around the beacon instead of the whole planet. It pulses with the pillar.
/// </summary>
public class BeaconPulse : MonoBehaviour
{
    [Header("Pulse")]
    [Tooltip("Pulses per second.")]
    [SerializeField] private float pulseSpeed = 0.6f;
    [SerializeField] private Color color = new Color(1f, 0.05f, 0.03f, 1f);

    [Header("Pillar (visible through fog)")]
    [Tooltip("No-fog material for the pillar. Empty = Resources/StarNoFog.")]
    [SerializeField] private Material noFogMaterial;
    [Tooltip("HDR brightness of the pillar at the dimmest / brightest point of the pulse.")]
    [SerializeField] private float pillarMinIntensity = 1.2f;
    [SerializeField] private float pillarMaxIntensity = 5f;

    [Header("Light (local only)")]
    [Tooltip("Light to pulse. Empty = the first Light in children.")]
    [SerializeField] private Light beaconLight;
    [Tooltip("How far the red light reaches (world units).")]
    [SerializeField] private float lightRange = 45f;
    [SerializeField] private float lightMinIntensity = 2f;
    [SerializeField] private float lightMaxIntensity = 12f;
    [Tooltip("Move the light to the pillar's base (where the dungeon entrance is).")]
    [SerializeField] private bool lightAtBase = true;

    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private Material pillarMaterial;

    private void Start()
    {
        if (beaconLight == null) beaconLight = GetComponentInChildren<Light>();
        if (beaconLight != null)
        {
            beaconLight.type = LightType.Point;          // local light, not planet-wide
            beaconLight.range = lightRange;
            beaconLight.color = color;
            beaconLight.shadows = LightShadows.None;
            if (lightAtBase)
            {
                // Pillar is a unit cube scaled tall; sit the light just above its bottom end.
                Vector3 baseLocal = new Vector3(0f, -0.5f, 0f);
                beaconLight.transform.position = transform.TransformPoint(baseLocal) + transform.up * 2f;
            }
        }

        if (noFogMaterial == null) noFogMaterial = Resources.Load<Material>("StarNoFog");
        Renderer pillar = GetComponent<Renderer>();
        if (pillar != null && noFogMaterial != null)
        {
            pillarMaterial = new Material(noFogMaterial);
            pillar.sharedMaterial = pillarMaterial;
            pillar.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    private void Update()
    {
        // Smooth 0..1..0 pulse.
        float t = 0.5f - 0.5f * Mathf.Cos(Time.time * pulseSpeed * Mathf.PI * 2f);

        if (beaconLight != null)
            beaconLight.intensity = Mathf.Lerp(lightMinIntensity, lightMaxIntensity, t);

        if (pillarMaterial != null)
            pillarMaterial.SetColor(ColorId, color * Mathf.Lerp(pillarMinIntensity, pillarMaxIntensity, t));
    }

    private void OnDestroy()
    {
        if (pillarMaterial != null) Destroy(pillarMaterial);
    }
}
