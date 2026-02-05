// ============================================
// MOBILE HEALTH BAR - HP and Shield display
// DarkOrbit-style health/shield bars for mobile HUD
// ============================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StarReapers.Events;

namespace StarReapers.UI.Mobile
{
    /// <summary>
    /// Mobile HUD health and shield bar display.
    /// Shows HP and Shield with numeric values.
    ///
    /// Features:
    /// - Animated fill bars
    /// - Numeric display (current / max)
    /// - Color gradient based on health percentage
    /// - Subscribes to PlayerHealthChangedEvent and PlayerShieldChangedEvent
    /// </summary>
    public class MobileHealthBar : MonoBehaviour
    {
        // ============================================
        // CONFIGURATION
        // ============================================

        [Header("Health Bar")]
        [SerializeField] private Image _healthFill;
        [SerializeField] private Image _healthBackground;
        [SerializeField] private TMP_Text _healthText;
        [SerializeField] private Image _healthIcon;

        [Header("Shield Bar")]
        [SerializeField] private Image _shieldFill;
        [SerializeField] private Image _shieldBackground;
        [SerializeField] private TMP_Text _shieldText;
        [SerializeField] private Image _shieldIcon;

        [Header("Health Colors")]
        [SerializeField] private Color _healthHighColor = new Color(1f, 0.27f, 0.27f, 1f); // #FF4444
        [SerializeField] private Color _healthLowColor = new Color(0.5f, 0f, 0f, 1f);
        [SerializeField] private float _lowHealthThreshold = 0.3f;

        [Header("Shield Colors")]
        [SerializeField] private Color _shieldColor = new Color(0.27f, 0.53f, 1f, 1f); // #4488FF
        [SerializeField] private Color _shieldDepletedColor = new Color(0.2f, 0.3f, 0.5f, 0.5f);

        [Header("Animation")]
        [SerializeField] private float _fillSpeed = 5f;
        [SerializeField] private bool _animateFill = true;

        [Header("Text Format")]
        [SerializeField] private string _healthFormat = "{0:N0} / {1:N0}";
        [SerializeField] private string _shieldFormat = "{0:N0} / {1:N0}";

        // ============================================
        // RUNTIME STATE
        // ============================================

        private float _targetHealthFill;
        private float _targetShieldFill;
        private float _currentHealthFill;
        private float _currentShieldFill;

        private float _currentHealth;
        private float _maxHealth;
        private float _currentShield;
        private float _maxShield;

        // ============================================
        // UNITY LIFECYCLE
        // ============================================

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerHealthChangedEvent>(OnHealthChanged);
            EventBus.Subscribe<PlayerShieldChangedEvent>(OnShieldChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerHealthChangedEvent>(OnHealthChanged);
            EventBus.Unsubscribe<PlayerShieldChangedEvent>(OnShieldChanged);
        }

        private void Update()
        {
            if (_animateFill)
            {
                AnimateFill();
            }
        }

        // ============================================
        // PUBLIC METHODS
        // ============================================

        /// <summary>
        /// Manually set health values (for testing or initialization).
        /// </summary>
        public void SetHealth(float current, float max)
        {
            _currentHealth = current;
            _maxHealth = max;
            _targetHealthFill = max > 0 ? current / max : 0;

            if (!_animateFill)
            {
                _currentHealthFill = _targetHealthFill;
                UpdateHealthVisuals();
            }

            UpdateHealthText();
        }

        /// <summary>
        /// Manually set shield values (for testing or initialization).
        /// </summary>
        public void SetShield(float current, float max)
        {
            _currentShield = current;
            _maxShield = max;
            _targetShieldFill = max > 0 ? current / max : 0;

            if (!_animateFill)
            {
                _currentShieldFill = _targetShieldFill;
                UpdateShieldVisuals();
            }

            UpdateShieldText();
        }

        // ============================================
        // EVENT HANDLERS
        // ============================================

        private void OnHealthChanged(PlayerHealthChangedEvent evt)
        {
            _currentHealth = evt.CurrentHealth;
            _maxHealth = evt.MaxHealth;
            _targetHealthFill = evt.Percentage;

            if (!_animateFill)
            {
                _currentHealthFill = _targetHealthFill;
                UpdateHealthVisuals();
            }

            UpdateHealthText();
        }

        private void OnShieldChanged(PlayerShieldChangedEvent evt)
        {
            _currentShield = evt.CurrentShield;
            _maxShield = evt.MaxShield;
            _targetShieldFill = _maxShield > 0 ? evt.CurrentShield / evt.MaxShield : 0;

            if (!_animateFill)
            {
                _currentShieldFill = _targetShieldFill;
                UpdateShieldVisuals();
            }

            UpdateShieldText();
        }

        // ============================================
        // PRIVATE METHODS
        // ============================================

        private void AnimateFill()
        {
            // Animate health fill
            if (Mathf.Abs(_currentHealthFill - _targetHealthFill) > 0.001f)
            {
                _currentHealthFill = Mathf.Lerp(_currentHealthFill, _targetHealthFill, _fillSpeed * Time.deltaTime);
                UpdateHealthVisuals();
            }

            // Animate shield fill
            if (Mathf.Abs(_currentShieldFill - _targetShieldFill) > 0.001f)
            {
                _currentShieldFill = Mathf.Lerp(_currentShieldFill, _targetShieldFill, _fillSpeed * Time.deltaTime);
                UpdateShieldVisuals();
            }
        }

        private void UpdateHealthVisuals()
        {
            if (_healthFill != null)
            {
                _healthFill.fillAmount = _currentHealthFill;

                // Color gradient based on health percentage
                if (_currentHealthFill <= _lowHealthThreshold)
                {
                    _healthFill.color = _healthLowColor;
                }
                else
                {
                    float t = (_currentHealthFill - _lowHealthThreshold) / (1f - _lowHealthThreshold);
                    _healthFill.color = Color.Lerp(_healthLowColor, _healthHighColor, t);
                }
            }
        }

        private void UpdateShieldVisuals()
        {
            if (_shieldFill != null)
            {
                _shieldFill.fillAmount = _currentShieldFill;

                // Change color when depleted
                _shieldFill.color = _currentShieldFill > 0.01f ? _shieldColor : _shieldDepletedColor;
            }
        }

        private void UpdateHealthText()
        {
            if (_healthText != null)
            {
                _healthText.text = string.Format(_healthFormat, _currentHealth, _maxHealth);
            }
        }

        private void UpdateShieldText()
        {
            if (_shieldText != null)
            {
                _shieldText.text = string.Format(_shieldFormat, _currentShield, _maxShield);
            }
        }
    }
}
