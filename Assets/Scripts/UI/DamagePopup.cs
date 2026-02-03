// ============================================
// DamagePopup.cs
// Individual floating damage number component
// Animates text rising and fading out
// ============================================

using UnityEngine;
using TMPro;
using SpaceCombat.Interfaces;
using SpaceCombat.ScriptableObjects;

namespace SpaceCombat.UI
{
    /// <summary>
    /// Controls a single damage popup instance.
    /// Spawned by DamagePopupManager, returns to pool when animation completes.
    ///
    /// Design Patterns:
    /// - Object Pool: Implements IPoolable for efficient reuse
    /// - Flyweight: Config shared across all instances
    ///
    /// Animation:
    /// - Rises upward from spawn point
    /// - Scales based on config curve
    /// - Fades out based on config curve
    /// - Billboard: Always faces camera
    /// </summary>
    [RequireComponent(typeof(TextMeshPro))]
    public class DamagePopup : MonoBehaviour, IPoolable
    {
        // ============================================
        // COMPONENTS
        // ============================================

        private TextMeshPro _textMesh;
        private Transform _cameraTransform;

        // ============================================
        // RUNTIME STATE
        // ============================================

        private DamagePopupConfig _config;
        private Vector3 _startPosition;
        private Vector3 _velocity;
        private float _elapsedTime;
        private float _duration;
        private Color _baseColor;
        private float _baseScale;
        private bool _isActive;

        // Callback when animation completes
        public System.Action<DamagePopup> OnComplete;

        // IPoolable property
        public bool IsActive => _isActive;

        // ============================================
        // UNITY LIFECYCLE
        // ============================================

        private void Awake()
        {
            _textMesh = GetComponent<TextMeshPro>();

            // Configure TextMeshPro for world space
            _textMesh.alignment = TextAlignmentOptions.Center;
            _textMesh.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private void Start()
        {
            // Cache camera transform for billboard effect
            if (Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            if (!_isActive) return;

            _elapsedTime += Time.deltaTime;
            float normalizedTime = _elapsedTime / _duration;

            if (normalizedTime >= 1f)
            {
                // Animation complete - return to pool
                _isActive = false;
                OnComplete?.Invoke(this);
                return;
            }

            // Move upward
            transform.position += _velocity * Time.deltaTime;

            // Apply scale curve
            float scale = _config.ScaleCurve.Evaluate(normalizedTime) * _baseScale;
            transform.localScale = Vector3.one * scale;

            // Apply alpha curve
            float alpha = _config.AlphaCurve.Evaluate(normalizedTime);
            Color color = _baseColor;
            color.a = alpha;
            _textMesh.color = color;

            // Billboard effect - always face camera
            if (_cameraTransform != null)
            {
                transform.rotation = _cameraTransform.rotation;
            }
        }

        // ============================================
        // PUBLIC METHODS
        // ============================================

        /// <summary>
        /// Initialize popup with config and display damage.
        /// Called by DamagePopupManager when spawning.
        /// </summary>
        public void Initialize(DamagePopupConfig config, Vector3 worldPosition,
            float damage, bool isCritical, bool isShieldDamage, DamageType damageType)
        {
            _config = config;
            _elapsedTime = 0f;
            _duration = config.Duration;
            _isActive = true;

            // Set position with random horizontal offset
            float horizontalOffset = Random.Range(-config.HorizontalSpread, config.HorizontalSpread);
            _startPosition = worldPosition + new Vector3(horizontalOffset, config.VerticalOffset, 0f);
            transform.position = _startPosition;

            // Set velocity (rising upward in world space)
            _velocity = Vector3.up * config.RiseSpeed;

            // Set text
            int displayDamage = Mathf.RoundToInt(damage);
            _textMesh.text = displayDamage.ToString();

            // Set font
            if (config.FontAsset != null)
            {
                _textMesh.font = config.FontAsset;
            }

            // Set size
            _baseScale = config.GetFontSize(isCritical);
            transform.localScale = Vector3.one * _baseScale;

            // Set color
            _baseColor = config.GetColorForDamage(damageType, isCritical, isShieldDamage);
            _textMesh.color = _baseColor;

            // Set outline
            if (config.EnableOutline)
            {
                _textMesh.outlineWidth = config.OutlineThickness;
                _textMesh.outlineColor = config.OutlineColor;
            }
            else
            {
                _textMesh.outlineWidth = 0f;
            }

            // Critical hit - add exclamation and bold
            if (isCritical)
            {
                _textMesh.text = displayDamage + "!";
                _textMesh.fontStyle = FontStyles.Bold;
            }
            else
            {
                _textMesh.fontStyle = FontStyles.Normal;
            }

            // Initial billboard
            if (_cameraTransform == null && Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
            }
            if (_cameraTransform != null)
            {
                transform.rotation = _cameraTransform.rotation;
            }
        }

        // ============================================
        // IPoolable IMPLEMENTATION
        // ============================================

        public void OnSpawn()
        {
            gameObject.SetActive(true);
            _isActive = false; // Will be set true when Initialize is called
        }

        public void OnDespawn()
        {
            gameObject.SetActive(false);
            _isActive = false;
        }

        public void ResetState()
        {
            _elapsedTime = 0f;
            _isActive = false;
            transform.localScale = Vector3.one;
            if (_textMesh != null)
            {
                _textMesh.text = "";
                _textMesh.color = Color.white;
            }
        }
    }
}
