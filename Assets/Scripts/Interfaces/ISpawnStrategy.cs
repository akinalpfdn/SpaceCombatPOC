// ============================================
// SPAWN STRATEGY INTERFACE - Strategy Pattern
// Defines contract for spawn position algorithms
// Allows swapping spawn distribution methods at runtime
// ============================================

using UnityEngine;

namespace StarReapers.Interfaces
{
    /// <summary>
    /// Strategy Pattern interface for spawn position calculation.
    /// Different implementations provide different distribution algorithms.
    /// 
    /// SOLID Principles:
    /// - Interface Segregation: Focused on single responsibility
    /// - Open/Closed: New strategies can be added without modifying existing code
    /// - Dependency Inversion: High-level modules depend on this abstraction
    /// </summary>
    public interface ISpawnStrategy
    {
        /// <summary>
        /// Calculate a spawn position within the given bounds.
        /// </summary>
        /// <param name="bounds">The area bounds for spawning</param>
        /// <param name="excludePosition">Position to avoid (typically player position)</param>
        /// <param name="minDistance">Minimum distance from exclude position</param>
        /// <returns>Calculated spawn position on XZ plane</returns>
        Vector3 GetSpawnPosition(Bounds bounds, Vector3 excludePosition, float minDistance);
        
        /// <summary>
        /// Calculate multiple spawn positions with proper distribution.
        /// </summary>
        /// <param name="bounds">The area bounds for spawning</param>
        /// <param name="count">Number of positions to generate</param>
        /// <param name="excludePosition">Position to avoid</param>
        /// <param name="minDistance">Minimum distance from exclude position</param>
        /// <param name="minSpacing">Minimum distance between spawned entities</param>
        /// <returns>Array of spawn positions</returns>
        Vector3[] GetSpawnPositions(Bounds bounds, int count, Vector3 excludePosition, 
            float minDistance, float minSpacing);
        
        /// <summary>
        /// Validate if a position is suitable for spawning.
        /// </summary>
        bool IsValidSpawnPosition(Vector3 position, Vector3 excludePosition, float minDistance);
        
        /// <summary>
        /// Strategy identifier for debugging and configuration.
        /// </summary>
        string StrategyName { get; }
    }
}
