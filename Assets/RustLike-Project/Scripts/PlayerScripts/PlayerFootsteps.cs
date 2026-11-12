using UnityEngine;

/// <summary>
/// Footstep sounds for the first-person player. Plays a step every Step Distance of ground
/// travelled (longer strides while sprinting) and a heavier step on landing.
/// Works on spherical planets: distance is measured along the ground, not including falling.
///
/// Drop footstep clips into Clips (a random one plays each step). Until then, simple
/// placeholder steps are generated in code so there's always something audible.
/// </summary>
[RequireComponent(typeof(FirstPersonController))]
public class PlayerFootsteps : MonoBehaviour
{
    [Header("Clips")]
    [Tooltip("Random clip per step. Leave empty to use generated placeholder steps.")]
    [SerializeField] private AudioClip[] clips;
    [Tooltip("Optional clip for landing after a jump/fall. Uses a step clip if empty.")]
    [SerializeField] private AudioClip landClip;
    [SerializeField] private AudioSource audioSource;

    [Header("Timing")]
    [Tooltip("Metres of ground travelled per step while walking.")]
    [SerializeField] private float walkStepDistance = 2.2f;
    [Tooltip("Metres of ground travelled per step while sprinting.")]
    [SerializeField] private float sprintStepDistance = 3f;
    [Tooltip("Below this ground speed no steps play.")]
    [SerializeField] private float minSpeed = 0.5f;
    [Tooltip("Airborne at least this long before landing plays a landing step.")]
    [SerializeField] private float minAirTimeForLanding = 0.25f;

    [Header("Volume / Variation")]
    [SerializeField, Range(0f, 1f)] private float walkVolume = 0.45f;
    [SerializeField, Range(0f, 1f)] private float sprintVolume = 0.6f;
    [SerializeField, Range(0f, 1f)] private float landVolume = 0.75f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.9f, 1.1f);

    private FirstPersonController controller;
    private GravityBody gravityBody;
    private Rigidbody rb;
    private float distanceSinceStep;
    private float airTime;
    private bool wasGrounded = true;
    private int lastClip = -1;

    private static AudioClip[] placeholderClips;

    private void Awake()
    {
        controller = GetComponent<FirstPersonController>();
        gravityBody = GetComponent<GravityBody>();
        rb = GetComponent<Rigidbody>();

        if (audioSource == null)
        {
            // Own source so steps don't fight other sounds on the player.
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f; // the listener is on the player, keep it 2D

        if (clips == null || clips.Length == 0 || System.Array.TrueForAll(clips, c => c == null))
        {
            // (Re)build if missing; with Domain Reload off, last session's clips may be destroyed.
            if (placeholderClips == null || placeholderClips[0] == null) placeholderClips = CreatePlaceholderSteps();
            clips = placeholderClips;
        }
    }

    private void Update()
    {
        bool grounded = controller.IsGrounded;

        if (!grounded)
        {
            airTime += Time.deltaTime;
            wasGrounded = false;
            return;
        }

        if (!wasGrounded)
        {
            if (airTime >= minAirTimeForLanding)
                Play(landClip != null ? landClip : PickClip(), landVolume);
            distanceSinceStep = 0f;
        }
        wasGrounded = true;
        airTime = 0f;

        // Ground speed = velocity along the surface (ignore falling/sticking to the ground).
        Vector3 up = gravityBody != null ? gravityBody.UpDirection : transform.up;
        Vector3 velocity = rb != null ? rb.linearVelocity : Vector3.zero;
        float speed = Vector3.ProjectOnPlane(velocity, up).magnitude;

        if (speed < minSpeed || !controller.IsMoving)
        {
            // Next step comes quickly after starting to move again.
            distanceSinceStep = Mathf.Max(distanceSinceStep, StepDistance * 0.6f);
            return;
        }

        distanceSinceStep += speed * Time.deltaTime;
        if (distanceSinceStep >= StepDistance)
        {
            distanceSinceStep = 0f;
            Play(PickClip(), controller.IsSprinting ? sprintVolume : walkVolume);
        }
    }

    private float StepDistance => controller.IsSprinting ? sprintStepDistance : walkStepDistance;

    private AudioClip PickClip()
    {
        if (clips == null || clips.Length == 0) return null;
        if (clips.Length == 1) return clips[0];

        // Random, but never the same clip twice in a row.
        int index = Random.Range(0, clips.Length - 1);
        if (index >= lastClip && lastClip >= 0) index++;
        lastClip = index;
        return clips[index];
    }

    private void Play(AudioClip clip, float volume)
    {
        if (clip == null) return;
        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        audioSource.PlayOneShot(clip, volume * GameAudioSettings.Sfx);
    }

    // Short muffled thuds (filtered noise + low thump), a few variations.
    private static AudioClip[] CreatePlaceholderSteps()
    {
        const int sampleRate = 44100;
        const float length = 0.14f;
        int samples = Mathf.CeilToInt(sampleRate * length);
        var result = new AudioClip[4];
        var random = new System.Random(462);

        for (int v = 0; v < result.Length; v++)
        {
            float[] data = new float[samples];
            float thumpFreq = 70f + v * 12f;
            float filter = 0.08f + v * 0.02f;
            float noise = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float attack = Mathf.Clamp01(t / 0.004f);
                noise += ((float)random.NextDouble() * 2f - 1f - noise) * filter; // low-passed noise
                float scuff = noise * Mathf.Exp(-t * 35f);
                float thump = Mathf.Sin(2f * Mathf.PI * thumpFreq * t) * Mathf.Exp(-t * 45f);
                data[i] = attack * (scuff * 1.6f + thump * 0.7f);
            }

            AudioClip clip = AudioClip.Create("PlaceholderStep" + v, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            result[v] = clip;
        }
        return result;
    }
}
