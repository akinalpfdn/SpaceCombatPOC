// ============================================
// SPAWN STRATEGY BASE - Template Method Pattern
// Provides common functionality for spawn strategies
// Concrete strategies override specific algorithms
// ============================================

using System.Collections.Generic;
using UnityEngine;
using StarReapers.Interfaces;

namespace StarReapers.Spawning
{
    /// <summary>
    /// Base class for spawn strategies implementing Template Method Pattern.
    /// Provides common validation and utility methods.
    /// 
    /// Design Patterns:
    /// - Template Method: Defines skeleton algorithm, subclasses override specific steps
    /// - Strategy: Concrete implementations provide different distribution algorithms
    /// </summary>
    public abstract class SpawnStrategyBase : ISpawnStrategy
    {
        protected const int MAX_ATTEMPTS = 100;
        protected const float DEFAULT_Y_POSITION = 0f;
        
        public abstract string StrategyName { get; }
        
        /// <summary>
        /// Template method for getting a single spawn position.
        /// Concrete strategies override CalculatePosition().
        /// </summary>
        public virtual Vector3 GetSpawnPosition(Bounds bounds, Vector3 excludePosition, float minDistance)
        {
            for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
            {
                Vector3 candidate = CalculatePosition(bounds);
                
                if (IsValidSpawnPosition(candidate, excludePosition, minDistance))
                {
                    return candidate;
                }
            }
            
            // Fallback: Return edge position away from exclude position
            return GetFallbackPosition(bounds, excludePosition);
        }
        
        /// <summary>
        /// Template method for getting multiple spawn positions with distribution.
        /// </summary>
        public virtual Vector3[] GetSpawnPositions(Bounds bounds, int count, Vector3 excludePosition, 
            float minDistance, float minSpacing)
        {
            var positions = new List<Vector3>(count);
            var occupiedPositions = new List<Vector3>();
            
            for (int i = 0; i < count; i++)
            {
                Vector3 position = GetDistributedPosition(bounds, excludePosition, minDistance, 
                    occupiedPositions, minSpacing);
                
                positions.Add(position);
                occupiedPositions.Add(position);
            }
            
            return positions.ToArray();
        }
        
        /// <summary>
        /// Abstract method - concrete strategies implement their distribution algorithm.
        /// </summary>
        protected abstract Vector3 CalculatePosition(Bounds bounds);
        
        /// <summary>
        /// Validates spawn position against exclusion zone.
        /// </summary>
        public virtual bool IsValidSpawnPosition(Vector3 position, Vector3 excludePosition, float minDistance)
        {
            // Calculate 2D distance on XZ plane
            float dx = position.x - excludePosition.x;
            float dz = position.z - excludePosition.z;
            float distanceSquared = dx * dx + dz * dz;
            
            return distanceSquared >= minDistance * minDistance;
        }
        
        /// <summary>
        /// Check if position is far enough from all occupied positions.
        /// </summary>
        protected bool IsSpacedFromOthers(Vector3 position, List<Vector3> occupiedPositions, float minSpacing)
        {
            float minSpacingSquared = minSpacing * minSpacing;
            
            foreach (var occupied in occupiedPositions)
            {
                float dx = position.x - occupied.x;
                float dz = position.z - occupied.z;
                float distanceSquared = dx * dx + dz * dz;
                
                if (distanceSquared < minSpacingSquared)
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Get a position that's distributed away from both player and other spawns.
        /// </summary>
        protected virtual Vector3 GetDistributedPosition(Bounds bounds, Vector3 excludePosition, 
            float minDistance, List<Vector3> occupiedPositions, float minSpacing)
        {
            for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
            {
                Vector3 candidate = CalculatePosition(bounds);
                
                if (IsValidSpawnPosition(candidate, excludePosition, minDistance) &&
                    IsSpacedFromOthers(candidate, occupiedPositions, minSpacing))
                {
                    return candidate;
                }
            }
            
            // Fallback: Just get a valid position away from player
            return GetSpawnPosition(bounds, excludePosition, minDistance);
        }
        
        /// <summary>
        /// Fallback position when no valid position found - place at edge opposite to exclude position.
        /// </summary>
        protected virtual Vector3 GetFallbackPosition(Bounds bounds, Vector3 excludePosition)
        {
            // Direction from exclude position to center of bounds
            Vector3 center = bounds.center;
            Vector3 direction = (center - excludePosition).normalized;
            
            // If player is at center, pick random direction
            if (direction.magnitude < 0.01f)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            }
            
            // Place at edge of bounds in opposite direction
            float maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.z);
            return center + direction * maxExtent * 0.9f;
        }
        
        /// <summary>
        /// Utility: Clamp position to bounds.
        /// </summary>
        protected Vector3 ClampToBounds(Vector3 position, Bounds bounds)
        {
            return new Vector3(
                Mathf.Clamp(position.x, bounds.min.x, bounds.max.x),
                DEFAULT_Y_POSITION,
                Mathf.Clamp(position.z, bounds.min.z, bounds.max.z)
            );
        }
    }
}
