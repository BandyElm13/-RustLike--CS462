using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Global music player. Lives in the Main Menu scene (Global_Audio_Manager prefab) and survives
/// scene loads. When a scene loads it switches to that scene's music state (see Scene Music);
/// gameplay scripts can also call TriggerMusicState directly (WaveManager → "wavestart").
///
/// If Play is pressed in a scene that has no AudioManager (e.g. testing a planet directly),
/// the prefab is loaded from Resources/Global_Audio_Manager so music still works.
///
/// States: menustart, wavestart, ambientstart, player_death_theme, stop.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Audio Source")]
    [SerializeField] private AudioSource musicSource;

    [Header("Music Library (Drag your 3 MP3s Here)")]
    public AudioClip menuTheme;
    public AudioClip waveTheme;
    public AudioClip ambientTheme; // Changed from finalTheme to ambientTheme!
    public AudioClip player_death_theme;

    [System.Serializable]
    public struct SceneMusic
    {
        public string sceneName;
        [Tooltip("Music state to switch to when this scene loads. Empty = keep current music.")]
        public string musicState;
    }

    [Header("Scene Music")]
    [Tooltip("Music state applied automatically when each scene loads.")]
    [SerializeField] private SceneMusic[] sceneMusic =
    {
        new SceneMusic { sceneName = "Main Menu", musicState = "menustart" },
        new SceneMusic { sceneName = "Planet-1-RobotWorld", musicState = "wavestart" },
        new SceneMusic { sceneName = "Dungeon", musicState = "ambientstart" },
        new SceneMusic { sceneName = "DeathScene", musicState = "player_death_theme" },
    };

    private const string ResourcePath = "Global_Audio_Manager";

    private float baseVolume = 1f;
    private float MusicVolume => baseVolume * GameAudioSettings.Music;
    private Coroutine fadeRoutine;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource == null) musicSource = GetComponent<AudioSource>();
        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        baseVolume = musicSource.volume;

        SceneManager.sceneLoaded += OnSceneLoaded;
        GameAudioSettings.Changed += ApplyVolume;
        ApplyVolume();
    }

    void OnDestroy()
    {
        if (instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        GameAudioSettings.Changed -= ApplyVolume;
        instance = null;
    }

    void Start()
    {
        // The scene this manager was created in (normally the Main Menu → menu theme).
        ApplySceneMusic(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) ApplySceneMusic(scene.name);
    }

    private void ApplySceneMusic(string sceneName)
    {
        foreach (SceneMusic entry in sceneMusic)
        {
            if (entry.sceneName == sceneName && !string.IsNullOrEmpty(entry.musicState))
            {
                TriggerMusicState(entry.musicState);
                return;
            }
        }
    }

    // THE GLOBAL TRIGGER
    public void TriggerMusicState(string situationState)
    {
        if (string.IsNullOrEmpty(situationState)) return;

        AudioClip selectedClip;

        switch (situationState.ToLower())
        {
            case "menustart":
                selectedClip = menuTheme;
                break;
            case "wavestart":
                selectedClip = waveTheme;
                break;
            case "ambientstart": // Matches your new ambient situation trigger!
                selectedClip = ambientTheme;
                break;
            case "player_death_theme":
                selectedClip = player_death_theme;
                break;
            case "stop":
            case "none":
                CancelFade();
                musicSource.Stop();
                musicSource.clip = null;
                return;
            default:
                Debug.LogWarning("AudioManager: unknown music state '" + situationState + "'.");
                return;
        }

        // No clip assigned for this state yet (e.g. ambientTheme for the Dungeon):
        // keep whatever is playing instead of going silent. Expected, so no warning.
        if (selectedClip == null) return;

        if (musicSource.clip != selectedClip || !musicSource.isPlaying)
        {
            CancelFade(); // a new track always starts at full volume
            musicSource.Stop();
            musicSource.clip = selectedClip;
            musicSource.Play();
        }
    }

    /// <summary>
    /// Fade the current music to silence over duration seconds, then stop it.
    /// The next TriggerMusicState that starts a track plays at normal volume again.
    /// </summary>
    public void FadeOutMusic(float duration)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeOutRoutine(duration));
    }

    private System.Collections.IEnumerator FadeOutRoutine(float duration)
    {
        float start = musicSource.volume;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            musicSource.volume = Mathf.Lerp(start, 0f, t / duration);
            yield return null;
        }
        musicSource.Stop();
        musicSource.clip = null;
        musicSource.volume = MusicVolume;
        fadeRoutine = null;
    }

    // Music slider changed: update the playing track (unless a fade-out is running).
    private void ApplyVolume()
    {
        if (fadeRoutine == null && musicSource != null) musicSource.volume = MusicVolume;
    }

    private void CancelFade()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
        musicSource.volume = MusicVolume;
    }

    // Make sure there's always an AudioManager, even when Play is pressed outside the Main Menu.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        if (instance != null) return;

        AudioManager prefab = Resources.Load<AudioManager>(ResourcePath);
        if (prefab == null)
        {
            Debug.LogWarning("AudioManager: no AudioManager in the scene and none found at Resources/" + ResourcePath + ".");
            return;
        }

        AudioManager created = Instantiate(prefab);
        created.name = prefab.name;
    }

    // Domain Reload is off in this project: clear the static so a new Play session starts clean.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() => instance = null;
}
