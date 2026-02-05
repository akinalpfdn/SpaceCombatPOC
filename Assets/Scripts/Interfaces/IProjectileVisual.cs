// ============================================
// PROJECTILE VISUAL - Strategy Pattern Interface
// Abstracts visual representation of projectiles
// Allows swapping between sprite/mesh/particle visuals
// ============================================

using UnityEngine;

namespace StarReapers.Interfaces
{
    /// <summary>
    /// Strategy interface for projectile visual representation.
    /// Decouples visual rendering from projectile physics/damage logic.
    ///
    /// Design Patterns:
    /// - Strategy: Different visual implementations (sprite, mesh, particle)
    /// - Interface Segregation: Only visual concerns, no physics/damage
    ///
    /// SOLID Principles:
    /// - Single Responsibility: Only handles how projectile looks
    /// - Open/Closed: New visual types via new implementations
    /// - Dependency Inversion: Projectile depends on abstraction
    /// </summary>
    public interface IProjectileVisual
    {
        /// <summary>
        /// Apply HDR color to the visual (body + trail).
        /// Color should be HDR (intensity > 1) for bloom glow effect.
        /// </summary>
        void SetColor(Color color, float emissionIntensity);

        /// <summary>
        /// Apply uniform scale to the visual.
        /// </summary>
        void SetScale(float scale);

        /// <summary>
        /// Called when projectile is retrieved from pool.
        /// Should enable visuals and clear trail artifacts.
        /// </summary>
        void OnSpawn();

        /// <summary>
        /// Called when projectile is returned to pool.
        /// Should disable visuals to prevent ghost rendering.
        /// </summary>
        void OnDespawn();

        /// <summary>
        /// Full state reset for pool reuse.
        /// </summary>
        void ResetVisual();
    }
}
