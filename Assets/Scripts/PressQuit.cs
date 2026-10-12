using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PressQuit : MonoBehaviour
{
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioSource audioSource;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void OnClick()
    {
        StartCoroutine(PlayThenQuit());
    }

    private IEnumerator PlayThenQuit()
    {
        // Block repeat clicks while the sound plays
        button.interactable = false;

        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
            // Quitting would cut the sound off, so wait for it to finish
            yield return new WaitForSecondsRealtime(clickSound.length);
        }

#if UNITY_EDITOR
        // Application.Quit is ignored in the editor, so stop play mode instead
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
