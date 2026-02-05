// ============================================
// WEAPON SLOT BUTTON - Individual weapon slot UI
// Mobile UI button for weapon switching (LA-1 to LA-4)
// ============================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using SpaceCombat.Entities;

namespace SpaceCombat.UI.Mobile
{
    /// <summary>
    /// Individual weapon slot button for mobile HUD.
    /// Displays slot number and weapon name, handles selection.
    ///
    /// Usage:
    /// - Attach to a UI Button
    /// - Set slot index (0-3)
    /// - WeaponSlotBar manages multiple instances
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class WeaponSlotButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        // ============================================
        // CONFIGURATION
        // ============================================

        [Header("Slot Settings")]
        [SerializeField] private int _slotIndex = 0; // 0-3 for LA-1 to LA-4

        [Header("UI References")]
        [SerializeField] private Image _background;
        [SerializeField] private Image _border;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _slotNumberText;
        [SerializeField] private TMP_Text _weaponNameText;

        [Header("Colors")]
        [SerializeField] private Color _normalColor = new Color(0.04f, 0.09f, 0.16f, 0.8f);
        [SerializeField] private Color _selectedColor = new Color(0f, 0.83f, 1f, 0.3f);
        [SerializeField] private Color _normalBorderColor = new Color(0f, 0.5f, 0.6f, 0.8f);
        [SerializeField] private Color _selectedBorderColor = new Color(0f, 0.83f, 1f, 1f);
        [SerializeField] private Color _pressedColor = new Color(0f, 0.4f, 0.5f, 0.9f);

        [Header("Selection Indicator")]
        [SerializeField] private GameObject _selectedIndicator;
        [SerializeField] private GameObject _equippedArrow;

        // ============================================
        // RUNTIME STATE
        // ============================================

        private Button _button;
        private Image _buttonImage; // Fallback if _background not assigned
        private PlayerShip _playerShip;
        private bool _isSelected;
        private bool _isPressed;

        // ============================================
        // PROPERTIES
        // ============================================

        public int SlotIndex => _slotIndex;
        public bool IsSelected => _isSelected;

        // ============================================
        // UNITY LIFECYCLE
        // ============================================

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnButtonClicked);

            // CRITICAL: Disable Button's ColorTint transition
            // Without this, EventSystem state changes (e.g., clicking joystick)
            // will override our manual colors via Button's color animation
            _button.transition = Selectable.Transition.None;

            // If _background not assigned, use Button's own Image component
            if (_background == null)
            {
                _buttonImage = GetComponent<Image>();
            }

            // Set slot number text only if empty (allows custom text in Inspector)
            if (_slotNumberText != null && string.IsNullOrEmpty(_slotNumberText.text))
            {
                _slotNumberText.text = (_slotIndex + 1).ToString();
            }

            UpdateVisuals();
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnButtonClicked);
            }
        }

        // ============================================
        // PUBLIC METHODS
        // ============================================

        /// <summary>
        /// Set the PlayerShip reference for weapon switching.
        /// </summary>
        public void SetPlayerShip(PlayerShip playerShip)
        {
            _playerShip = playerShip;
        }

        /// <summary>
        /// Set whether this slot is currently selected/active.
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            UpdateVisuals();
        }

        /// <summary>
        /// Set the weapon name to display (optional).
        /// Only sets if current text is empty, preserving Inspector values.
        /// </summary>
        public void SetWeaponName(string name)
        {
            if (_weaponNameText != null && string.IsNullOrEmpty(_weaponNameText.text))
            {
                _weaponNameText.text = name;
            }
        }

        /// <summary>
        /// Force set the weapon name, overwriting any existing value.
        /// </summary>
        public void ForceSetWeaponName(string name)
        {
            if (_weaponNameText != null)
            {
                _weaponNameText.text = name;
            }
        }

        /// <summary>
        /// Set the weapon icon sprite (optional).
        /// </summary>
        public void SetIcon(Sprite iconSprite)
        {
            if (_icon != null && iconSprite != null)
            {
                _icon.sprite = iconSprite;
                _icon.enabled = true;
            }
        }

        // ============================================
        // EVENT HANDLERS
        // ============================================

        private void OnButtonClicked()
        {
            if (_playerShip != null)
            {
                _playerShip.SelectWeaponSlot(_slotIndex);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPressed = true;
            UpdateVisuals();
        }

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
            // Determine which Image to use for background color
            Image targetImage = _background != null ? _background : _buttonImage;

            // Background color
            if (targetImage != null)
            {
                if (_isPressed)
                {
                    targetImage.color = _pressedColor;
                }
                else if (_isSelected)
                {
                    targetImage.color = _selectedColor;
                }
                else
                {
                    targetImage.color = _normalColor;
                }
            }

            // Border color
            if (_border != null)
            {
                _border.color = _isSelected ? _selectedBorderColor : _normalBorderColor;
            }

            // Selection indicator
            if (_selectedIndicator != null)
            {
                _selectedIndicator.SetActive(_isSelected);
            }

            // Equipped arrow (small chevron showing active weapon)
            if (_equippedArrow != null)
            {
                _equippedArrow.SetActive(_isSelected);
            }
        }
    }
}
