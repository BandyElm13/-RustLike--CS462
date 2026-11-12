using UnityEngine;

/// <summary>
/// Player volume settings (0–1), saved in PlayerPrefs.
///   Master → AudioListener.volume (everything).
///   Music  → AudioManager's music source (and DeathSequence's fallback theme).
///   SFX    → multiply sound-effect volumes by GameAudioSettings.Sfx when playing them.
/// </summary>
public static class GameAudioSettings
{
    private const string MasterKey = "volume_master";
    private const string MusicKey = "volume_music";
    private const string SfxKey = "volume_sfx";

    public static float Master { get; private set; } = 1f;
    public static float Music { get; private set; } = 1f;
    public static float Sfx { get; private set; } = 1f;

    /// <summary>Raised whenever a volume changes (AudioManager listens to update the music).</summary>
    public static event System.Action Changed;

    public static void SetMaster(float value)
    {
        Master = Mathf.Clamp01(value);
        AudioListener.volume = Master;
        Save(MasterKey, Master);
    }

    public static void SetMusic(float value)
    {
        Music = Mathf.Clamp01(value);
        Save(MusicKey, Music);
    }

    public static void SetSfx(float value)
    {
        Sfx = Mathf.Clamp01(value);
        Save(SfxKey, Sfx);
    }

    private static void Save(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    // Load saved values before the first scene so every sound starts at the right volume.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Load()
    {
        Master = PlayerPrefs.GetFloat(MasterKey, 1f);
        Music = PlayerPrefs.GetFloat(MusicKey, 1f);
        Sfx = PlayerPrefs.GetFloat(SfxKey, 1f);
        AudioListener.volume = Master;
    }
}
