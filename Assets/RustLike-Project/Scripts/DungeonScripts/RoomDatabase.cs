using System.Collections.Generic;
using UnityEngine;

namespace ProceduralDungeons
{
    [CreateAssetMenu(
        fileName = "RoomDatabase",
        menuName = "Dungeon/Room Database"
    )]
    public class RoomDatabase : ScriptableObject
    {
        public List<RoomDefinition> rooms = new List<RoomDefinition>();
    }
}
