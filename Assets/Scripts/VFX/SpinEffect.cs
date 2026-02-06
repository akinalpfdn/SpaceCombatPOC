// ============================================
// SPIN EFFECT - Continuous rotation visual effect
// Reusable component for spinning objects (bosses, pickups, etc.)
// ============================================
//
// Usage:
// - Attach to a CHILD visual object (not the root!)
// - Root handles AI rotation (facing, targeting)
// - Child handles visual spin independently
//
// Why child? If placed on root, the spin overrides AI rotation,
// breaking weapon aiming and enemy facing direction.
// ============================================

using UnityEngine;

namespace StarReapers.VFX
{
    /// <summary>
    /// Applies continuous rotation to a GameObject.
    /// Designed for visual children so gameplay rotation is unaffected.
    /// Component Pattern: self-contained, reusable on any object.
    /// </summary>
    public class SpinEffect : MonoBehaviour
    {
        [Header("Rotation")]
        [Tooltip("Rotation speed in degrees/second per axis. Y = top-down spin on XZ plane.")]
        [SerializeField] private Vector3 _rotationSpeed = new Vector3(0f, 90f, 0f);

        [Header("Options")]
        [Tooltip("Use unscaled time (spins during pause)")]
        [SerializeField] private bool _unscaledTime = false;

        private void Update()
        {
            float dt = _unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            transform.Rotate(_rotationSpeed * dt, Space.Self);
        }
    }
}
