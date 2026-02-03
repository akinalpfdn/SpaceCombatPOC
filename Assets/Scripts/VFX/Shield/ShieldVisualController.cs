// ============================================
// ShieldVisualController.cs
// DarkOrbit-style shield visual effects controller
// Manages hit ripples, hexagon pattern, and health-based colors
// ============================================

using System.Collections;
using UnityEngine;
using SpaceCombat.Entities;
using SpaceCombat.Events;
using SpaceCombat.Interfaces;
using SpaceCombat.ScriptableObjects;

namespace SpaceCombat.VFX.Shield
{
    /// <summary>
    /// Controls DarkOrbit-style shield visual effects.
    /// Subscribes to ShieldHitEvent for ripple effects at exact hit points.
    ///
    /// Design Patterns:
    /// - Strategy: Implements IShieldVisual interface
    /// - Observer: Subscribes to ShieldHitEvent via EventBus
    ///
    /// Performance Considerations:
    /// - Uses material instance instead of MaterialPropertyBlock
    ///   (MaterialPropertyBlock doesn't work with SRP Batcher for CBUFFER properties)
    /// - Each shield gets its own material instance (required for per-shield colors/hits)
    /// - Fixed-size hit array (no runtime allocations)
    /// - Shader property IDs cached at startup
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class ShieldVisualController : MonoBehaviour, IShieldVisual
    {
        // ============================================
        // CONFIGURATION
        // ============================================

        [Header("Configuration")]
        [SerializeField] private ShieldVisualConfig _config;

        [Header("Debug")]
        [SerializeField] private bool _debugLogHits = false;

        // ============================================
        // COMPONENTS
        // ============================================

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private BaseEntity _parentEntity;

        // ============================================
        // RUNTIME STATE
        // ============================================

        private Material _materialInstance;
        private ShieldHitData[] _hitPoints;
        private int _nextHitIndex;
        private float _currentShieldHealth = 1f;
        private bool _isActive = true;
        private Coroutine _hexagonRevealCoroutine;
        private float _currentHexagonVisibility;

        // ============================================
        // SHADER PROPERTY ID CACHE
        // ============================================

        // Cached for performance - avoid string lookups every frame
        private static readonly int SHIELD_COLOR_ID = Shader.PropertyToID("_ShieldColor");
        private static readonly int FRESNEL_POWER_ID = Shader.PropertyToID("_FresnelPower");
        private static readonly int FRESNEL_INTENSITY_ID = Shader.PropertyToID("_FresnelIntensity");
        private static readonly int HEXAGON_VISIBILITY_ID = Shader.PropertyToID("_HexagonVisibility");
        private static readonly int HEXAGON_SCALE_ID = Shader.PropertyToID("_HexagonScale");
        private static readonly int RIPPLE_SPEED_ID = Shader.PropertyToID("_RippleSpeed");
        private static readonly int RIPPLE_WIDTH_ID = Shader.PropertyToID("_RippleWidth");
        private static readonly int RIPPLE_MAX_RADIUS_ID = Shader.PropertyToID("_RippleMaxRadius");
        private static readonly int IDLE_PULSE_ID = Shader.PropertyToID("_IdlePulse");

        // Hit point array - Vector4: xyz = local position, w = normalized time (or -1 if inactive)
        private static int[] _hitPointIds;

        // ============================================
        // UNITY LIFECYCLE
        // ============================================

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();

            // Cache hit point shader property IDs
            if (_hitPointIds == null)
            {
                int maxHits = _config != null ? _config.MaxSimultaneousHits : 8;
                _hitPointIds = new int[maxHits];
                for (int i = 0; i < maxHits; i++)
                {
                    _hitPointIds[i] = Shader.PropertyToID($"_HitPoint{i}");
                }
            }

            // Initialize hit points array
            int hitCount = _config != null ? _config.MaxSimultaneousHits : 8;
            _hitPoints = new ShieldHitData[hitCount];

            // Get parent entity for health subscription
            _parentEntity = GetComponentInParent<BaseEntity>();
        }

