using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PressPlay : MonoBehaviour
{
    // Must match a scene name added to File > Build Profiles (Scene List)
    [SerializeField] private string sceneToLoad = "GameScene";
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
        StartCoroutine(PlayThenLoad());
    }

    private IEnumerator PlayThenLoad()
    {
        // Block repeat clicks while the sound plays
        button.interactable = false;

        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
            // Loading the scene would cut the sound off, so wait for it to finish
            yield return new WaitForSecondsRealtime(clickSound.length);
        }

        SceneManager.LoadScene(sceneToLoad);
    }
}
