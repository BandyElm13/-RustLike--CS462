using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProceduralDungeons
{
    public class DungeonGenerator : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private DungeonSettings settings;
        [SerializeField] private RoomDatabase roomDatabase;

        [Header("Door handling")]
        [Tooltip("Optional prefab used to block room exits that do not connect to another room.")]
        [SerializeField] private GameObject closedDoorPrefab;

        [Header("Generation")]
        [SerializeField] private bool generateOnStart = true;
        [SerializeField] private Transform generatedRoomsParent;

        [Header("Player Spawning")]
        [SerializeField] private bool spawnPlayerAfterGeneration = true;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform playerParent;

        private GameObject spawnedPlayer;

        private readonly Dictionary<Vector2Int, DungeonRoom> spawnedRooms =
            new Dictionary<Vector2Int, DungeonRoom>();

        // Number of clockwise 90-degree turns applied to each room prefab.
        // This is also used when checking which exits are connected to neighbors.
        private readonly Dictionary<Vector2Int, int> spawnedRoomRotations =
            new Dictionary<Vector2Int, int>();

        private static readonly DungeonDirection[] Directions =
        {
            DungeonDirection.North,
            DungeonDirection.East,
            DungeonDirection.South,
            DungeonDirection.West
        };

        private class RoomCandidate
        {
            public RoomDefinition definition;
            public List<int> validRotations = new List<int>();
        }

        public int GeneratedSeed { get; private set; }
        public Transform PlayerSpawnPoint { get; private set; }
        public IReadOnlyDictionary<Vector2Int, DungeonRoom> SpawnedRooms => spawnedRooms;

        private void Start()
        {
            if (generateOnStart)
                GenerateDungeon();
        }

        [ContextMenu("Generate Dungeon")]
        public void GenerateDungeon()
        {
            if (!ValidateConfiguration())
                return;

            ClearDungeon();

            GeneratedSeed = settings.useRandomSeed
                ? UnityEngine.Random.Range(int.MinValue, int.MaxValue)
                : settings.seed;

            System.Random random = new System.Random(GeneratedSeed);

            HashSet<Vector2Int> layout = BuildLayout(settings.roomCount, random);
            Dictionary<Vector2Int, int> depths = CalculateDepths(layout);

            Vector2Int bossPosition = FindFarthestPosition(depths);
            List<Vector2Int> orderedPositions = layout
                .OrderBy(position => depths[position])
                .ToList();

            // Assign and instantiate rooms only after the complete layout exists.
            foreach (Vector2Int position in orderedPositions)
            {
                RoomType requestedType;

                if (position == Vector2Int.zero)
                    requestedType = RoomType.Start;
                else if (position == bossPosition && layout.Count > 1)
                    requestedType = RoomType.Boss;
                else
                    requestedType = RoomType.Combat;

                RoomCandidate candidate = SelectRoomDefinition(
                    position,
                    layout,
                    depths[position],
                    requestedType,
                    random
                );

                if (candidate == null)
                {
                    Debug.LogError(
                        $"No compatible room definition found for cell {position}. " +
                        $"Check that the Room Database contains prefabs with the required door connections " +
                        $"and that their connections can be aligned by rotating the prefab.",
                        this
                    );
                    ClearDungeon();
                    return;
                }

                Vector3 localPosition = new Vector3(
                    position.x * settings.roomSpacing.x,
                    0f,
                    position.y * settings.roomSpacing.y
                );

                Vector3 worldPosition = transform.TransformPoint(localPosition);
                // Keep the prefab's authored orientation when it already fits.
                // Only choose a rotated orientation when the default cannot connect.
                int selectedRotation = candidate.validRotations.Contains(0)
                    ? 0
                    : candidate.validRotations[random.Next(candidate.validRotations.Count)];
                Quaternion roomRotation = transform.rotation *
                    Quaternion.Euler(0f, selectedRotation * 90f, 0f);

                DungeonRoom room = Instantiate(
                    candidate.definition.roomPrefab,
                    worldPosition,
                    roomRotation,
                    generatedRoomsParent != null ? generatedRoomsParent : transform
                ).GetComponent<DungeonRoom>();

                if (room == null)
                {
                    Debug.LogError(
                        $"Prefab '{candidate.definition.name}' is missing a DungeonRoom component on its root.",
                        candidate.definition
                    );
                    ClearDungeon();
                    return;
                }

                spawnedRooms.Add(position, room);
                spawnedRoomRotations.Add(position, selectedRotation);
                room.InitializeRoom(depths[position], GeneratedSeed);

                if (position == Vector2Int.zero)
                    PlayerSpawnPoint = room.PlayerSpawnPoint;
            }

            CloseUnusedConnections(layout);

            // Spawn enemies after all rooms have been generated.
            foreach (Vector2Int position in orderedPositions)
            {
                spawnedRooms[position].SpawnEnemies(random);
            }

            if (spawnPlayerAfterGeneration){
                SpawnPlayer();
            }

            Debug.Log(
                $"Generated dungeon with {spawnedRooms.Count} rooms. Seed: {GeneratedSeed}",
                this
            );
        }

        public void ClearDungeon()
        {
            if (spawnedPlayer != null)
            {
                if (Application.isPlaying)
                    Destroy(spawnedPlayer);
                else
                    DestroyImmediate(spawnedPlayer);

                spawnedPlayer = null;
            }

            foreach (DungeonRoom room in spawnedRooms.Values)
            {
                if (room == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(room.gameObject);
                else
                    DestroyImmediate(room.gameObject);
            }

            spawnedRooms.Clear();
            spawnedRoomRotations.Clear();
            PlayerSpawnPoint = null;
        }

        private void SpawnPlayer()
        {
            if (playerPrefab == null)
            {
                Debug.LogError(
                    "Player Prefab is not assigned. Assign your player prefab in the Dungeon Generator inspector.",
                    this
                );
                return;
            }

            if (PlayerSpawnPoint == null)
            {
                Debug.LogError(
                    "Player Spawn Point is missing. Assign the PlayerSpawnPoint child in the starting room's DungeonRoom component.",
                    this
                );
                return;
            }

            spawnedPlayer = Instantiate(
                playerPrefab,
                PlayerSpawnPoint.position,
                PlayerSpawnPoint.rotation,
                playerParent
            );

            spawnedPlayer.name = playerPrefab.name;

            GravityBody gravityBody = spawnedPlayer.GetComponent<GravityBody>();

            if (gravityBody != null)
            {
                gravityBody.useWorldGravity = true;
                gravityBody.attractor = null;
            }
        }

        private bool ValidateConfiguration()
        {
            if (settings == null)
            {
                Debug.LogError("Dungeon Settings has not been assigned.", this);
                return false;
            }

            if (roomDatabase == null || roomDatabase.rooms == null ||
                roomDatabase.rooms.Count == 0)
            {
                Debug.LogError("Room Database is missing or contains no room definitions.", this);
                return false;
            }

            if (settings.roomSpacing.x <= 0f || settings.roomSpacing.y <= 0f)
            {
                Debug.LogError("Room spacing must be greater than zero.", this);
                return false;
            }

            foreach (RoomDefinition definition in roomDatabase.rooms)
            {
                if (definition == null || definition.roomPrefab == null)
                    continue;

                if (definition.roomPrefab.GetComponent<DungeonRoom>() == null)
                {
                    Debug.LogError(
                        $"Room prefab '{definition.roomPrefab.name}' needs DungeonRoom on its root.",
                        definition.roomPrefab
                    );
                    return false;
                }
            }

            return true;
        }

        // Creates a connected, branching tree of grid cells. Each new cell
        // touches exactly one existing cell, so the first version has no loops.
        private HashSet<Vector2Int> BuildLayout(int targetCount, System.Random random)
        {
            HashSet<Vector2Int> cells = new HashSet<Vector2Int>
            {
                Vector2Int.zero
            };

            while (cells.Count < targetCount)
            {
                List<Vector2Int> frontier = new List<Vector2Int>();

                foreach (Vector2Int cell in cells)
                {
                    foreach (DungeonDirection direction in Directions)
                    {
                        Vector2Int candidate = cell + DirectionOffset(direction);

                        if (cells.Contains(candidate))
                            continue;

                        // Keep the initial layout as a tree: every new room
                        // connects to exactly one already-placed room.
                        if (CountOccupiedNeighbors(candidate, cells) == 1)
                            frontier.Add(candidate);
                    }
                }

                if (frontier.Count == 0)
                    break;

                Vector2Int next = frontier[random.Next(frontier.Count)];
                cells.Add(next);
            }

            return cells;
        }

        private int CountOccupiedNeighbors(Vector2Int cell, HashSet<Vector2Int> cells)
        {
            int count = 0;

            foreach (DungeonDirection direction in Directions)
            {
                if (cells.Contains(cell + DirectionOffset(direction)))
                    count++;
            }

            return count;
        }

        private Dictionary<Vector2Int, int> CalculateDepths(HashSet<Vector2Int> layout)
        {
            Dictionary<Vector2Int, int> depths = new Dictionary<Vector2Int, int>();
            Queue<Vector2Int> queue = new Queue<Vector2Int>();

            depths[Vector2Int.zero] = 0;
            queue.Enqueue(Vector2Int.zero);

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();

                foreach (DungeonDirection direction in Directions)
                {
                    Vector2Int neighbor = current + DirectionOffset(direction);

                    if (!layout.Contains(neighbor) || depths.ContainsKey(neighbor))
                        continue;

                    depths[neighbor] = depths[current] + 1;
                    queue.Enqueue(neighbor);
                }
            }

            return depths;
        }

        private Vector2Int FindFarthestPosition(Dictionary<Vector2Int, int> depths)
        {
            Vector2Int farthest = Vector2Int.zero;
            int greatestDepth = -1;

            foreach (KeyValuePair<Vector2Int, int> pair in depths)
            {
                if (pair.Value > greatestDepth)
                {
                    greatestDepth = pair.Value;
                    farthest = pair.Key;
                }
            }

            return farthest;
        }

        private RoomCandidate SelectRoomDefinition(
            Vector2Int position,
            HashSet<Vector2Int> layout,
            int depth,
            RoomType requestedType,
            System.Random random)
        {
            List<RoomCandidate> compatible = new List<RoomCandidate>();

            foreach (RoomDefinition definition in roomDatabase.rooms)
            {
                if (definition == null || definition.roomPrefab == null)
                    continue;

                if (depth < definition.minimumDepth || depth > definition.maximumDepth)
                    continue;

                DungeonRoom prefabRoom = definition.roomPrefab.GetComponent<DungeonRoom>();
                if (prefabRoom == null)
                    continue;

                RoomCandidate candidate = new RoomCandidate
                {
                    definition = definition
                };

                // Test all four cardinal orientations. Store each valid rotation
                // so the selected room can be instantiated in a matching orientation.
                for (int rotation = 0; rotation < 4; rotation++)
                {
                    if (HasRequiredConnections(prefabRoom, position, layout, rotation))
                        candidate.validRotations.Add(rotation);
                }

                if (candidate.validRotations.Count > 0)
                    compatible.Add(candidate);
            }

            List<RoomCandidate> preferred = compatible
                .Where(candidate => candidate.definition.roomType == requestedType)
                .ToList();

            // For ordinary rooms, allow any non-special room if no Combat room
            // matches the required doorway pattern.
            if (preferred.Count == 0 && requestedType == RoomType.Combat)
            {
                preferred = compatible
                    .Where(candidate =>
                        candidate.definition.roomType != RoomType.Start &&
                        candidate.definition.roomType != RoomType.Boss)
                    .ToList();
            }

            return ChooseWeighted(preferred, random);
        }

        private bool HasRequiredConnections(
            DungeonRoom room,
            Vector2Int position,
            HashSet<Vector2Int> layout,
            int clockwiseQuarterTurns)
        {
            HashSet<DungeonDirection> availableDirections = new HashSet<DungeonDirection>();

            foreach (RoomConnection connection in room.GetConnections())
            {
                if (connection == null)
                    continue;

                availableDirections.Add(
                    RotateDirection(connection.direction, clockwiseQuarterTurns)
                );
            }

            foreach (DungeonDirection requiredDirection in Directions)
            {
                Vector2Int neighbor = position + DirectionOffset(requiredDirection);

                if (layout.Contains(neighbor) && !availableDirections.Contains(requiredDirection))
                    return false;
            }

            return true;
        }

        private RoomCandidate ChooseWeighted(
            List<RoomCandidate> candidates,
            System.Random random)
        {
            if (candidates == null || candidates.Count == 0)
                return null;

            // Each room definition is counted once, regardless of how many
            // rotations are valid. This prevents four-door rooms receiving
            // extra weight merely because they have more valid orientations.
            int totalWeight = candidates.Sum(
                candidate => Mathf.Max(0, candidate.definition.weight)
            );

            if (totalWeight <= 0)
                return candidates[random.Next(candidates.Count)];

            int roll = random.Next(totalWeight);

            foreach (RoomCandidate candidate in candidates)
            {
                roll -= Mathf.Max(0, candidate.definition.weight);

                if (roll < 0)
                    return candidate;
            }

            return candidates[candidates.Count - 1];
        }

        private void CloseUnusedConnections(HashSet<Vector2Int> layout)
        {
            foreach (KeyValuePair<Vector2Int, DungeonRoom> pair in spawnedRooms)
            {
                int rotation = spawnedRoomRotations[pair.Key];

                foreach (RoomConnection connection in pair.Value.GetConnections())
                {
                    if (connection == null)
                        continue;

                    DungeonDirection worldDirection = RotateDirection(
                        connection.direction,
                        rotation
                    );

                    Vector2Int neighbor = pair.Key + DirectionOffset(worldDirection);

                    if (layout.Contains(neighbor))
                        continue;

                    if (closedDoorPrefab != null)
                    {
                        Instantiate(
                            closedDoorPrefab,
                            connection.transform.position,
                            connection.transform.rotation,
                            pair.Value.transform
                        );
                    }
                }
            }
        }

        // A positive 90-degree Y rotation maps North (+Z) to East (+X).
        private static DungeonDirection RotateDirection(
            DungeonDirection direction,
            int clockwiseQuarterTurns)
        {
            int turns = ((clockwiseQuarterTurns % 4) + 4) % 4;

            for (int i = 0; i < turns; i++)
            {
                switch (direction)
                {
                    case DungeonDirection.North:
                        direction = DungeonDirection.East;
                        break;
                    case DungeonDirection.East:
                        direction = DungeonDirection.South;
                        break;
                    case DungeonDirection.South:
                        direction = DungeonDirection.West;
                        break;
                    case DungeonDirection.West:
                        direction = DungeonDirection.North;
                        break;
                }
            }

            return direction;
        }

        private static Vector2Int DirectionOffset(DungeonDirection direction)
        {
            switch (direction)
            {
                case DungeonDirection.North: return Vector2Int.up;
                case DungeonDirection.East:  return Vector2Int.right;
                case DungeonDirection.South: return Vector2Int.down;
                case DungeonDirection.West:  return Vector2Int.left;
                default: return Vector2Int.zero;
            }
        }
    }
}
