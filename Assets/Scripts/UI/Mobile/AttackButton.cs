// ============================================
// ATTACK BUTTON - Mobile attack toggle button
// Triggers attack on/off based on TargetSelector state
// ============================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SpaceCombat.Combat;

namespace SpaceCombat.UI.Mobile
{
    /// <summary>
    /// Mobile attack button that toggles attack state.
    ///
    /// Behavior:
    /// - Target yok → En yakın düşmanı seç + saldırıya başla
    /// - Target var ama saldırmıyor → Saldırıya başla
    /// - Zaten saldırıyorsa → Saldırıyı kes
    ///
    /// Usage:
    /// - Attach to a UI Button
    /// - Assign TargetSelector reference
    /// - Optionally customize visuals for attack state
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class AttackButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        // ============================================
        // CONFIGURATION
        // ============================================

        [Header("References")]
        [SerializeField] private TargetSelector _targetSelector;

        [Header("Visual Feedback")]
        [SerializeField] private Image _buttonImage;
        [SerializeField] private Color _normalColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color _attackingColor = new Color(1f, 0.4f, 0.1f, 1f);
        [SerializeField] private Color _pressedColor = new Color(0.6f, 0.1f, 0.1f, 1f);

        [Header("Optional")]
        [Tooltip("Icon to show when attacking")]
        [SerializeField] private GameObject _attackingIndicator;

        // ============================================
        // RUNTIME STATE
        // ============================================

        private Button _button;
        private bool _isPressed;

        // ============================================
        // UNITY LIFECYCLE
        // ============================================

        private void Awake()
        {
            _button = GetComponent<Button>();

            if (_buttonImage == null)
            {
                _buttonImage = GetComponent<Image>();
            }

            // Register button click
            _button.onClick.AddListener(OnButtonClicked);
        }

        private void Start()
        {
            // TargetSelector reference is set by MobileInputManager
            // since player is spawned at runtime
            UpdateVisuals();
        }

        private void Update()
        {
            // Update visuals based on attack state
            if (!_isPressed)
            {
                UpdateVisuals();
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnButtonClicked);
            }
        }

        // ============================================
        // EVENT HANDLERS
        // ============================================

        /// <summary>
        /// Called when button is clicked.
        /// </summary>
        private void OnButtonClicked()
        {
            if (_targetSelector != null)
            {
                _targetSelector.ToggleAttack();
            }
        }

        /// <summary>
        /// IPointerDownHandler - visual feedback on press.
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            _isPressed = true;
            if (_buttonImage != null)
            {
                _buttonImage.color = _pressedColor;
            }
        }

        /// <summary>
        /// IPointerUpHandler - restore visual on release.
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
            UpdateVisuals();
        }

        // ============================================
        // PRIVATE METHODS
        // ============================================

        private void UpdateVisuals()
        {
            bool isAttacking = _targetSelector != null && _targetSelector.IsFiringEnabled;

            if (_buttonImage != null)
            {
                _buttonImage.color = isAttacking ? _attackingColor : _normalColor;
            }

            if (_attackingIndicator != null)
            {
                _attackingIndicator.SetActive(isAttacking);
            }
        }

        // ============================================
        // PUBLIC METHODS
        // ============================================

        /// <summary>
        /// Set the TargetSelector reference at runtime.
        /// </summary>
        public void SetTargetSelector(TargetSelector targetSelector)
        {
            _targetSelector = targetSelector;
        }
    }
}
