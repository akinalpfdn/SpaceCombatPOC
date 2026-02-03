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

        // Distance-based scaling
        private const float REFERENCE_DISTANCE = 15f; // Distance at which _baseScale is "correct"

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

            // Move in velocity direction (camera up)
            transform.position += _velocity * Time.deltaTime;

            // Calculate distance-based scale to maintain consistent screen size
            // Popup appears same size regardless of distance from camera
            float distanceScale = 1f;
            if (_cameraTransform != null)
            {
                float distance = Vector3.Distance(transform.position, _cameraTransform.position);
                distanceScale = distance / REFERENCE_DISTANCE;
            }

            // Apply scale curve with distance compensation
            float curveScale = _config.ScaleCurve.Evaluate(normalizedTime);
            float finalScale = curveScale * _baseScale * distanceScale;
            transform.localScale = Vector3.one * finalScale;

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

            // Cache camera if not yet cached
            if (_cameraTransform == null && Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
            }

            // Set position with random horizontal offset (spread in XZ plane for top-down view)
            // Use camera's right vector for horizontal spread so it spreads on screen
            float horizontalOffset = Random.Range(-config.HorizontalSpread, config.HorizontalSpread);
            Vector3 spreadDirection = _cameraTransform != null ? _cameraTransform.right : Vector3.right;

            // Offset in camera's up direction (screen up) for vertical positioning
            Vector3 upDirection = _cameraTransform != null ? _cameraTransform.up : Vector3.up;

            _startPosition = worldPosition + (spreadDirection * horizontalOffset) + (upDirection * config.VerticalOffset);
            transform.position = _startPosition;

            // Set velocity to rise in camera's up direction (screen up)
            // This makes the popup always rise "upward" on screen regardless of camera angle
            _velocity = upDirection * config.RiseSpeed;

            // Set text with thousands separator (e.g., 2,146)
            int displayDamage = Mathf.RoundToInt(damage);
            _textMesh.text = displayDamage.ToString("N0");

            // Set font
            if (config.FontAsset != null)
            {
                _textMesh.font = config.FontAsset;
            }

            // Set size with distance compensation for consistent screen size
            _baseScale = config.GetFontSize(isCritical);
            float initialDistanceScale = 1f;
            if (_cameraTransform != null)
            {
                float distance = Vector3.Distance(transform.position, _cameraTransform.position);
                initialDistanceScale = distance / REFERENCE_DISTANCE;
            }
            transform.localScale = Vector3.one * _baseScale * initialDistanceScale;

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
                _textMesh.text = displayDamage.ToString("N0") + "!";
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