        private void Start()
        {
            ApplyConfig();

            // Subscribe to parent entity's shield changes
            if (_parentEntity != null)
            {
                _parentEntity.OnShieldChanged += OnShieldHealthChanged;

                // Initialize with current shield state
                if (_parentEntity.MaxShield > 0)
                {
                    SetShieldHealth(_parentEntity.CurrentShield / _parentEntity.MaxShield);
                }
            }

            // Subscribe to shield hit events
            EventBus.Subscribe<ShieldHitEvent>(OnShieldHitEvent);
        }

        private void OnDestroy()
        {
            if (_parentEntity != null)
            {
                _parentEntity.OnShieldChanged -= OnShieldHealthChanged;
            }

            EventBus.Unsubscribe<ShieldHitEvent>(OnShieldHitEvent);

            // Clean up material instance to prevent memory leak
            if (_materialInstance != null)
            {
                Destroy(_materialInstance);
                _materialInstance = null;
            }
        }

        private void Update()
        {
            if (!_isActive) return;

            UpdateHitPoints();
            UpdateIdlePulse();
            ApplyMaterialProperties();
        }

        // ============================================
        // INITIALIZATION
        // ============================================

        /// <summary>
        /// Initializes the controller with a config at runtime.
        /// Called by PlayerShip when creating shield visual dynamically.
        /// </summary>
        public void InitializeWithConfig(ShieldVisualConfig config, BaseEntity parentEntity)
        {
            _config = config;
            _parentEntity = parentEntity;

            // Reinitialize hit points array if needed
            int hitCount = _config != null ? _config.MaxSimultaneousHits : 8;
            if (_hitPoints == null || _hitPoints.Length != hitCount)
            {
                _hitPoints = new ShieldHitData[hitCount];
            }

            ApplyConfig();

            // Subscribe to parent entity's shield changes
            if (_parentEntity != null)
            {
                _parentEntity.OnShieldChanged += OnShieldHealthChanged;

                // Initialize with current shield state
                if (_parentEntity.MaxShield > 0)
                {
                    SetShieldHealth(_parentEntity.CurrentShield / _parentEntity.MaxShield);
                }
            }

            // Subscribe to shield hit events (if not already)
            EventBus.Subscribe<ShieldHitEvent>(OnShieldHitEvent);
        }

        private void ApplyConfig()
        {
            if (_config == null)
            {
                Debug.LogWarning("[ShieldVisualController] No config assigned!");
                return;
            }

            // Apply mesh
            if (_config.ShieldMesh != null)
            {
                _meshFilter.mesh = _config.ShieldMesh;
            }

            // Apply material - creates a new instance for this shield
            // NOTE: We use material instance instead of MaterialPropertyBlock because
            // SRP Batcher ignores MaterialPropertyBlock for CBUFFER properties.
            if (_config.ShieldMaterial != null)
            {
                _materialInstance = new Material(_config.ShieldMaterial);
                _meshRenderer.material = _materialInstance;
            }
            else
            {
                _materialInstance = _meshRenderer.material;
            }

            // NOTE: Scale is NOT applied here - it's handled by the parent (PlayerShip)
            // which uses ship-specific scale from ShipConfig.shieldScale.
            // This allows each ship to have different shield sizes while sharing the same config.

            // Set static shader properties on material instance
            _materialInstance.SetFloat(FRESNEL_POWER_ID, _config.IdleFresnelPower);
            _materialInstance.SetFloat(FRESNEL_INTENSITY_ID, _config.IdleFresnelIntensity);
            _materialInstance.SetFloat(HEXAGON_SCALE_ID, _config.HexagonScale);
            _materialInstance.SetFloat(RIPPLE_SPEED_ID, _config.RippleSpeed);
            _materialInstance.SetFloat(RIPPLE_WIDTH_ID, _config.RippleWidth);
            _materialInstance.SetFloat(RIPPLE_MAX_RADIUS_ID, _config.RippleMaxRadius);
            _materialInstance.SetFloat(HEXAGON_VISIBILITY_ID, 0f);

            // Initialize color
            _materialInstance.SetColor(SHIELD_COLOR_ID, _config.ColorFull);

            // Initialize all hit points as inactive
            for (int i = 0; i < _hitPoints.Length && i < _hitPointIds.Length; i++)
            {
                _materialInstance.SetVector(_hitPointIds[i], new Vector4(0, 0, 0, -1));
            }
        }

