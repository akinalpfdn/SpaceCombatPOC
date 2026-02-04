// ============================================
// TARGET INDICATOR INTERFACE
// Common interface for 2D and 3D target indicators
// ============================================

using UnityEngine;

namespace SpaceCombat.Interfaces
{
    /// <summary>
    /// Interface for target indicator implementations.
    /// Allows TargetSelector to work with both 2D and 3D indicators.
    /// </summary>
    public interface ITargetIndicator
    {
        /// <summary>
        /// Set the target to follow. Pass null to hide.
        /// </summary>
        void SetTarget(Transform target);

        /// <summary>
        /// Set the indicator color.
        /// </summary>
        void SetColor(Color color);

        /// <summary>
        /// Set the base scale for the indicator.
        /// </summary>
        void SetBaseScale(float scale);

        /// <summary>
        /// Get the GameObject this indicator is attached to.
        /// </summary>
        GameObject gameObject { get; }
    }
}
