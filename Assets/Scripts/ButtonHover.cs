using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private string prefix = "[ ";
    [SerializeField] private string suffix = " ]";

    private TMP_Text label;
    private string originalText;
    private bool highlighted;

    private void Awake()
    {
        label = GetComponentInChildren<TMP_Text>();
        originalText = label.text;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void OnPointerEnter(PointerEventData eventData) => Highlight();
    public void OnPointerExit(PointerEventData eventData) => Unhighlight();
    public void OnSelect(BaseEventData eventData) => Highlight();
    public void OnDeselect(BaseEventData eventData) => Unhighlight();

    private void OnDisable()
    {
        Unhighlight();
    }

    private void Highlight()
    {
        if (highlighted) return;
        highlighted = true;

        label.text = prefix + originalText + suffix;

        if (hoverSound != null && audioSource != null)
            audioSource.PlayOneShot(hoverSound);
    }

    private void Unhighlight()
    {
        if (!highlighted) return;
        highlighted = false;

        label.text = originalText;
    }
}