        // ============================================
        // EVENT HANDLERS
        // ============================================

        private void OnShieldHitEvent(ShieldHitEvent evt)
        {
            // Only respond to hits on our parent entity
            if (_parentEntity == null) return;
            if (evt.Target != _parentEntity.gameObject) return;

            // Calculate intensity based on damage (normalize by some reference value)
            float intensity = Mathf.Clamp01(evt.DamageAmount / 50f); // 50 damage = full intensity

            OnShieldHit(evt.HitWorldPosition, intensity);
        }

        private void OnShieldHealthChanged(float current, float max)
        {
            if (max > 0)
            {
                SetShieldHealth(current / max);
            }
        }

        // ============================================
        // IShieldVisual IMPLEMENTATION
        // ============================================

        public void OnShieldHit(Vector3 hitWorldPosition, float intensity)
        {
            if (!_isActive) return;

            // Convert world position to local space for shader
            Vector3 localPos = transform.InverseTransformPoint(hitWorldPosition);

            // Add hit to circular buffer
            _hitPoints[_nextHitIndex] = new ShieldHitData
            {
                LocalPosition = localPos,
                StartTime = Time.time,
                Intensity = intensity,
                IsActive = true
            };

            _nextHitIndex = (_nextHitIndex + 1) % _hitPoints.Length;

            // Trigger hexagon pattern reveal
            if (_hexagonRevealCoroutine != null)
            {
                StopCoroutine(_hexagonRevealCoroutine);
            }
            _hexagonRevealCoroutine = StartCoroutine(HexagonRevealCoroutine());

            if (_debugLogHits)
            {
                Debug.Log($"[ShieldVisualController] Hit at {hitWorldPosition}, local: {localPos}, intensity: {intensity}");
            }
        }

        public void SetShieldHealth(float normalizedHealth)
        {
            _currentShieldHealth = Mathf.Clamp01(normalizedHealth);

            if (_config != null && _materialInstance != null)
            {
                Color targetColor = _config.GetColorForHealth(_currentShieldHealth);
                _materialInstance.SetColor(SHIELD_COLOR_ID, targetColor);
            }

            // Auto-hide when depleted
            if (_currentShieldHealth <= 0f)
            {
                SetShieldActive(false);
            }
            else if (!_isActive)
            {
                SetShieldActive(true);
            }
        }

        public void SetShieldActive(bool active)
        {
            _isActive = active;
            _meshRenderer.enabled = active;

            if (!active)
            {
                // Clear all hit points when deactivated
                ClearHitPoints();
            }
        }

        public void OnSpawn()
        {
            _currentShieldHealth = 1f;
            _nextHitIndex = 0;
            ClearHitPoints();

            if (_config != null && _materialInstance != null)
            {
                _materialInstance.SetColor(SHIELD_COLOR_ID, _config.ColorFull);
            }

            SetShieldActive(true);
        }

        public void OnDespawn()
        {
            SetShieldActive(false);
            ClearHitPoints();

            if (_hexagonRevealCoroutine != null)
            {
                StopCoroutine(_hexagonRevealCoroutine);
                _hexagonRevealCoroutine = null;
            }
        }

        // ============================================
        // UPDATE METHODS
        // ============================================

