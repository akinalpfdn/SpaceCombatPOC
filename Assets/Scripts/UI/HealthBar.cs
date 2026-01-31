// ============================================
// HEALTH BAR - Shows health above/below entities
// Works with BaseEntity or any entity with health
// ============================================

using UnityEngine;
using SpaceCombat.Entities;

namespace SpaceCombat.UI
{
    public class HealthBar : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private SpriteRenderer _background;
        [SerializeField] private SpriteRenderer _fill;

        [Header("Settings")]
        [SerializeField] private Vector2 _offset = new Vector2(0, -1f); // Position below entity
        [SerializeField] private Vector2 _size = new Vector2(2f, 0.5f); // Increased height
        [SerializeField] private Color _backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f); // Dark gray background
        [SerializeField] private Color _healthyColor = new Color(0.2f, 0.8f, 0.2f, 1f); // Green
        [SerializeField] private Color _criticalColor = new Color(0.9f, 0.1f, 0.1f, 1f); // Red
        [SerializeField] private float _criticalPercent = 0.3f;

        [Header("Display")]
        [SerializeField] private bool _alwaysShow = false;
        [SerializeField] private bool _hideWhenFull = true;

        [Header("External Target (optional)")]
        [SerializeField] private BaseEntity _externalTarget;

        private BaseEntity _entity;
        private float _maxWidth;

        private void Awake()
        {
            CreateHealthBar();
        }

        private void CreateHealthBar()
        {
            // Create background (direct child, will be positioned in world space)
            var bgObj = new GameObject("HealthBar_BG");
            bgObj.transform.SetParent(transform, false);
            bgObj.transform.localPosition = _offset;

            _background = bgObj.AddComponent<SpriteRenderer>();
            _background.drawMode = SpriteDrawMode.Simple;
            _background.color = _backgroundColor;
            _background.sortingOrder = 100;

            // Create fill
            var fillObj = new GameObject("HealthBar_Fill");
            fillObj.transform.SetParent(bgObj.transform, false);

            _fill = fillObj.AddComponent<SpriteRenderer>();
            _fill.drawMode = SpriteDrawMode.Simple;
            _fill.color = _healthyColor;
            _fill.sortingOrder = 101;

            _maxWidth = _size.x;

            // Set sizes
            UpdateSize(_size.x, _size.x);
        }

        private void Start()
        {
            // Use external target if set, otherwise look on this GameObject
            _entity = _externalTarget ?? GetComponent<BaseEntity>();

            // Create default sprites if none assigned
            if (_background.sprite == null)
            {
                _background.sprite = CreateBoxSprite();
            }
            if (_fill.sprite == null)
            {
                _fill.sprite = CreateBoxSprite();
            }

            // Initial update
            UpdateHealthBar();

            // Hide if full health and hideWhenFull is enabled
            if (_hideWhenFull && _entity.IsAtFullHealth)
            {
                SetVisible(false);
            }
        }

        private Sprite CreateBoxSprite()
        {
            // Create a simple white box texture - use 1x1 pixel for proper scaling
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            // No wrap, filter mode for crisp scaling
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            // Set pixelsPerUnit to 1 so scale directly matches world units
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
        }

        private void LateUpdate()
        {
            // Update position and rotation every frame
            if (_background != null)
            {
                // Position at entity position + offset (in world space, not affected by rotation)
                _background.transform.position = (Vector2)transform.position + _offset;
                // Keep horizontal
                _background.transform.rotation = Quaternion.identity;
            }

            UpdateHealthBar();
        }

        private void UpdateHealthBar()
        {
            if (_entity == null || _background == null || _fill == null) return;

            float healthPercent = _entity.CurrentHealth / _entity.MaxHealth;
            float fillWidth = _maxWidth * healthPercent;

            UpdateSize(_maxWidth, fillWidth);

            // Update color based on health
            if (healthPercent <= _criticalPercent)
            {
                _fill.color = _criticalColor;
            }
            else
            {
                _fill.color = _healthyColor;
            }

            // Handle visibility
            bool shouldShow = _alwaysShow || !_entity.IsAtFullHealth;
            SetVisible(shouldShow);
        }

        private void UpdateSize(float backgroundWidth, float fillWidth)
        {
            // Background size (full width)
            _background.transform.localScale = new Vector3(backgroundWidth, _size.y, 1f);

            // Fill size (current health)
            _fill.transform.localScale = new Vector3(fillWidth, _size.y, 1f);

            // Position fill so it starts from left edge of background
            _fill.transform.localPosition = new Vector3(-backgroundWidth / 2f + fillWidth / 2f, 0, 0);
        }

        private void SetVisible(bool visible)
        {
            if (_background != null)
                _background.enabled = visible;
            if (_fill != null)
                _fill.enabled = visible;
        }

        public void SetOffset(Vector2 offset)
        {
            _offset = offset;
        }

        public void SetAlwaysShow(bool alwaysShow)
        {
            _alwaysShow = alwaysShow;
        }

        public void SetTarget(BaseEntity target)
        {
            _externalTarget = target;
            _entity = target;
        }
    }
}
