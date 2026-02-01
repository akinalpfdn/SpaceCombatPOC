// ============================================
// HEALTH BAR - Shows health above/below entities
// Works with BaseEntity or any entity with health
// ============================================
// 
// Design Patterns Used:
// - Component Pattern: Self-contained UI element that can be attached to any entity
// - Strategy Pattern Ready: Color calculation can be extended via IHealthBarColorStrategy
// - Observer Pattern Ready: Can subscribe to health change events for efficiency
// ============================================

using UnityEngine;
using SpaceCombat.Entities;

namespace SpaceCombat.UI
{
    /// <summary>
    /// Configuration data for health bar appearance.
    /// Follows Single Responsibility Principle - separates config from behavior.
    /// </summary>
    [System.Serializable]
    public class HealthBarConfig
    {
        [Header("Size & Position")]
        public Vector3 Offset = new Vector3(0, 0, -1f);  // 3D: offset on Z for "below" entity
        public Vector2 Size = new Vector2(2f, 0.3f);
        public float BorderPadding = 0.05f;
        
        [Header("Colors")]
        public Color BackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        public Color BorderColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        public Color HealthyColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        public Color WarningColor = new Color(0.9f, 0.7f, 0.1f, 1f);
        public Color CriticalColor = new Color(0.9f, 0.1f, 0.1f, 1f);
        
        [Header("Thresholds")]
        [Range(0f, 1f)] public float WarningThreshold = 0.5f;
        [Range(0f, 1f)] public float CriticalThreshold = 0.25f;
        
        [Header("Sorting")]
        public int BaseSortingOrder = 100;
        public string SortingLayerName = "UI";
    }
    
    /// <summary>
    /// World-space health bar component for entities.
    /// Follows SOLID principles:
    /// - SRP: Only handles health bar display
    /// - OCP: Extensible via configuration and color strategies
    /// - LSP: Works with any BaseEntity
    /// - ISP: Minimal public interface
    /// - DIP: Depends on abstractions (BaseEntity)
    /// </summary>
    public class HealthBar : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private HealthBarConfig _config = new HealthBarConfig();
        
        [Header("Display Settings")]
        [SerializeField] private bool _alwaysShow = false;
        [SerializeField] private bool _hideWhenFull = true;
        
        [Header("Target (optional - auto-detects if not set)")]
        [SerializeField] private BaseEntity _externalTarget;
        
        // Visual components
        private Transform _barContainer;
        private SpriteRenderer _border;
        private SpriteRenderer _background;
        private SpriteRenderer _fill;
        
