// ============================================
// SPAWN CONFIG - ScriptableObject for spawn settings
// Data-driven configuration for spawn system
// Allows designers to tweak without code changes
// ============================================

using UnityEngine;

namespace SpaceCombat.Spawning
{
    /// <summary>
    /// ScriptableObject configuration for spawn system.
    /// 
    /// Benefits:
    /// - Data-driven design
    /// - Easy tweaking in Unity Editor
    /// - Multiple configs for different scenarios (waves, zones, difficulty)
    /// - No code changes needed for balance adjustments
    /// </summary>
    [CreateAssetMenu(fileName = "SpawnConfig", menuName = "SpaceCombat/Spawning/Spawn Config")]
    public class SpawnConfig : ScriptableObject
    {
        [Header("Distribution")]
        [Tooltip("How enemies should be distributed across the map")]
        public SpawnDistributionType DistributionType = SpawnDistributionType.UniformRandom;
        
        [Header("Strategy Parameters")]
        [Tooltip("Fine-tuning parameters for the selected strategy")]
        public SpawnStrategyConfig StrategyConfig = new SpawnStrategyConfig();
        
        [Header("Spawn Counts")]
        [Tooltip("Initial number of enemies to spawn")]
        [Range(1, 100)]
        public int InitialEnemyCount = 10;
        
        [Tooltip("Maximum enemies alive at once")]
        [Range(1, 200)]
        public int MaxEnemies = 50;
        
        [Header("Distances")]
        [Tooltip("Minimum distance from player for spawning")]
        [Range(5f, 100f)]
        public float MinDistanceFromPlayer = 20f;
        
        [Tooltip("Minimum spacing between spawned enemies")]
        [Range(1f, 20f)]
        public float MinSpacingBetweenEnemies = 5f;
        
        [Header("Timing")]
        [Tooltip("Delay before respawning after death")]
        [Range(0f, 30f)]
        public float RespawnDelay = 2f;
        
        [Tooltip("Should enemies respawn after death?")]
        public bool EnableRespawn = true;
        
        [Header("Bounds Override")]
        [Tooltip("If true, use custom bounds instead of MapBounds component")]
        public bool UseCustomBounds = false;
        
        [Tooltip("Custom spawn area bounds (only if UseCustomBounds is true)")]
        public Vector3 CustomBoundsCenter = Vector3.zero;
        
        [Tooltip("Custom spawn area size (only if UseCustomBounds is true)")]
        public Vector3 CustomBoundsSize = new Vector3(200f, 0f, 200f);
        
        /// <summary>
        /// Get the configured bounds for spawning.
        /// </summary>
        public Bounds GetBounds(Bounds defaultBounds)
        {
            if (UseCustomBounds)
            {
                return new Bounds(CustomBoundsCenter, CustomBoundsSize);
            }
            return defaultBounds;
        }
        
        /// <summary>
        /// Validate configuration values.
        /// </summary>
        private void OnValidate()
        {
            InitialEnemyCount = Mathf.Min(InitialEnemyCount, MaxEnemies);
            MinSpacingBetweenEnemies = Mathf.Min(MinSpacingBetweenEnemies, MinDistanceFromPlayer * 0.5f);
            
            if (CustomBoundsSize.x < 10f) CustomBoundsSize.x = 10f;
            if (CustomBoundsSize.z < 10f) CustomBoundsSize.z = 10f;
        }
    }
}