        private void UpdateHitPoints()
        {
            if (_config == null || _materialInstance == null) return;

            float duration = _config.RippleDuration;

            for (int i = 0; i < _hitPoints.Length && i < _hitPointIds.Length; i++)
            {
                if (_hitPoints[i].IsActive)
                {
                    // Check if expired
                    if (_hitPoints[i].IsExpired(duration))
                    {
                        _hitPoints[i].IsActive = false;
                    }
                }

                // Pack into Vector4 for shader: xyz = position, w = normalized time (or -1 if inactive)
                Vector4 hitData;
                if (_hitPoints[i].IsActive)
                {
                    hitData = new Vector4(
                        _hitPoints[i].LocalPosition.x,
                        _hitPoints[i].LocalPosition.y,
                        _hitPoints[i].LocalPosition.z,
                        _hitPoints[i].GetNormalizedTime(duration)
                    );
                }
                else
                {
                    hitData = new Vector4(0, 0, 0, -1);
                }

                _materialInstance.SetVector(_hitPointIds[i], hitData);
            }
        }

        private void UpdateIdlePulse()
        {
            if (_config == null || _materialInstance == null) return;

            // Subtle sine wave pulse for idle state
            float pulse = Mathf.Sin(Time.time * _config.IdlePulseSpeed) * _config.IdlePulseAmount;
            float fresnelIntensity = _config.IdleFresnelIntensity + pulse;
            _materialInstance.SetFloat(FRESNEL_INTENSITY_ID, fresnelIntensity);

            // Keep config values in sync (allows real-time tweaking in Play Mode)
            _materialInstance.SetFloat(HEXAGON_SCALE_ID, _config.HexagonScale);
            _materialInstance.SetFloat(RIPPLE_WIDTH_ID, _config.RippleWidth);
            _materialInstance.SetFloat(RIPPLE_MAX_RADIUS_ID, _config.RippleMaxRadius);
        }

        private void ApplyMaterialProperties()
        {
            // No longer needed - we set properties directly on material instance
            // Material changes are applied immediately without SetPropertyBlock
        }

        private void ClearHitPoints()
        {
            for (int i = 0; i < _hitPoints.Length; i++)
            {
                _hitPoints[i].IsActive = false;
            }

            _currentHexagonVisibility = 0f;
            if (_materialInstance != null)
            {
                _materialInstance.SetFloat(HEXAGON_VISIBILITY_ID, 0f);
            }
        }

        // ============================================
        // COROUTINES
        // ============================================

        private IEnumerator HexagonRevealCoroutine()
        {
            if (_config == null || _materialInstance == null) yield break;

            float duration = _config.HexagonRevealDuration;
            float elapsed = 0f;

            // Quick flash in
            _currentHexagonVisibility = 1f;
            _materialInstance.SetFloat(HEXAGON_VISIBILITY_ID, _currentHexagonVisibility);

            // Fade out
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Smooth fade out curve
                _currentHexagonVisibility = 1f - (t * t); // Quadratic ease out
                _materialInstance.SetFloat(HEXAGON_VISIBILITY_ID, _currentHexagonVisibility);

                yield return null;
            }

            _currentHexagonVisibility = 0f;
            _materialInstance.SetFloat(HEXAGON_VISIBILITY_ID, 0f);
            _hexagonRevealCoroutine = null;
        }
    }

    // ============================================
    // HIT DATA STRUCT
    // ============================================

    /// <summary>
    /// Data structure for tracking individual shield hit points.
    /// Stored in a circular buffer for efficient management.
    /// </summary>
    public struct ShieldHitData
    {
        public Vector3 LocalPosition;  // Hit position in shield's local space
        public float StartTime;        // Time.time when hit occurred
        public float Intensity;        // Based on damage amount (0-1)
        public bool IsActive;

        /// <summary>
        /// Gets the normalized time (0-1) since hit occurred.
        /// </summary>
        public float GetNormalizedTime(float duration)
        {
            return Mathf.Clamp01((Time.time - StartTime) / duration);
        }

        /// <summary>
        /// Returns true if the hit effect has finished.
        /// </summary>
        public bool IsExpired(float duration)
        {
            return Time.time - StartTime > duration;
        }
    }
}
