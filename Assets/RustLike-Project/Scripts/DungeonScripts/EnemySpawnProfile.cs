using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralDungeons
{
    [CreateAssetMenu(
        fileName = "NewEnemySpawnProfile",
        menuName = "Procedural Dungeons/Enemy Spawn Profile")]
    public class EnemySpawnProfile : ScriptableObject
    {
        [Serializable]
        public class EnemyEntry
        {
            public GameObject enemyPrefab;

            [Min(0)]
            public int weight = 1;
        }

        [Header("Enemy Selection")]
        public List<EnemyEntry> enemies = new List<EnemyEntry>();

        public GameObject GetRandomEnemy(System.Random random)
        {
            if (enemies == null || enemies.Count == 0)
                return null;

            int totalWeight = 0;

            foreach (EnemyEntry entry in enemies)
            {
                if (entry != null && entry.enemyPrefab != null)
                    totalWeight += Mathf.Max(0, entry.weight);
            }

            if (totalWeight <= 0)
                return null;

            int roll = random.Next(totalWeight);

            foreach (EnemyEntry entry in enemies)
            {
                if (entry == null || entry.enemyPrefab == null)
                    continue;

                roll -= Mathf.Max(0, entry.weight);

                if (roll < 0)
                    return entry.enemyPrefab;
            }

            return null;
        }
    }
}