using UnityEngine;

namespace ProceduralDungeons
{
    [CreateAssetMenu(
        fileName = "DungeonSettings",
        menuName = "Dungeon/Dungeon Settings"
    )]
    public class DungeonSettings : ScriptableObject
    {
        [Header("Layout")]
        [Min(1)] public int roomCount = 12;

        // Distance between room prefab origins. Set this to the room's
        // exact outside width (X) and depth (Y), including walls.
        public Vector2 roomSpacing = new Vector2(20f, 20f);

        [Header("Randomization")]
        public bool useRandomSeed = true;
        public int seed = 12345;
    }
}
