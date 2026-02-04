// ============================================
// KEYBOARD INPUT PROVIDER
// Keyboard and mouse input for desktop/testing
// ============================================

using System;
using UnityEngine;
using VContainer;
using SpaceCombat.Interfaces;
using SpaceCombat.Core;

namespace SpaceCombat.Input
{
    /// <summary>
    /// Keyboard and mouse input provider.
    /// For desktop/testing.
    /// </summary>
    public class KeyboardInputProvider : MonoBehaviour, IInputProvider
    {
        [Header("Settings")]
        [SerializeField] private bool _useMouseForAim = true;
        [SerializeField] private Camera _mainCamera;

        // Properties
        public Vector2 MovementInput => _movementInput;
        public Vector2 AimDirection => _aimDirection;
        public bool IsFiring => _isFiring;
        public bool IsSpecialAbility => _isSpecialAbility;

        // Events
        public event Action OnFirePressed;
        public event Action OnFireReleased;
        public event Action OnSpecialAbilityPressed;
        public event Action<int> OnWeaponSlotSelected;

        // State
        private Vector2 _movementInput;
        private Vector2 _aimDirection;
        private bool _isFiring;
        private bool _isSpecialAbility;

        // Cached references
        private Transform _playerTransform;
        private GameManager _gameManager;

        [Inject]
        public void Construct(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        private void Awake()
        {
            if (_mainCamera == null)
                _mainCamera = Camera.main;
        }

        private void Update()
        {
            UpdateMovementInput();
            UpdateAimInput();
            UpdateFireInput();
            UpdateSpecialInput();
            UpdateWeaponSlotInput();
        }

        private void UpdateMovementInput()
        {
            _movementInput = new Vector2(
                UnityEngine.Input.GetAxisRaw("Horizontal"),
                UnityEngine.Input.GetAxisRaw("Vertical")
            );

            // Normalize diagonal movement
            if (_movementInput.magnitude > 1f)
            {
                _movementInput.Normalize();
            }
        }

        private void UpdateAimInput()
        {
            if (_useMouseForAim && _mainCamera != null)
            {
                // Cache player transform - only search once
                if (_playerTransform == null)
                {
                    if (_gameManager != null && _gameManager.Player != null)
                        _playerTransform = _gameManager.Player.transform;
                }

                if (_playerTransform != null)
                {
                    Vector3 mousePos = _mainCamera.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
                    _aimDirection = ((Vector2)mousePos - (Vector2)_playerTransform.position).normalized;
                }
            }
            else
            {
                // Aim in movement direction
                if (_movementInput.magnitude > 0.1f)
                {
                    _aimDirection = _movementInput.normalized;
                }
            }
        }

        private void UpdateFireInput()
        {
            bool wasFiring = _isFiring;

            // Left mouse button or space
            _isFiring = UnityEngine.Input.GetMouseButton(0) || UnityEngine.Input.GetKey(KeyCode.Space);

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
            _isSpecialAbility = UnityEngine.Input.GetKey(KeyCode.LeftShift) ||
                               UnityEngine.Input.GetMouseButton(1);

            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftShift) ||
                UnityEngine.Input.GetMouseButtonDown(1))
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
            // Keys 1-4 select weapon slots 0-3
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1) || UnityEngine.Input.GetKeyDown(KeyCode.Keypad1))
            {
                OnWeaponSlotSelected?.Invoke(0);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2) || UnityEngine.Input.GetKeyDown(KeyCode.Keypad2))
            {
                OnWeaponSlotSelected?.Invoke(1);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3) || UnityEngine.Input.GetKeyDown(KeyCode.Keypad3))
            {
                OnWeaponSlotSelected?.Invoke(2);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha4) || UnityEngine.Input.GetKeyDown(KeyCode.Keypad4))
            {
                OnWeaponSlotSelected?.Invoke(3);
            }
        }

        private void OnDestroy()
        {
        }
    }
}
