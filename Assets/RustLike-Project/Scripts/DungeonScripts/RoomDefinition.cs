using UnityEngine;

namespace ProceduralDungeons
{
    [CreateAssetMenu(
        fileName = "RoomDefinition",
        menuName = "Dungeon/Room Definition"
    )]
    public class RoomDefinition : ScriptableObject
    {
        [Header("Room")]
        public string displayName;
        public RoomType roomType;
        public GameObject roomPrefab;

        [Header("Selection")]
        [Min(0)] public int weight = 10;
        [Min(0)] public int minimumDepth = 0;
        [Min(0)] public int maximumDepth = 999;
    }
}
