using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Death screen sequence (DeathScene): starts the death theme on the global AudioManager,
/// plays a sound shortly after the scene opens, then fades in "You Died" and the run's
/// Time Alive (from RunTimer) and the run's Score, holds, slowly fades all out, then loads the Main Menu.
/// If there's no global AudioManager, it plays fallbackDeathTheme itself.
/// </summary>
public class DeathSequence : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TMP_Text youDiedText;
    [SerializeField] private TMP_Text timeAliveText;
    [Tooltip("{0} is replaced by the run time (00:00:00).")]
    [SerializeField] private string timeAliveFormat = "Time Alive:\n{0}";
    [SerializeField] private TMP_Text scoreText;
    [Tooltip("{0} is replaced by the global score this run ended with.")]
    [SerializeField] private string scoreFormat = "Score:\n{0}";

    [Header("Sound")]
    [Tooltip("CRT power-on sound played the moment the death screen loads.")]
    [SerializeField] private AudioClip tvOnSound;
    [SerializeField, Range(0f, 1f)] private float tvOnVolume = 0.8f;
    [Tooltip("Sound played a little after the scene starts.")]
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioSource audioSource;
    [SerializeField, Range(0f, 1f)] private float deathSoundVolume = 1f;
    [Tooltip("Seconds after the scene starts before the sound plays.")]
    [SerializeField] private float soundDelay = 0.5f;

    [Header("Music")]
    [Tooltip("AudioManager music state for the death theme.")]
    [SerializeField] private string deathMusicState = "player_death_theme";
    [Tooltip("Played here when there's no global AudioManager (e.g. Play pressed in a planet scene) " +
             "or it has no death theme assigned.")]
    [SerializeField] private AudioClip fallbackDeathTheme;
    [SerializeField, Range(0f, 1f)] private float fallbackMusicVolume = 1f;
    [Tooltip("Fade the death music out over the same time as the text fade-out.")]
    [SerializeField] private bool fadeMusicWithText = true;

    private bool usingFallbackMusic;

    [Header("Fades (seconds)")]
    [Tooltip("Delay after the scene starts before 'You Died' begins fading in.")]
    [SerializeField] private float youDiedDelay = 1.5f;
    [SerializeField] private float youDiedFadeDuration = 3f;
    [Tooltip("Delay after 'You Died' finishes before Time Alive begins fading in.")]
    [SerializeField] private float timeAliveDelay = 1f;
    [SerializeField] private float timeAliveFadeDuration = 3f;
    [Tooltip("Delay after Time Alive finishes before Score begins fading in.")]
    [SerializeField] private float scoreDelay = 0.5f;
    [SerializeField] private float scoreFadeDuration = 2f;
    [Tooltip("How long all texts stay fully visible.")]
    [SerializeField] private float holdDuration = 6f;
    [Tooltip("How long both texts take to fade out before the next scene loads.")]
    [SerializeField] private float fadeOutDuration = 5f;
    [Tooltip("Black screen pause after the fade-out, before loading.")]
    [SerializeField] private float endPause = 1f;

    [Header("Continue")]
    [Tooltip("Scene loaded once the sequence ends. Leave empty to stay on the death screen.")]
    [SerializeField] private string continueScene = "Main Menu";
    [Tooltip("AudioManager music state to switch to when continuing.")]
    [SerializeField] private string continueMusicState = "menustart";

    private void Start()
    {
        // Gameplay locks the cursor; free it on the death screen.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Freeze the run time (PlayerStats already does this; safe if the scene is opened directly).
        RunTimer.Stop();

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (tvOnSound != null)
            audioSource.PlayOneShot(tvOnSound, tvOnVolume * GameAudioSettings.Sfx);

        if (timeAliveText != null)
            timeAliveText.text = string.Format(timeAliveFormat, RunTimer.Format(RunTimer.Elapsed));

        if (scoreText != null)
            scoreText.text = string.Format(scoreFormat, EnemyStats.currentScore.ToString("D3"));

        SetAlpha(youDiedText, 0f);
        SetAlpha(timeAliveText, 0f);
        SetAlpha(scoreText, 0f);

        PlayDeathMusic();

        StartCoroutine(PlaySoundDelayed());
        StartCoroutine(RunSequence());
    }

    private void PlayDeathMusic()
    {
        // AudioManager only exists if the game was started from the Main Menu.
        AudioManager manager = AudioManager.instance;
        if (manager != null && manager.player_death_theme != null)
        {
            manager.TriggerMusicState(deathMusicState);
            return;
        }

        if (fallbackDeathTheme == null) return;
        if (manager != null) manager.TriggerMusicState("none"); // stop whatever it was playing

        usingFallbackMusic = true;
        audioSource.clip = fallbackDeathTheme;
        audioSource.volume = fallbackMusicVolume * GameAudioSettings.Music;
        audioSource.loop = true;
        audioSource.Play();
    }

    private IEnumerator FadeLocalMusic(float duration)
    {
        float start = audioSource.volume;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            audioSource.volume = Mathf.Lerp(start, 0f, t / duration);
            yield return null;
        }
        audioSource.Stop();
    }

    private IEnumerator PlaySoundDelayed()
    {
        yield return new WaitForSecondsRealtime(soundDelay);
        if (deathSound != null)
            audioSource.PlayOneShot(deathSound, deathSoundVolume * GameAudioSettings.Sfx);
    }

    // Fade in "You Died", then Time Alive, then Score; hold; slowly fade all out; then load the next scene.
    private IEnumerator RunSequence()
    {
        yield return new WaitForSecondsRealtime(youDiedDelay);
        yield return Fade(new[] { youDiedText }, 0f, 1f, youDiedFadeDuration);
        yield return new WaitForSecondsRealtime(timeAliveDelay);
        yield return Fade(new[] { timeAliveText }, 0f, 1f, timeAliveFadeDuration);
        yield return new WaitForSecondsRealtime(scoreDelay);
        yield return Fade(new[] { scoreText }, 0f, 1f, scoreFadeDuration);

        yield return new WaitForSecondsRealtime(holdDuration);

        // Music fades out together with the text.
        if (fadeMusicWithText)
        {
            if (usingFallbackMusic) StartCoroutine(FadeLocalMusic(fadeOutDuration));
            else if (AudioManager.instance != null) AudioManager.instance.FadeOutMusic(fadeOutDuration);
        }
        yield return Fade(new[] { youDiedText, timeAliveText, scoreText }, 1f, 0f, fadeOutDuration);
        yield return new WaitForSecondsRealtime(endPause);

        if (string.IsNullOrEmpty(continueScene)) yield break;

        if (AudioManager.instance != null && !string.IsNullOrEmpty(continueMusicState))
            AudioManager.instance.TriggerMusicState(continueMusicState);
        SceneManager.LoadScene(continueScene);
    }

    private static IEnumerator Fade(TMP_Text[] texts, float from, float to, float duration)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
            foreach (TMP_Text text in texts) SetAlpha(text, alpha);
            yield return null;
        }
        foreach (TMP_Text text in texts) SetAlpha(text, to);
    }

    private static void SetAlpha(TMP_Text text, float alpha)
    {
        if (text != null) text.alpha = alpha;
    }
}
