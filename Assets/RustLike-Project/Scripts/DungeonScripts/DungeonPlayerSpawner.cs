using UnityEngine;
using ProceduralDungeons;

public class DungeonPlayerSpawner : MonoBehaviour
{
    [SerializeField] private DungeonGenerator dungeonGenerator;
    [SerializeField] private Transform player;

    private void Start()
    {
        // Generate the dungeon first.
        dungeonGenerator.GenerateDungeon();

        // Get the starting room's spawn point.
        Transform spawnPoint = dungeonGenerator.PlayerSpawnPoint;

        if (spawnPoint == null)
        {
            Debug.LogError("Player spawn point was not found!");
            return;
        }

        // Temporarily disable CharacterController if present.
        CharacterController controller =
            player.GetComponent<CharacterController>();

        if (controller != null)
            controller.enabled = false;

        // Move and rotate the player.
        player.SetPositionAndRotation(
            spawnPoint.position,
            spawnPoint.rotation
        );

        if (controller != null)
            controller.enabled = true;
    }
}