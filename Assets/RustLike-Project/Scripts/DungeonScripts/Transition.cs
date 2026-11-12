

using UnityEngine;
using UnityEngine.SceneManagement;

public class Transition : MonoBehaviour
{
    [SerializeField] private string dungeonScene = "Dungeon";
    [Tooltip("Spin the object in place (the original floating cube). Turn off for teleporters placed on a planet.")]
    [SerializeField] private bool spin = true;
    private float speed = 50f;
    private bool loading;

    public void OnTriggerEnter(Collider other)
    {
        // Player tag lives on the root; colliders may be on children.
        Transform root = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform;
        if (!loading && (other.CompareTag("Player") || root.CompareTag("Player")))
        {
            loading = true;
            Debug.Log("Scene Was Loaded");
            SceneManager.LoadScene(dungeonScene);
        }
    }

    void Update()
    {
        if (!spin) return;
        transform.Rotate(0, speed * Time.deltaTime, 0, Space.Self);
        transform.Rotate(1, speed * Time.deltaTime, 0, Space.Self);
    }
}