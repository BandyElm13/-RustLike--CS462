using UnityEngine;

/// <summary>
/// Player gun: hold left mouse for full-auto hitscan fire from the screen center, hold right
/// mouse to aim down sights. Ammo is infinite; each shot costs energy, which recharges after
/// a short pause (shown by EnergyHUD), so you can still run dry and wait for it to refill.
/// The tracer starts at the hand's muzzle (tracerPoint).
/// </summary>
public class PlayerShooting : MonoBehaviour
{
    [Header("Shooting")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float shootRange = 100f;
    [SerializeField] private int damage = 25;
    [SerializeField] private BulletTracer tracerPrefab;
    [Tooltip("Where tracers start: the tip of the hand model. Falls back to a child named 'Muzzle'.")]
    [SerializeField] private Transform tracerPoint;

    [Header("Timing Variables")]
    private float timestamp;
    public static float energy = 0f;
    /// <summary>Energy capacity of the current player (EnergyHUD scales its bar to this).</summary>
    public static float maxEnergy = 30f;
    private float chargeBuffer = 0f;
    [Tooltip("Seconds between shots while the fire button is held (full auto).")]
    [SerializeField] private float fireRate = 0.12f;
    [Tooltip("Energy regained per second once recharging starts.")]
    [SerializeField] private float chargeRate = 1f;
    [Tooltip("Seconds after the last shot before energy starts recharging.")]
    [SerializeField] private float chargeDelay = 2f;
    [Tooltip("Shots in a full charge (each shot costs Energy Cost).")]
    [SerializeField] private float energyMax = 30f;
    [SerializeField] private float energyCost = 1f;

    [Header("Sound")]
    [Tooltip("Played on every shot (SFX/ShootSound).")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField, Range(0f, 1f)] private float shootVolume = 0.6f;
    [SerializeField] private Vector2 shootPitchRange = new Vector2(0.9f, 1.1f);

    [Header("Aim Down Sights (hold right mouse)")]
    [Tooltip("The hand/gun model under the camera. Falls back to a camera child named 'Player-Hand'.")]
    [SerializeField] private Transform handModel;
    [Tooltip("Hand's local position while aiming, as an offset from where it normally sits.")]
    [SerializeField] private Vector3 aimHandOffset = new Vector3(0.9f, 0.35f, 0f);
    [Tooltip("How quickly the hand and zoom move in/out of aim.")]
    [SerializeField] private float aimSpeed = 12f;

    private FirstPersonController controller;
    private AudioSource shootAudio;
    private Vector3 handRestPosition;

    private void Start()
    {
        controller = GetComponent<FirstPersonController>();

        if (playerCamera == null)
            playerCamera = controller != null && controller.Camera != null ? controller.Camera : Camera.main;

        // Fall back to Assets/Resources/Tracer.prefab if none is assigned on the prefab.
        if (tracerPrefab == null)
            tracerPrefab = Resources.Load<BulletTracer>("Tracer");

        if (handModel == null && playerCamera != null)
            handModel = playerCamera.transform.Find("Player-Hand");
        if (handModel != null) handRestPosition = handModel.localPosition;

        if (tracerPoint == null)
            tracerPoint = FindChildRecursive(handModel != null ? handModel : transform, "Muzzle");

        shootAudio = gameObject.AddComponent<AudioSource>();
        shootAudio.playOnAwake = false;
        shootAudio.spatialBlend = 0f;

        // energy is static, so it would otherwise start at 0 (or carry over from a previous run).
        maxEnergy = energyMax;
        energy = energyMax;
        chargeBuffer = 0f;
    }

    private void Update()
    {
        // Aim down sights while right mouse is held.
        bool aiming = Input.GetMouseButton(1);
        if (controller != null) controller.IsAiming = aiming;
        if (handModel != null)
        {
            Vector3 target = handRestPosition + (aiming ? aimHandOffset : Vector3.zero);
            handModel.localPosition = Vector3.Lerp(handModel.localPosition, target, aimSpeed * Time.deltaTime);
        }

        // Full auto: keep firing while left mouse is held.
        if (Input.GetMouseButton(0))
        {
            if (canShoot()) Shoot();
        }

        // Energy recharge
        chargeBuffer -= Time.deltaTime;
        if ((energy < energyMax) && (chargeBuffer <= 0f))
            energy = Mathf.Min(energyMax, energy + chargeRate * Time.deltaTime);
    }

    private bool canShoot()
    {
        if ((Time.time < timestamp) || (energy < energyCost)) return false;
        timestamp = Time.time + fireRate;
        return true;
    }

    private void Shoot()
    {
        energy -= energyCost;
        chargeBuffer = chargeDelay;
        PlayerStats.NotifyCombat(); // firing counts as combat (slows health regen)

        if (shootSound != null)
        {
            shootAudio.pitch = Random.Range(shootPitchRange.x, shootPitchRange.y);
            shootAudio.PlayOneShot(shootSound, shootVolume * GameAudioSettings.Sfx);
        }

        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        // Don't let the ray hit the player's own colliders.
        bool didHit = false;
        RaycastHit hit = default;
        RaycastHit[] hits = Physics.RaycastAll(ray, shootRange, ~0, QueryTriggerInteraction.Ignore);
        float closest = float.MaxValue;
        foreach (RaycastHit h in hits)
        {
            if (h.collider.transform.IsChildOf(transform)) continue;
            if (h.distance < closest) { closest = h.distance; hit = h; didHit = true; }
        }

        if (tracerPrefab != null)
            Instantiate(tracerPrefab).Fire(tracerPoint ? tracerPoint.position : ray.origin, didHit ? hit.point : ray.GetPoint(shootRange));

        if (didHit)
        {
            EnemyStats enemy = hit.collider.GetComponentInParent<EnemyStats>();
            if (enemy != null) enemy.TakeDamage(damage);
        }
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
