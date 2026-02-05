// ============================================
// IShieldVisual.cs
// Strategy interface for shield visual effects
// Allows different shield visual implementations
// ============================================

using UnityEngine;

namespace StarReapers.Interfaces
{
    /// <summary>
    /// Interface for shield visual effects.
    /// Enables Strategy pattern for different shield visual implementations.
    ///
    /// Design Pattern: Strategy
    /// - Allows swapping visual representations without changing shield logic
    /// - Decouples visual from damage system
    /// </summary>
    public interface IShieldVisual
    {
        /// <summary>
        /// Called when shield is hit at a specific world position.
        /// Triggers ripple/impact effect from that point.
        /// </summary>
        /// <param name="hitWorldPosition">World space position of the impact</param>
        /// <param name="intensity">Normalized intensity (0-1) based on damage amount</param>
        void OnShieldHit(Vector3 hitWorldPosition, float intensity);

        /// <summary>
        /// Updates shield color based on remaining health.
        /// </summary>
        /// <param name="normalizedHealth">Shield health as 0-1 value (0=depleted, 1=full)</param>
        void SetShieldHealth(float normalizedHealth);

        /// <summary>
        /// Shows or hides the shield visual.
        /// </summary>
        /// <param name="active">True to show, false to hide</param>
        void SetShieldActive(bool active);

        /// <summary>
        /// Called when entity spawns (e.g., from pool).
        /// Resets visual state.
        /// </summary>
        void OnSpawn();

        /// <summary>
        /// Called when entity despawns (e.g., returned to pool).
        /// Cleans up visual state.
        /// </summary>
        void OnDespawn();
    }
}
