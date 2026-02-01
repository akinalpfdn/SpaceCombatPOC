// ============================================
// PLAYER STATUS DISPLAY - Text-based health/shield
// Shows "HP: 100/100" and "SH: 50/50" as text
// ============================================

using UnityEngine;
using SpaceCombat.Entities;

namespace SpaceCombat.UI
{
    /// <summary>
    /// Displays player health and shield as text
    /// Attach to Player ship or a UI canvas
    /// 3D Version - Uses XZ plane for movement, Y for vertical offset
    /// </summary>
    public class PlayerStatusDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BaseEntity _target;

        [Header("Display Settings")]
        [SerializeField] private bool _showHealth = true;
        [SerializeField] private bool _showShield = true;
        [SerializeField] private Vector3 _offset = new Vector3(0, 0, -1.5f);  // 3D: offset on Z for "below"
        [SerializeField] private float _lineSpacing = 0.4f;

        [Header("Text Styling")]
        [SerializeField] private float _fontScale = 0.05f;
        [SerializeField] private Color _healthColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color _shieldColor = new Color(0.3f, 0.6f, 1f, 1f);
        [SerializeField] private int _sortingOrder = 200;
        [SerializeField] private string _sortingLayerName = "UI";

        private Transform _container;
        private TextMesh _healthText;
        private TextMesh _shieldText;
        private Font _font;
        
        // Cached for optimization
        private float _lastHealth = -1f;
        private float _lastShield = -1f;

        private void Start()
        {
            if (_target == null)
            {
                _target = GetComponent<BaseEntity>();
            }

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CreateContainer();
            CreateTextObjects();
            ForceUpdate();
        }

        private void LateUpdate()
        {
            if (_target == null || _container == null) return;

            // 3D Version: Position container at entity position + offset (XZ plane movement)
            Vector3 pos = transform.position;
            _container.position = new Vector3(pos.x + _offset.x, pos.y + _offset.y, pos.z + _offset.z);
            _container.rotation = Quaternion.identity;

            // Only update if values changed
            if (!Mathf.Approximately(_target.CurrentHealth, _lastHealth) ||
                !Mathf.Approximately(_target.CurrentShield, _lastShield))
            {
                UpdateDisplay();
            }
        }

        private void CreateContainer()
        {
            var containerObj = new GameObject("PlayerStatus_Container");
            _container = containerObj.transform;
            _container.SetParent(transform, false);
            _container.localPosition = _offset;
        }

        private void CreateTextObjects()
        {
            float currentY = 0f;
            
            // Health text (top)
            if (_showHealth)
            {
                _healthText = CreateTextMesh("HealthText", _healthColor);
                _healthText.transform.localPosition = new Vector3(0, currentY, 0);
                currentY -= _lineSpacing;
            }

            // Shield text (below health)
            if (_showShield)
            {
                _shieldText = CreateTextMesh("ShieldText", _shieldColor);
                _shieldText.transform.localPosition = new Vector3(0, currentY, 0);
            }
        }

        private TextMesh CreateTextMesh(string name, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(_container, false);
            
            var textMesh = obj.AddComponent<TextMesh>();
            textMesh.font = _font;
            textMesh.fontSize = 64;
            textMesh.characterSize = 0.1f;
            textMesh.color = color;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.transform.localScale = Vector3.one * _fontScale;
            
            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.sortingOrder = _sortingOrder;
            
            if (!string.IsNullOrEmpty(_sortingLayerName))
            {
                renderer.sortingLayerName = _sortingLayerName;
            }
            
            if (_font != null && _font.material != null)
            {
                renderer.material = _font.material;
            }
            
            return textMesh;
        }

        private void ForceUpdate()
        {
            _lastHealth = -1f;
            _lastShield = -1f;
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (_target == null) return;

            if (_healthText != null)
            {
                _healthText.text = $"HP: {_target.CurrentHealth:F0}/{_target.MaxHealth:F0}";
            }

            if (_shieldText != null)
            {
                if (_target.MaxShield > 0)
                {
                    _shieldText.text = $"SH: {_target.CurrentShield:F0}/{_target.MaxShield:F0}";
                    _shieldText.gameObject.SetActive(true);
                }
                else
                {
                    _shieldText.gameObject.SetActive(false);
                }
            }
            
            _lastHealth = _target.CurrentHealth;
            _lastShield = _target.CurrentShield;
        }

        public void SetTarget(BaseEntity target)
        {
            _target = target;
            ForceUpdate();
        }
    }
}