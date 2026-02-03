// ============================================
// DamagePopupConfig.cs
// Configuration for floating damage numbers
// DarkOrbit/RPG style damage popup settings
// ============================================

using UnityEngine;
using TMPro;

namespace SpaceCombat.ScriptableObjects
{
    /// <summary>
    /// Configuration asset for damage popup visual effects.
    /// Contains all tunable parameters for popup appearance and animation.
    ///
    /// Usage:
    /// 1. Create asset: Right-click > Create > SpaceCombat > Damage Popup Config
    /// 2. Assign font asset (create from Bebas Neue TTF via TextMeshPro)
    /// 3. Assign to DamagePopupManager
    /// </summary>
    [CreateAssetMenu(fileName = "NewDamagePopup", menuName = "SpaceCombat/Damage Popup Config")]
    public class DamagePopupConfig : ScriptableObject
    {
        // ============================================
        // FONT SETTINGS
        // ============================================

        [Header("Font Settings")]
        [Tooltip("TextMeshPro font asset (create from Bebas Neue TTF)")]
        [SerializeField] private TMP_FontAsset _fontAsset;

        [Tooltip("Base font size for normal damage")]
        [Range(2f, 10f)]
        [SerializeField] private float _baseFontSize = 4f;

        [Tooltip("Font size multiplier for critical hits")]
        [Range(1f, 2f)]
        [SerializeField] private float _criticalSizeMultiplier = 1.5f;

        // ============================================
        // ANIMATION SETTINGS
        // ============================================

        [Header("Animation")]
        [Tooltip("How long the popup stays visible")]
        [Range(0.5f, 3f)]
        [SerializeField] private float _duration = 1.2f;

        [Tooltip("How fast the popup rises")]
        [Range(0.5f, 5f)]
        [SerializeField] private float _riseSpeed = 2f;

        [Tooltip("Random horizontal spread range")]
        [Range(0f, 1f)]
        [SerializeField] private float _horizontalSpread = 0.3f;

        [Tooltip("Vertical offset from hit position")]
        [Range(0f, 2f)]
        [SerializeField] private float _verticalOffset = 0.5f;

        [Tooltip("Scale animation curve (0-1 over lifetime)")]
        [SerializeField] private AnimationCurve _scaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Tooltip("Alpha fade curve (0-1 over lifetime)")]
        [SerializeField] private AnimationCurve _alphaCurve = AnimationCurve.Linear(0, 1, 1, 0);

        // ============================================
        // COLOR SETTINGS
        // ============================================

        [Header("Colors")]
        [Tooltip("Normal damage color")]
        [SerializeField] private Color _normalDamageColor = Color.white;

        [Tooltip("Critical hit color")]
        [SerializeField] private Color _criticalDamageColor = new Color(1f, 0.8f, 0f, 1f); // Gold

        [Tooltip("Shield damage color")]
        [SerializeField] private Color _shieldDamageColor = new Color(0.3f, 0.7f, 1f, 1f); // Light blue

        [Tooltip("Laser damage color")]
        [SerializeField] private Color _laserDamageColor = new Color(1f, 0.4f, 0.1f, 1f); // Orange

        [Tooltip("Plasma damage color")]
        [SerializeField] private Color _plasmaDamageColor = new Color(0.8f, 0.2f, 1f, 1f); // Purple

        [Tooltip("EMP damage color")]
        [SerializeField] private Color _empDamageColor = new Color(0.3f, 0.9f, 1f, 1f); // Cyan

        // ============================================
        // OUTLINE SETTINGS
        // ============================================

        [Header("Outline")]
        [Tooltip("Enable text outline for better visibility")]
        [SerializeField] private bool _enableOutline = true;

        [Tooltip("Outline color")]
        [SerializeField] private Color _outlineColor = Color.black;

        [Tooltip("Outline thickness")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _outlineThickness = 0.2f;

        // ============================================
        // POOLING SETTINGS
        // ============================================

        [Header("Pooling")]
        [Tooltip("Initial pool size")]
        [Range(5, 50)]
        [SerializeField] private int _initialPoolSize = 20;

        [Tooltip("Maximum simultaneous popups")]
        [Range(10, 100)]
        [SerializeField] private int _maxPoolSize = 50;

        // ============================================
        // PUBLIC PROPERTIES
        // ============================================

        // Font
        public TMP_FontAsset FontAsset => _fontAsset;
        public float BaseFontSize => _baseFontSize;
        public float CriticalSizeMultiplier => _criticalSizeMultiplier;

        // Animation
        public float Duration => _duration;
        public float RiseSpeed => _riseSpeed;
        public float HorizontalSpread => _horizontalSpread;
        public float VerticalOffset => _verticalOffset;
        public AnimationCurve ScaleCurve => _scaleCurve;
        public AnimationCurve AlphaCurve => _alphaCurve;

        // Colors
        public Color NormalDamageColor => _normalDamageColor;
        public Color CriticalDamageColor => _criticalDamageColor;
        public Color ShieldDamageColor => _shieldDamageColor;
        public Color LaserDamageColor => _laserDamageColor;
        public Color PlasmaDamageColor => _plasmaDamageColor;
        public Color EmpDamageColor => _empDamageColor;

        // Outline
        public bool EnableOutline => _enableOutline;
        public Color OutlineColor => _outlineColor;
        public float OutlineThickness => _outlineThickness;

        // Pooling
        public int InitialPoolSize => _initialPoolSize;
        public int MaxPoolSize => _maxPoolSize;

        // ============================================
        // UTILITY METHODS
        // ============================================

        /// <summary>
        /// Gets the appropriate color for the damage type.
        /// </summary>
        public Color GetColorForDamage(Interfaces.DamageType damageType, bool isCritical, bool isShieldDamage)
        {
            if (isCritical)
                return _criticalDamageColor;

            if (isShieldDamage)
                return _shieldDamageColor;

            return damageType switch
            {
                Interfaces.DamageType.Laser => _laserDamageColor,
                Interfaces.DamageType.Plasma => _plasmaDamageColor,
                Interfaces.DamageType.EMP => _empDamageColor,
                _ => _normalDamageColor
            };
        }

        /// <summary>
        /// Gets the font size based on critical status.
        /// </summary>
        public float GetFontSize(bool isCritical)
        {
            return isCritical ? _baseFontSize * _criticalSizeMultiplier : _baseFontSize;
        }
    }
}