        // Cached references
        private BaseEntity _entity;
        private Sprite _sharedSprite;
        private float _lastHealthPercent = -1f;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            CreateSharedSprite();
            CreateHealthBarStructure();
        }
        
        private void Start()
        {
            _entity = _externalTarget ?? GetComponent<BaseEntity>();
            
            if (_entity == null)
            {
                Debug.LogWarning($"[HealthBar] No BaseEntity found on {gameObject.name}. Health bar will not function.", this);
                SetVisible(false);
                return;
            }
            
            // Initial update
            ForceUpdate();
        }
        
        private void LateUpdate()
        {
            if (_entity == null || _barContainer == null) return;

            // 3D Version: Keep bar at world position on XZ plane (not affected by entity rotation)
            Vector3 pos = _entity.transform.position;
            _barContainer.position = new Vector3(pos.x + _config.Offset.x, pos.y + _config.Offset.y, pos.z + _config.Offset.z);
            _barContainer.rotation = Quaternion.identity;

            // Only update visuals if health changed (optimization)
            float currentPercent = _entity.CurrentHealth / _entity.MaxHealth;
            if (!Mathf.Approximately(currentPercent, _lastHealthPercent))
            {
                UpdateHealthVisuals(currentPercent);
                _lastHealthPercent = currentPercent;
            }
        }
        
        private void OnDestroy()
        {
            // Clean up dynamically created sprite
            if (_sharedSprite != null)
            {
                Destroy(_sharedSprite.texture);
                Destroy(_sharedSprite);
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void CreateSharedSprite()
        {
            // Single shared sprite for all renderers (memory efficient)
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            texture.filterMode = FilterMode.Point;
            
            // Center pivot for consistent positioning
            _sharedSprite = Sprite.Create(
                texture, 
                new Rect(0, 0, 1, 1), 
                new Vector2(0.5f, 0.5f), 
                1f
            );
        }
        
        private void CreateHealthBarStructure()
        {
            // Container - holds all bar elements, positioned in world space
            var containerObj = new GameObject("HealthBar_Container");
            _barContainer = containerObj.transform;
            _barContainer.SetParent(transform, false);
            _barContainer.localPosition = _config.Offset;
            
            // Layer 1: Border (outermost)
            _border = CreateBarLayer("Border", _barContainer, _config.BaseSortingOrder);
            _border.color = _config.BorderColor;
            
            // Layer 2: Background (inside border)
            _background = CreateBarLayer("Background", _barContainer, _config.BaseSortingOrder + 1);
            _background.color = _config.BackgroundColor;
            
            // Layer 3: Fill (health indicator)
            _fill = CreateBarLayer("Fill", _barContainer, _config.BaseSortingOrder + 2);
            _fill.color = _config.HealthyColor;
            
            // Set initial sizes
            ApplySizes(1f);
        }
        
        private SpriteRenderer CreateBarLayer(string name, Transform parent, int sortingOrder)
        {
            var obj = new GameObject($"HealthBar_{name}");
            obj.transform.SetParent(parent, false);
            
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = _sharedSprite;
            renderer.sortingOrder = sortingOrder;
            
            if (!string.IsNullOrEmpty(_config.SortingLayerName))
            {
                renderer.sortingLayerName = _config.SortingLayerName;
            }
            
            return renderer;
        }
        
        #endregion
        
        #region Visual Updates
        
        private void ForceUpdate()
        {
            if (_entity == null) return;
            
            float percent = _entity.CurrentHealth / _entity.MaxHealth;
            UpdateHealthVisuals(percent);
            _lastHealthPercent = percent;
        }
        
        private void UpdateHealthVisuals(float healthPercent)
        {
            healthPercent = Mathf.Clamp01(healthPercent);
            
            // Update fill size
            ApplySizes(healthPercent);
            
            // Update fill color based on health
            _fill.color = GetHealthColor(healthPercent);
            
            // Handle visibility
            bool shouldShow = _alwaysShow || healthPercent < 1f;
            SetVisible(shouldShow);
        }
        
        private void ApplySizes(float fillPercent)
        {
            float width = _config.Size.x;
            float height = _config.Size.y;
            float padding = _config.BorderPadding;
            
            // Border: Full size (centered at origin)
            _border.transform.localScale = new Vector3(width, height, 1f);
            _border.transform.localPosition = Vector3.zero;
            
            // Background: Slightly smaller (inside border, centered)
            float bgWidth = width - (padding * 2f);
            float bgHeight = height - (padding * 2f);
            _background.transform.localScale = new Vector3(bgWidth, bgHeight, 1f);
            _background.transform.localPosition = Vector3.zero;
            
            // Fill: Width based on health percent
            // All sprites use CENTER pivot, so we calculate position from left edge
            float fillWidth = bgWidth * fillPercent;
            _fill.transform.localScale = new Vector3(fillWidth, bgHeight, 1f);
            
            // Position fill so it grows from left edge
            // Left edge of background (centered) is at -bgWidth/2
            // Center of fill should be at: leftEdge + fillWidth/2
            float fillCenterX = (-bgWidth / 2f) + (fillWidth / 2f);
            _fill.transform.localPosition = new Vector3(fillCenterX, 0f, 0f);
        }
        
        private Color GetHealthColor(float healthPercent)
        {
            if (healthPercent <= _config.CriticalThreshold)
            {
                return _config.CriticalColor;
            }
            else if (healthPercent <= _config.WarningThreshold)
            {
                // Lerp between critical and warning colors
                float t = (healthPercent - _config.CriticalThreshold) / 
                          (_config.WarningThreshold - _config.CriticalThreshold);
                return Color.Lerp(_config.CriticalColor, _config.WarningColor, t);
            }
            else
            {
                // Lerp between warning and healthy colors
                float t = (healthPercent - _config.WarningThreshold) / 
                          (1f - _config.WarningThreshold);
                return Color.Lerp(_config.WarningColor, _config.HealthyColor, t);
            }
        }
        
        private void SetVisible(bool visible)
        {
            if (_border != null) _border.enabled = visible;
            if (_background != null) _background.enabled = visible;
            if (_fill != null) _fill.enabled = visible;
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Set the position offset from the entity.
        /// 3D Version: Uses Vector3 for XZ plane positioning
        /// </summary>
        public void SetOffset(Vector3 offset)
        {
            _config.Offset = offset;
        }
        
        /// <summary>
        /// Set whether the health bar is always visible.
        /// </summary>
        public void SetAlwaysShow(bool alwaysShow)
        {
            _alwaysShow = alwaysShow;
            ForceUpdate();
        }
        
        /// <summary>
        /// Set an external target entity to track.
        /// </summary>
        public void SetTarget(BaseEntity target)
        {
            _externalTarget = target;
            _entity = target;
            _lastHealthPercent = -1f; // Force update
        }
        
        /// <summary>
        /// Update configuration at runtime.
        /// </summary>
        public void SetConfig(HealthBarConfig config)
        {
            _config = config;
            ForceUpdate();
        }
        
        /// <summary>
        /// Get current configuration (for reading).
        /// </summary>
        public HealthBarConfig GetConfig() => _config;
        
        #endregion
    }
}