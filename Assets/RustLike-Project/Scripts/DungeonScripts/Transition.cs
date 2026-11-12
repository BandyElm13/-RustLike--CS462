

using UnityEngine;
using UnityEngine.SceneManagement;

public class Transition : MonoBehaviour
{
    [SerializeField] private string dungeonScene = "Dungeon";
    private float speed = 50f;
    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Scene Was Loaded");
            SceneManager.LoadScene(dungeonScene);
        }
    }

    void Update()
    {
        transform.Rotate(0, speed * Time.deltaTime, 0, Space.Self);
        transform.Rotate(1, speed * Time.deltaTime, 0, Space.Self);
    }
}