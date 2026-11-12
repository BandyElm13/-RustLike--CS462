using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    public static int currentPlayerHealth = 100;
    /// <summary>Max health of the current player (HealthHUD scales its bar to this).</summary>
    public static int maxPlayerHealth = 100;

    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    [Tooltip("Health regained per second once regeneration starts.")]
    [SerializeField] private float regenPerSecond = 3f;
    [Tooltip("Seconds without taking damage before health starts regenerating.")]
    [SerializeField] private float regenDelay = 4f;
    [Tooltip("Seconds out of combat (no damage taken, no shots fired) before regen speeds up.")]
    [SerializeField] private float outOfCombatDelay = 5f;
    [Tooltip("Health regained per second once out of combat.")]
    [SerializeField] private float outOfCombatRegenPerSecond = 15f;

    /// <summary>Time.time of the last combat action (hit taken or shot fired).</summary>
    public static float LastCombatTime { get; private set; } = -999f;

    /// <summary>Call when the player does something that counts as combat (e.g. firing).</summary>
    public static void NotifyCombat() => LastCombatTime = Time.time;

    // Must match a scene name added to File > Build Profiles (Scene List)
    [SerializeField] private string gameOverScene = "DeathScene";

    [Tooltip("Testing only: E = take 10 damage, R = heal 10. Off by default (E is the ability key).")]
    [SerializeField] private bool enableDebugKeys = false;

    [Header("Damage Indication")]
    [Tooltip("Full-screen vignette flashed when the player takes damage (UI/VignettePNG).")]
    [SerializeField] private Texture2D damageVignette;
    [SerializeField] private Color vignetteColor = Color.white;
    [Tooltip("Seconds from the hit until the vignette has fully faded out.")]
    [SerializeField] private float vignetteDuration = 1f;
    [Tooltip("Higher = drops off faster right after the hit (1 = linear fade).")]
    [SerializeField] private float vignetteFadeSharpness = 2.5f;
    [Tooltip("Sound played when the player takes damage (SFX/DamageSound).")]
    [SerializeField] private AudioClip damageSound;
    [SerializeField, Range(0f, 1f)] private float damageSoundVolume = 0.8f;
    [SerializeField] private Vector2 damagePitchRange = new Vector2(0.85f, 1.15f);

    public bool isDead;

    private RawImage vignetteImage;
    private AudioSource damageAudio;
    private float vignetteTimer;
    private float regenCooldown;   // counts down after a hit; regen runs at 0
    private float regenBuffer;     // fractional health waiting to be added

    void Start()
    {
        // Automatically resets player health to full whenever you press Start/Play
        maxPlayerHealth = Mathf.Max(1, maxHealth);
        currentPlayerHealth = maxPlayerHealth;
        isDead = false;
        LastCombatTime = -999f;
        Debug.Log("Player Health Initialized = " + currentPlayerHealth);

        SetUpDamageIndication();
    }

    public void takeDamage(int amount)
    {
        if (isDead) return;

        currentPlayerHealth -= amount;
        if (currentPlayerHealth < 0) currentPlayerHealth = 0;

        if (amount > 0)
        {
            ShowDamage();
            regenCooldown = regenDelay; // getting hit pauses regeneration
            regenBuffer = 0f;
            NotifyCombat();
        }

        if (currentPlayerHealth <= 0)
        {
            PlayerDeath();
        }
    }

    private void Regenerate()
    {
        if (isDead || currentPlayerHealth >= maxPlayerHealth) { regenBuffer = 0f; return; }

        if (regenCooldown > 0f)
        {
            regenCooldown -= Time.deltaTime;
            return;
        }

        // Out of combat for a while (no hits, no shots): regenerate much faster.
        bool outOfCombat = Time.time - LastCombatTime >= outOfCombatDelay;
        float rate = outOfCombat ? outOfCombatRegenPerSecond : regenPerSecond;

        // Health is an int, so bank fractions until a whole point is ready.
        regenBuffer += rate * Time.deltaTime;
        int whole = Mathf.FloorToInt(regenBuffer);
        if (whole <= 0) return;

        regenBuffer -= whole;
        currentPlayerHealth = Mathf.Min(maxPlayerHealth, currentPlayerHealth + whole);
    }

    private void SetUpDamageIndication()
    {
        // Own audio source so the hit sound's pitch doesn't affect other player sounds.
        damageAudio = gameObject.AddComponent<AudioSource>();
        damageAudio.playOnAwake = false;
        damageAudio.spatialBlend = 0f;

        if (damageVignette == null) return;

        // Screen-space overlay above the HUD, drawn but never blocking clicks.
        GameObject canvasObject = new GameObject("DamageVignette_Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        GameObject imageObject = new GameObject("DamageVignette", typeof(RectTransform), typeof(RawImage));
        imageObject.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero; // stretch to fill the screen

        vignetteImage = imageObject.GetComponent<RawImage>();
        vignetteImage.texture = damageVignette;
        vignetteImage.raycastTarget = false;
        SetVignetteAlpha(0f);
    }

    private void ShowDamage()
    {
        vignetteTimer = vignetteDuration; // restart the flash on every hit

        if (damageSound != null && damageAudio != null)
        {
            damageAudio.pitch = Random.Range(damagePitchRange.x, damagePitchRange.y);
            damageAudio.PlayOneShot(damageSound, damageSoundVolume * GameAudioSettings.Sfx);
        }
    }

    private void UpdateVignette()
    {
        if (vignetteImage == null || vignetteTimer <= 0f) return;

        vignetteTimer -= Time.deltaTime;
        float t = Mathf.Clamp01(vignetteTimer / Mathf.Max(0.01f, vignetteDuration)); // 1 at hit → 0
        SetVignetteAlpha(Mathf.Pow(t, vignetteFadeSharpness));
    }

    private void SetVignetteAlpha(float alpha)
    {
        Color c = vignetteColor;
        c.a *= alpha;
        vignetteImage.color = c;
        vignetteImage.enabled = alpha > 0.001f;
    }

    private void PlayerDeath()
    {
        isDead = true;
        Debug.Log("Player died! Loading " + gameOverScene);

        // Freeze the run time so the death screen shows how long this run lasted.
        RunTimer.Stop();

        // FirstPersonController locks the cursor; free it so the death screen can be clicked
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // The death theme is started by DeathSequence in the death scene.
        SceneManager.LoadScene(gameOverScene);
    }

    void Update()
    {
        UpdateVignette();
        Regenerate();

        if (!enableDebugKeys) return;

        // Press E to take 10 damage (1 segment)
        if(Input.GetKeyDown(KeyCode.E))
        {
            takeDamage(10);
            Debug.Log("New damage: " + currentPlayerHealth);
        }

        // Press R to add 10 health back (1 segment)
        if(Input.GetKeyDown(KeyCode.R))
        {
            if (currentPlayerHealth < maxPlayerHealth)
            {
                currentPlayerHealth += 10;
                if (currentPlayerHealth > maxPlayerHealth) currentPlayerHealth = maxPlayerHealth;
                Debug.Log("Healed! Current Health: " + currentPlayerHealth);
            }
        }
    }
}
