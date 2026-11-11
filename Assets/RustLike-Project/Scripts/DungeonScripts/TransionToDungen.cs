

using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionToDungen : MonoBehaviour
{
    [SerializeField] private string dungeonScene = "Dungeon";
    private float speed = 50f;
    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SceneManager.LoadScene(dungeonScene);
        }
    }

    void Update()
    {
        transform.Rotate(0, speed * Time.deltaTime, 0, Space.Self);
    }
}