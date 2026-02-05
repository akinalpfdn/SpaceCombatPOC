// ============================================
// MAP CONFIG - Main configuration ScriptableObject for each map
// Composite pattern: References SpawnConfig for parameters, defines enemy composition
// Design Pattern: Composition over Inheritance
// ============================================

using System.Linq;
using UnityEngine;
using StarReapers.Spawning;
using StarReapers.Entities;

namespace StarReapers.Maps
{
    /// <summary>
    /// Main configuration for a game map.
    ///
    /// This ScriptableObject follows the Composite Pattern:
    /// - References existing SpawnConfig for spawn parameters (how to spawn)
    /// - Defines which enemies spawn and their weights (what to spawn)
    /// - Configures boss spawning separately
    ///
    /// Benefits:
    /// - Single source of truth for each map's configuration
    /// - Easy to create new maps by duplicating and modifying
    /// - Separation of concerns: spawn logic vs map content
    /// </summary>
    [CreateAssetMenu(fileName = "NewMapConfig", menuName = "StarReapers/Maps/Map Config")]
    public class MapConfig : ScriptableObject
    {
        // ============================================
        // MAP IDENTITY
        // ============================================

        [Header("Map Identity")]
        [Tooltip("Display name of the map")]
        public string mapName = "New Map";

        [Tooltip("Map index for progression/unlocking (1-based)")]
        [Range(1, 100)]
        public int mapIndex = 1;

        [Tooltip("Brief description shown in map selection")]
        [TextArea(2, 4)]
        public string description = "";

        // ============================================
        // SPAWN CONFIGURATION
        // ============================================

        [Header("Spawn Settings")]
        [Tooltip("Reference to spawn parameters (distribution, timing, bounds)")]
        public SpawnConfig spawnConfig;

        // ============================================
        // ENEMY COMPOSITION
        // ============================================

        [Header("Enemy Composition")]
        [Tooltip("List of enemies that spawn in this map with their weights")]
        public EnemySpawnEntry[] enemies = new EnemySpawnEntry[0];

        // ============================================
        // BOSS SETTINGS
        // ============================================

        [Header("Boss Settings")]
        [Tooltip("Configuration for boss enemy spawning")]
        public BossSpawnSettings bossSettings = new BossSpawnSettings();

        // ============================================
        // COMPUTED PROPERTIES
        // ============================================

        /// <summary>
        /// Total weight of all enemy entries for probability calculation.
        /// </summary>
        public int TotalWeight => enemies?.Where(e => e.IsValid).Sum(e => e.spawnWeight) ?? 0;

        /// <summary>
        /// Number of valid enemy types configured.
        /// </summary>
        public int EnemyTypeCount => enemies?.Count(e => e.IsValid) ?? 0;

        /// <summary>
        /// Whether this map configuration is valid and ready to use.
        /// </summary>
        public bool IsValid => spawnConfig != null && EnemyTypeCount > 0;

        // ============================================
        // PUBLIC METHODS
        // ============================================

        /// <summary>
        /// Selects a random enemy prefab based on configured weights.
        /// Uses weighted random selection algorithm.
        /// </summary>
        /// <returns>Selected enemy prefab, or null if no valid entries</returns>
        public Enemy GetRandomEnemyPrefab()
        {
            if (enemies == null || enemies.Length == 0)
                return null;

            var validEntries = enemies.Where(e => e.IsValid).ToArray();
            if (validEntries.Length == 0)
                return null;

            int totalWeight = validEntries.Sum(e => e.spawnWeight);
            if (totalWeight <= 0)
                return validEntries[0].enemyPrefab;

            int randomValue = Random.Range(0, totalWeight);
            int currentWeight = 0;

            foreach (var entry in validEntries)
            {
                currentWeight += entry.spawnWeight;
                if (randomValue < currentWeight)
                {
                    return entry.enemyPrefab;
                }
            }

            // Fallback (should not reach here)
            return validEntries[validEntries.Length - 1].enemyPrefab;
        }

        /// <summary>
        /// Gets a specific enemy prefab by index.
        /// </summary>
        public Enemy GetEnemyPrefab(int index)
        {
            if (enemies == null || index < 0 || index >= enemies.Length)
                return null;

            return enemies[index].enemyPrefab;
        }

        /// <summary>
        /// Calculates boss spawn position based on map bounds and configured corner.
        /// </summary>
        /// <param name="mapBounds">The map bounds to calculate position from</param>
        /// <returns>World position for boss spawn</returns>
        public Vector3 GetBossSpawnPosition(Bounds mapBounds)
        {
            if (!bossSettings.enabled)
                return Vector3.zero;

            SpawnCorner corner = bossSettings.spawnCorner;

            // Handle random corner selection
            if (corner == SpawnCorner.Random)
            {
                corner = (SpawnCorner)Random.Range(0, 4);
            }

            float offset = bossSettings.cornerOffset;
            Vector3 min = mapBounds.min;
            Vector3 max = mapBounds.max;

            Vector3 position = corner switch
            {
                SpawnCorner.TopLeft => new Vector3(min.x + offset, 0f, max.z - offset),
                SpawnCorner.TopRight => new Vector3(max.x - offset, 0f, max.z - offset),
                SpawnCorner.BottomLeft => new Vector3(min.x + offset, 0f, min.z + offset),
                SpawnCorner.BottomRight => new Vector3(max.x - offset, 0f, min.z + offset),
                _ => mapBounds.center
            };

            return position;
        }

        // ============================================
        // VALIDATION
        // ============================================

        private void OnValidate()
        {
            // Ensure map index is at least 1
            if (mapIndex < 1) mapIndex = 1;

            // Validate boss settings
            if (bossSettings.enabled && bossSettings.bossPrefab == null)
            {
                Debug.LogWarning($"[MapConfig] {mapName}: Boss is enabled but no prefab assigned!");
            }

            // Validate enemy entries
            if (enemies != null)
            {
                int validCount = enemies.Count(e => e.IsValid);
                if (validCount == 0)
                {
                    Debug.LogWarning($"[MapConfig] {mapName}: No valid enemy entries configured!");
                }
            }
        }
    }
}
