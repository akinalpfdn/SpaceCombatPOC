// ============================================
// KEYBOARD INPUT PROVIDER
// Mouse and keyboard input for desktop/testing
// Uses new Input System (UnityEngine.InputSystem)
// Movement removed - joystick only
// ============================================

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using StarReapers.Interfaces;
using StarReapers.Core;

namespace StarReapers.Input
{
    /// <summary>
    /// Mouse and keyboard input provider for desktop/testing.
    /// Movement is handled by VirtualJoystick, this handles:
    /// - Mouse aim
    /// - Fire (left click / space)
    /// - Special ability (shift / right click)
    /// - Weapon slot selection (1-4 keys)
    /// </summary>
    public class KeyboardInputProvider : MonoBehaviour, IInputProvider
    {
        // ============================================
        // CONFIGURATION
        // ============================================

        [Header("Settings")]
        [SerializeField] private bool _useMouseForAim = true;
        [SerializeField] private Camera _mainCamera;

        // ============================================
        // PROPERTIES
        // ============================================

        public Vector2 MovementInput => Vector2.zero; // Movement via joystick only
        public Vector2 AimDirection => _aimDirection;
        public bool IsFiring => _isFiring;
        public bool IsSpecialAbility => _isSpecialAbility;

        // ============================================
        // EVENTS
        // ============================================

        public event Action OnFirePressed;
        public event Action OnFireReleased;
        public event Action OnSpecialAbilityPressed;
        public event Action<int> OnWeaponSlotSelected;

        // ============================================
        // RUNTIME STATE
        // ============================================

        private Vector2 _aimDirection;
        private bool _isFiring;
        private bool _isSpecialAbility;

        // Cached references
        private Transform _playerTransform;
        private GameManager _gameManager;

        // ============================================
        // DEPENDENCY INJECTION
        // ============================================

        [Inject]
        public void Construct(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        // ============================================
        // UNITY LIFECYCLE
        // ============================================

        private void Awake()
        {
            if (_mainCamera == null)
                _mainCamera = Camera.main;
        }

        private void Update()
        {
            // Null checks for new Input System devices
            if (Keyboard.current == null && Mouse.current == null) return;

            UpdateAimInput();
            UpdateFireInput();
            UpdateSpecialInput();
            UpdateWeaponSlotInput();
        }

        // ============================================
        // INPUT UPDATES
        // ============================================

        private void UpdateAimInput()
        {
            if (!_useMouseForAim || _mainCamera == null || Mouse.current == null) return;

            // Cache player transform - only search once
            if (_playerTransform == null)
            {
                if (_gameManager != null && _gameManager.Player != null)
                    _playerTransform = _gameManager.Player.transform;
            }

            if (_playerTransform != null)
            {
                Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
                Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, _mainCamera.nearClipPlane));
                _aimDirection = ((Vector2)mouseWorldPos - (Vector2)_playerTransform.position).normalized;
            }
        }

        private void UpdateFireInput()
        {
            bool wasFiring = _isFiring;

            // Left mouse button or space
            bool mousePressed = Mouse.current != null && Mouse.current.leftButton.isPressed;
            bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
            _isFiring = mousePressed || spacePressed;

            // Fire events
            if (_isFiring && !wasFiring)
            {
                OnFirePressed?.Invoke();
            }
            else if (!_isFiring && wasFiring)
            {
                OnFireReleased?.Invoke();
            }
        }

        private void UpdateSpecialInput()
        {
            bool wasSpecial = _isSpecialAbility;

            bool shiftPressed = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
            bool rightClickPressed = Mouse.current != null && Mouse.current.rightButton.isPressed;
            _isSpecialAbility = shiftPressed || rightClickPressed;

            // Event on press
            if (_isSpecialAbility && !wasSpecial)
            {
                OnSpecialAbilityPressed?.Invoke();
            }
        }

        /// <summary>
        /// Weapon slot selection via number keys 1-4.
        /// Maps to LA-1, LA-2, LA-3, LA-4 (Laser Ammo types).
        /// </summary>
        private void UpdateWeaponSlotInput()
        {
            if (Keyboard.current == null) return;

            // Keys 1-4 select weapon slots 0-3
            if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame)
            {
                OnWeaponSlotSelected?.Invoke(0);
            }
            else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame)
            {
                OnWeaponSlotSelected?.Invoke(1);
            }
            else if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame)
            {
                OnWeaponSlotSelected?.Invoke(2);
            }
            else if (Keyboard.current.digit4Key.wasPressedThisFrame || Keyboard.current.numpad4Key.wasPressedThisFrame)
            {
                OnWeaponSlotSelected?.Invoke(3);
            }
        }
    }
}
