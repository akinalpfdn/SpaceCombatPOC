// ============================================
// ShieldVisualConfig.cs
// Data-driven configuration for shield visual effects
// Allows artists to tweak without code changes
// ============================================

using UnityEngine;

namespace SpaceCombat.ScriptableObjects
{
    /// <summary>
    /// Configuration asset for DarkOrbit-style shield visual effects.
    /// Contains all tunable parameters for shield appearance and behavior.
    ///
    /// Usage:
    /// 1. Create asset: Right-click > Create > SpaceCombat > Shield Visual Config
    /// 2. Assign to ShieldVisualController component
    /// 3. Tweak values in inspector for desired effect
    /// </summary>
    [CreateAssetMenu(fileName = "NewShieldVisual", menuName = "SpaceCombat/Shield Visual Config")]
    public class ShieldVisualConfig : ScriptableObject
    {
        // ============================================
        // MESH & MATERIAL
        // ============================================

        [Header("Mesh Settings")]
        [Tooltip("Shield mesh - typically an ellipsoid/sphere that wraps around the ship")]
        [SerializeField] private Mesh _shieldMesh;

        [Tooltip("Shield material using ShieldURP shader")]
        [SerializeField] private Material _shieldMaterial;

        [Tooltip("Scale multiplier for the shield mesh relative to ship size")]
        [SerializeField] private Vector3 _meshScale = new Vector3(1.2f, 1.0f, 1.5f);

        // ============================================
        // IDLE STATE
        // ============================================

        [Header("Idle State (Set to 0 for invisible until hit)")]
        [Tooltip("Fresnel power - higher = sharper edge glow")]
        [Range(1f, 8f)]
        [SerializeField] private float _idleFresnelPower = 4f;

        [Tooltip("Fresnel intensity - 0 = invisible when idle, only visible on hit")]
        [Range(0f, 1f)]
        [SerializeField] private float _idleFresnelIntensity = 0f;

        [Tooltip("Speed of subtle pulsing effect")]
        [Range(0f, 3f)]
        [SerializeField] private float _idlePulseSpeed = 0f;

        [Tooltip("Amount of pulsing (0 = no pulse)")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _idlePulseAmount = 0f;

        // ============================================
        // HIT EFFECT
        // ============================================

        [Header("Hit Effect (Ripple Wave)")]
        [Tooltip("Speed at which ripple wave expands from hit point")]
        [Range(1f, 10f)]
        [SerializeField] private float _rippleSpeed = 3f;

        [Tooltip("How long the ripple effect lasts in seconds")]
        [Range(0.2f, 1.5f)]
        [SerializeField] private float _rippleDuration = 0.6f;

        [Tooltip("Width/thickness of the ripple ring")]
        [Range(0.05f, 0.5f)]
        [SerializeField] private float _rippleWidth = 0.15f;

        [Tooltip("Maximum radius the ripple can expand to")]
        [Range(0.5f, 3f)]
        [SerializeField] private float _rippleMaxRadius = 2f;

        [Header("Hit Effect (Hexagon Pattern)")]
        [Tooltip("How long hexagon pattern stays visible after hit")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _hexagonRevealDuration = 0.4f;

        [Tooltip("Scale of hexagon pattern (tiles per unit)")]
        [Range(1f, 20f)]
        [SerializeField] private float _hexagonScale = 8f;

        [Tooltip("Maximum simultaneous hits to track (circular buffer)")]
        [Range(4, 16)]
        [SerializeField] private int _maxSimultaneousHits = 8;

        // ============================================
        // HEALTH-BASED COLORS
        // ============================================

        [Header("Health Colors (HDR for Bloom)")]
        [Tooltip("Shield color when fully charged")]
        [ColorUsage(true, true)]
        [SerializeField] private Color _colorFull = new Color(0.2f, 0.6f, 1f, 1f) * 2f; // Blue HDR

        [Tooltip("Shield color at 50% health")]
        [ColorUsage(true, true)]
        [SerializeField] private Color _colorHalf = new Color(1f, 0.8f, 0.2f, 1f) * 2f; // Yellow HDR

        [Tooltip("Shield color when critical (below threshold)")]
        [ColorUsage(true, true)]
        [SerializeField] private Color _colorCritical = new Color(1f, 0.2f, 0.2f, 1f) * 3f; // Red HDR

        [Header("Health Thresholds")]
        [Tooltip("Below this percentage, shield is considered critical")]
        [Range(0.1f, 0.4f)]
        [SerializeField] private float _criticalThreshold = 0.25f;

        [Tooltip("Below this percentage, shield transitions from full to half color")]
        [Range(0.4f, 0.7f)]
        [SerializeField] private float _halfThreshold = 0.5f;

        // ============================================
        // PUBLIC PROPERTIES
        // ============================================

        // Mesh & Material
        public Mesh ShieldMesh => _shieldMesh;
        public Material ShieldMaterial => _shieldMaterial;
        public Vector3 MeshScale => _meshScale;

        // Idle State
        public float IdleFresnelPower => _idleFresnelPower;
        public float IdleFresnelIntensity => _idleFresnelIntensity;
        public float IdlePulseSpeed => _idlePulseSpeed;
        public float IdlePulseAmount => _idlePulseAmount;

        // Hit Effect
        public float RippleSpeed => _rippleSpeed;
        public float RippleDuration => _rippleDuration;
        public float RippleWidth => _rippleWidth;
        public float RippleMaxRadius => _rippleMaxRadius;
        public float HexagonRevealDuration => _hexagonRevealDuration;
        public float HexagonScale => _hexagonScale;
        public int MaxSimultaneousHits => _maxSimultaneousHits;

        // Colors
        public Color ColorFull => _colorFull;
        public Color ColorHalf => _colorHalf;
        public Color ColorCritical => _colorCritical;
        public float CriticalThreshold => _criticalThreshold;
        public float HalfThreshold => _halfThreshold;

        // ============================================
        // UTILITY METHODS
        // ============================================

        /// <summary>
        /// Calculates shield color based on normalized health (0-1).
        /// Smoothly interpolates between critical, half, and full colors.
        /// </summary>
        public Color GetColorForHealth(float normalizedHealth)
        {
            if (normalizedHealth <= _criticalThreshold)
            {
                return _colorCritical;
            }
            else if (normalizedHealth <= _halfThreshold)
            {
                // Lerp between critical and half
                float t = (normalizedHealth - _criticalThreshold) / (_halfThreshold - _criticalThreshold);
                return Color.Lerp(_colorCritical, _colorHalf, t);
            }
            else
            {
                // Lerp between half and full
                float t = (normalizedHealth - _halfThreshold) / (1f - _halfThreshold);
                return Color.Lerp(_colorHalf, _colorFull, t);
            }
        }
    }
}
