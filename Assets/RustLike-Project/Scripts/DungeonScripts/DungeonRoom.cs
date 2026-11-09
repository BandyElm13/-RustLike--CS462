using System.Collections.Generic;
using UnityEngine;

namespace ProceduralDungeons
{
    // Add this component to the root of every room prefab.
    public class DungeonRoom : MonoBehaviour
    {
        [Header("Optional gameplay points")]
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private Transform[] enemySpawnPoints;
        [SerializeField] private Transform[] treasureSpawnPoints;

        private RoomConnection[] connections;

        public Transform PlayerSpawnPoint =>
            playerSpawnPoint != null ? playerSpawnPoint : transform;

        public IReadOnlyList<Transform> EnemySpawnPoints => enemySpawnPoints;
        public IReadOnlyList<Transform> TreasureSpawnPoints => treasureSpawnPoints;

        private void Awake()
        {
            CacheConnections();
        }

        private void OnValidate()
        {
            CacheConnections();
        }

        private void CacheConnections()
        {
            connections = GetComponentsInChildren<RoomConnection>(true);
        }

        public bool HasConnection(DungeonDirection direction)
        {
            if (connections == null)
                CacheConnections();

            foreach (RoomConnection connection in connections)
            {
                if (connection != null && connection.direction == direction)
                    return true;
            }

            return false;
        }

        public IEnumerable<RoomConnection> GetConnections()
        {
            if (connections == null)
                CacheConnections();

            return connections;
        }

        // Override this in specialized room scripts (CombatRoom, ShopRoom, etc.)
        // to initialize room-specific gameplay after the dungeon is generated.
        public virtual void InitializeRoom(int depth, int dungeonSeed)
        {
        }
    }
}
