using UnityEngine;

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

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

        void Start()
    {
        // The exact millisecond the game boots into the Main Menu, turn on the menu theme!
        TriggerMusicState("menustart");
    }


    // THE GLOBAL TRIGGER
    public void TriggerMusicState(string situationState)
    {
        AudioClip selectedClip = null;

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
        }

        if (musicSource.clip != selectedClip)
        {
            musicSource.Stop();
            musicSource.clip = selectedClip;
            if (selectedClip != null)
            {
                musicSource.Play();
            }
        }
    }
}
