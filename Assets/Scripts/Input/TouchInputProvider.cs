// ============================================
// TOUCH INPUT PROVIDER
// Touch input for mobile devices
// Uses new Input System (EnhancedTouch)
// ============================================

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using StarReapers.Interfaces;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace StarReapers.Input
{
    /// <summary>
    /// Touch input provider for mobile.
    /// Supports virtual joystick and tap to fire.
    /// Uses new Input System's EnhancedTouch API.
    /// </summary>
    public class TouchInputProvider : MonoBehaviour, IInputProvider
    {
        // ============================================
        // CONFIGURATION
        // ============================================

        [Header("Joystick Settings")]
        [SerializeField] private float _joystickRadius = 100f;
        [SerializeField] private RectTransform _joystickBase;
        [SerializeField] private RectTransform _joystickHandle;

        [Header("Fire Settings")]
        [SerializeField] private bool _autoFire = false;
        [SerializeField] private float _autoFireDelay = 0.5f;

        // ============================================
        // PROPERTIES
        // ============================================

        public Vector2 MovementInput => _movementInput;
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

        private Vector2 _movementInput;
        private Vector2 _aimDirection = Vector2.up;
        private bool _isFiring;
        private bool _isSpecialAbility;

        private int _movementFingerId = -1;
        private int _fireFingerId = -1;
        private Vector2 _joystickStartPos;
        private float _autoFireTimer;

        // ============================================
        // UNITY LIFECYCLE
        // ============================================

        private void Awake()
        {
            // Enable EnhancedTouch for new Input System
            EnhancedTouchSupport.Enable();
        }

        private void OnDestroy()
        {
            // Disable when not needed (optional, can leave enabled)
            EnhancedTouchSupport.Disable();
        }

        private void Update()
        {
            ProcessTouches();
            UpdateAutoFire();
        }

        // ============================================
        // TOUCH PROCESSING
        // ============================================

        private void ProcessTouches()
        {
            bool wasFiring = _isFiring;

            // Process all active touches using new Input System
            foreach (var touch in Touch.activeTouches)
            {
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        HandleTouchBegan(touch);
                        break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        HandleTouchMoved(touch);
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        HandleTouchEnded(touch);
                        break;
                }
            }

            // Handle desktop mouse for testing (using new Input System)
            if (Touch.activeTouches.Count == 0)
            {
                HandleMouseInput();
            }

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

        private void HandleTouchBegan(Touch touch)
        {
            Vector2 position = touch.screenPosition;

            // Left half of screen = movement
            if (position.x < Screen.width * 0.5f)
            {
                if (_movementFingerId == -1)
                {
                    _movementFingerId = touch.touchId;
                    _joystickStartPos = position;

                    if (_joystickBase != null)
                    {
                        _joystickBase.position = position;
                        _joystickBase.gameObject.SetActive(true);
                    }
                }
            }
            // Right half = fire
            else
            {
                if (_fireFingerId == -1)
                {
                    _fireFingerId = touch.touchId;
                    _isFiring = true;
                }
            }
        }

        private void HandleTouchMoved(Touch touch)
        {
            if (touch.touchId == _movementFingerId)
            {
                Vector2 delta = touch.screenPosition - _joystickStartPos;

                // Clamp to joystick radius
                if (delta.magnitude > _joystickRadius)
                {
                    delta = delta.normalized * _joystickRadius;
                }

                _movementInput = delta / _joystickRadius;
                _aimDirection = _movementInput.normalized;

                // Update visual
                if (_joystickHandle != null)
                {
                    _joystickHandle.position = _joystickStartPos + delta;
                }
            }
            else if (touch.touchId == _fireFingerId)
            {
                _isFiring = true;
            }
        }

        private void HandleTouchEnded(Touch touch)
        {
            if (touch.touchId == _movementFingerId)
            {
                _movementFingerId = -1;
                _movementInput = Vector2.zero;

                if (_joystickBase != null)
                {
                    _joystickBase.gameObject.SetActive(false);
                }
                if (_joystickHandle != null)
                {
                    _joystickHandle.localPosition = Vector3.zero;
                }
            }
            else if (touch.touchId == _fireFingerId)
            {
                _fireFingerId = -1;
                _isFiring = false;
            }
        }

        private void HandleMouseInput()
        {
            if (Mouse.current == null) return;

            // No WASD movement - joystick only
            _movementInput = Vector2.zero;

            // Mouse click for fire (testing on desktop)
            _isFiring = Mouse.current.leftButton.isPressed;
        }

        private void UpdateAutoFire()
        {
            if (_autoFire && _movementInput.magnitude > 0.5f)
            {
                _autoFireTimer += Time.deltaTime;
                if (_autoFireTimer >= _autoFireDelay)
                {
                    _isFiring = true;
                }
            }
            else
            {
                _autoFireTimer = 0f;
            }
        }
    }
}
