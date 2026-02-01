// ============================================
// SPAWN STRATEGY FACTORY - Factory Pattern
// Creates spawn strategies based on configuration
// Centralizes strategy instantiation logic
// ============================================

using UnityEngine;
using SpaceCombat.Interfaces;

namespace SpaceCombat.Spawning
{
    /// <summary>
    /// Types of spawn distribution strategies available.
    /// Used by Factory to create appropriate strategy instance.
    /// </summary>
    public enum SpawnDistributionType
    {
        UniformRandom,
        Grid,
        Ring,
        Clustered,
        Edge,
        EdgeFarFromPlayer
    }
    
    /// <summary>
    /// Factory Pattern implementation for creating spawn strategies.
    /// 
    /// Benefits:
    /// - Centralizes creation logic
    /// - Easy to add new strategies
    /// - Decouples strategy creation from usage
    /// - Supports configuration-driven strategy selection
    /// </summary>
    public static class SpawnStrategyFactory
    {
        /// <summary>
        /// Create a spawn strategy based on the distribution type.
        /// </summary>
        /// <param name="type">The type of distribution strategy</param>
        /// <returns>Configured spawn strategy instance</returns>
        public static ISpawnStrategy Create(SpawnDistributionType type)
        {
            return type switch
            {
                SpawnDistributionType.UniformRandom => new UniformRandomSpawnStrategy(),
                SpawnDistributionType.Grid => new GridDistributionStrategy(),
                SpawnDistributionType.Ring => new RingDistributionStrategy(),
                SpawnDistributionType.Clustered => new ClusteredSpawnStrategy(),
                SpawnDistributionType.Edge => new EdgeSpawnStrategy(),
                SpawnDistributionType.EdgeFarFromPlayer => new EdgeSpawnStrategy(EdgeSpawnStrategy.EdgePreference.FarFromPlayer),
                _ => new UniformRandomSpawnStrategy()
            };
        }
        
        /// <summary>
        /// Create a spawn strategy with custom parameters.
        /// </summary>
        public static ISpawnStrategy Create(SpawnDistributionType type, SpawnStrategyConfig config)
        {
            return type switch
            {
                SpawnDistributionType.UniformRandom => new UniformRandomSpawnStrategy(),
                SpawnDistributionType.Grid => new GridDistributionStrategy(config.GridJitter),
                SpawnDistributionType.Ring => new RingDistributionStrategy(config.RingCount, config.InnerRadiusRatio),
                SpawnDistributionType.Clustered => new ClusteredSpawnStrategy(config.ClusterCount, config.ClusterRadius),
                SpawnDistributionType.Edge => new EdgeSpawnStrategy(config.EdgePreference, config.EdgeInset),
                SpawnDistributionType.EdgeFarFromPlayer => new EdgeSpawnStrategy(EdgeSpawnStrategy.EdgePreference.FarFromPlayer, config.EdgeInset),
                _ => new UniformRandomSpawnStrategy()
            };
        }
        
        /// <summary>
        /// Create strategy from ScriptableObject config.
        /// </summary>
        public static ISpawnStrategy CreateFromConfig(SpawnConfig config)
        {
            if (config == null)
            {
                Debug.LogWarning("[SpawnStrategyFactory] Null config provided, using UniformRandom");
                return new UniformRandomSpawnStrategy();
            }
            
            return Create(config.DistributionType, config.StrategyConfig);
        }
    }
    
    /// <summary>
    /// Configuration parameters for spawn strategies.
    /// Allows fine-tuning strategy behavior without code changes.
    /// </summary>
    [System.Serializable]
    public class SpawnStrategyConfig
    {
        [Header("Grid Strategy")]
        [Range(0f, 1f)]
        [Tooltip("Amount of random offset from grid positions (0 = exact grid, 1 = full cell random)")]
        public float GridJitter = 0.3f;
        
        [Header("Ring Strategy")]
        [Range(1, 10)]
        [Tooltip("Number of concentric rings")]
        public int RingCount = 3;
        
        [Range(0.1f, 0.9f)]
        [Tooltip("Inner ring radius as ratio of max radius")]
        public float InnerRadiusRatio = 0.3f;
        
        [Header("Cluster Strategy")]
        [Range(1, 20)]
        [Tooltip("Number of enemy clusters")]
        public int ClusterCount = 4;
        
        [Range(5f, 50f)]
        [Tooltip("Radius of each cluster")]
        public float ClusterRadius = 15f;
        
        [Header("Edge Strategy")]
        [Tooltip("Which edges to spawn from")]
        public EdgeSpawnStrategy.EdgePreference EdgePreference = EdgeSpawnStrategy.EdgePreference.All;
        
        [Range(0f, 20f)]
        [Tooltip("Distance from edge to spawn")]
        public float EdgeInset = 2f;
    }
}
