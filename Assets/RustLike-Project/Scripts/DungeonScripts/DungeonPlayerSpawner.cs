using UnityEngine;
using ProceduralDungeons;

// Moves an existing scene player to the dungeon's start room.
// If DungeonGenerator already spawns the player (Spawn Player After Generation),
// leave Player empty — this script then only makes sure the dungeon exists.
public class DungeonPlayerSpawner : MonoBehaviour
{
    [SerializeField] private DungeonGenerator dungeonGenerator;
    [Tooltip("A player object in this scene. Do NOT assign the Player prefab asset here.")]
    [SerializeField] private Transform player;

    private void Start()
    {
        if (dungeonGenerator == null)
        {
            Debug.LogError("DungeonPlayerSpawner: Dungeon Generator is not assigned.", this);
            return;
        }

        // Generate only once, whichever Start runs first.
        if (dungeonGenerator.SpawnedRooms.Count == 0)
            dungeonGenerator.GenerateDungeon();

        if (player == null)
            return;

        // A prefab asset reference has no valid scene; moving it would edit the asset.
        if (!player.gameObject.scene.IsValid())
        {
            Debug.LogWarning(
                "DungeonPlayerSpawner: Player references a prefab asset, not a scene object — skipping. " +
                "DungeonGenerator spawns the player itself when Spawn Player After Generation is on.",
                this
            );
            return;
        }

        Transform spawnPoint = dungeonGenerator.PlayerSpawnPoint;
        if (spawnPoint == null)
        {
            Debug.LogError("Player spawn point was not found!", this);
            return;
        }

        Rigidbody body = player.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.position = spawnPoint.position;
            body.rotation = spawnPoint.rotation;
            body.linearVelocity = Vector3.zero;
        }

        player.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
    }
}
