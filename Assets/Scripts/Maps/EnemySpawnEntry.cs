// ============================================
// ENEMY SPAWN ENTRY - Data class for weighted enemy spawning
// Used by MapConfig to define which enemies spawn and their weights
// ============================================

using System;
using UnityEngine;
using StarReapers.Entities;

namespace StarReapers.Maps
{
    /// <summary>
    /// Defines an enemy type that can spawn in a map along with its spawn weight.
    ///
    /// Weight System:
    /// - Higher weight = higher spawn probability relative to other entries
    /// - Example: Weight 30 vs Weight 10 = 3x more likely to spawn
    /// - MaxCount of 0 means unlimited concurrent spawns
    /// </summary>
    [Serializable]
    public class EnemySpawnEntry
    {
        [Tooltip("The enemy prefab to spawn")]
        public Enemy enemyPrefab;

        [Tooltip("Relative spawn weight (higher = more common)")]
        [Range(1, 100)]
        public int spawnWeight = 10;

        [Tooltip("Maximum concurrent enemies of this type (0 = unlimited)")]
        [Range(0, 50)]
        public int maxConcurrent = 0;

        /// <summary>
        /// Validates the entry has required references.
        /// </summary>
        public bool IsValid => enemyPrefab != null;
    }
}
