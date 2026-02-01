// ============================================
// SPAWN SERVICE INTERFACE - Interface Segregation
// Defines contract for spawn management
// Separates spawn logic from GameManager
// ============================================

using System;
using UnityEngine;

namespace SpaceCombat.Interfaces
{
    /// <summary>
    /// Service interface for managing entity spawning.
    /// 
    /// SOLID Principles:
    /// - Single Responsibility: Only handles spawning logic
    /// - Interface Segregation: Focused interface for spawn operations
    /// - Dependency Inversion: GameManager depends on this abstraction
    /// </summary>
    public interface ISpawnService
    {
        /// <summary>
        /// Initialize the spawn service with configuration.
        /// </summary>
        void Initialize(Spawning.SpawnConfig config);
        
        /// <summary>
        /// Spawn initial enemies at game start.
        /// </summary>
        /// <param name="count">Number of enemies to spawn</param>
        /// <param name="playerPosition">Current player position to avoid</param>
        void SpawnInitialEnemies(int count, Vector3 playerPosition);
        
        /// <summary>
        /// Spawn a single enemy at a calculated position.
        /// </summary>
        /// <param name="playerPosition">Current player position to avoid</param>
        /// <returns>The spawned enemy GameObject, or null if failed</returns>
        GameObject SpawnEnemy(Vector3 playerPosition);
        
        /// <summary>
        /// Spawn an enemy at a specific position.
        /// </summary>
        /// <param name="position">Exact position to spawn at</param>
        /// <returns>The spawned enemy GameObject, or null if failed</returns>
        GameObject SpawnEnemyAt(Vector3 position);
        
        /// <summary>
        /// Return an enemy to the pool.
        /// </summary>
        void ReturnEnemy(GameObject enemy);
        
        /// <summary>
        /// Return all active enemies to the pool.
        /// </summary>
        void ReturnAllEnemies();
        
        /// <summary>
        /// Change the spawn strategy at runtime.
        /// </summary>
        void SetStrategy(ISpawnStrategy strategy);
        
        /// <summary>
        /// Change the spawn strategy by type.
        /// </summary>
        void SetStrategy(Spawning.SpawnDistributionType type);
        
        /// <summary>
        /// Update spawn bounds (e.g., when map changes).
        /// </summary>
        void SetBounds(Bounds bounds);
        
        /// <summary>
        /// Get current spawn bounds.
        /// </summary>
        Bounds GetBounds();
        
        /// <summary>
        /// Get current active enemy count.
        /// </summary>
        int ActiveEnemyCount { get; }
        
        /// <summary>
        /// Event fired when an enemy is spawned.
        /// </summary>
        event Action<GameObject, Vector3> OnEnemySpawned;
        
        /// <summary>
        /// Event fired when an enemy is returned to pool.
        /// </summary>
        event Action<GameObject> OnEnemyReturned;
    }
}
