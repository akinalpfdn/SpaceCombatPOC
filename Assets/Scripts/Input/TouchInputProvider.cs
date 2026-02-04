// ============================================
// TOUCH INPUT PROVIDER
// Touch input for mobile devices
// ============================================

using System;
using UnityEngine;
using SpaceCombat.Interfaces;

namespace SpaceCombat.Input
{
    /// <summary>
    /// Touch input provider for mobile.
    /// Supports virtual joystick and tap to fire.
    /// </summary>
    public class TouchInputProvider : MonoBehaviour, IInputProvider
    {
        [Header("Joystick Settings")]
        [SerializeField] private float _joystickRadius = 100f;
        [SerializeField] private RectTransform _joystickBase;
        [SerializeField] private RectTransform _joystickHandle;

        [Header("Fire Settings")]
        [SerializeField] private bool _autoFire = false;
        [SerializeField] private float _autoFireDelay = 0.5f;

        // Properties
        public Vector2 MovementInput => _movementInput;
        public Vector2 AimDirection => _aimDirection;
        public bool IsFiring => _isFiring;
        public bool IsSpecialAbility => _isSpecialAbility;

        // Events
        public event Action OnFirePressed;
        public event Action OnFireReleased;
        public event Action OnSpecialAbilityPressed;
        public event Action<int> OnWeaponSlotSelected;  // Not used on mobile yet

        // State
        private Vector2 _movementInput;
        private Vector2 _aimDirection = Vector2.up;
        private bool _isFiring;
        private bool _isSpecialAbility;

        private int _movementFingerId = -1;
        private int _fireFingerId = -1;
        private Vector2 _joystickStartPos;
        private float _autoFireTimer;

        private void Awake()
        {
        }

        private void Update()
        {
            ProcessTouches();
            UpdateAutoFire();
        }

        private void ProcessTouches()
        {
            // Reset fire state each frame (will be set by touches)
            bool wasFiring = _isFiring;

            foreach (Touch touch in UnityEngine.Input.touches)
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

            // Handle desktop mouse for testing
            if (UnityEngine.Input.touchCount == 0)
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
            // Left half of screen = movement
            if (touch.position.x < Screen.width * 0.5f)
            {
                if (_movementFingerId == -1)
                {
                    _movementFingerId = touch.fingerId;
                    _joystickStartPos = touch.position;

                    if (_joystickBase != null)
                    {
                        _joystickBase.position = touch.position;
                        _joystickBase.gameObject.SetActive(true);
                    }
                }
            }
            // Right half = fire
            else
            {
                if (_fireFingerId == -1)
                {
                    _fireFingerId = touch.fingerId;
                    _isFiring = true;
                }
            }
        }

        private void HandleTouchMoved(Touch touch)
        {
            if (touch.fingerId == _movementFingerId)
            {
                Vector2 delta = touch.position - _joystickStartPos;

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
            else if (touch.fingerId == _fireFingerId)
            {
                _isFiring = true;
            }
        }

        private void HandleTouchEnded(Touch touch)
        {
            if (touch.fingerId == _movementFingerId)
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
            else if (touch.fingerId == _fireFingerId)
            {
                _fireFingerId = -1;
                _isFiring = false;
            }
        }

        private void HandleMouseInput()
        {
            // WASD for movement
            _movementInput = new Vector2(
                UnityEngine.Input.GetAxisRaw("Horizontal"),
                UnityEngine.Input.GetAxisRaw("Vertical")
            );

            if (_movementInput.magnitude > 0.1f)
            {
                _aimDirection = _movementInput.normalized;
            }

            _isFiring = UnityEngine.Input.GetMouseButton(0);
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

        private void OnDestroy()
        {
        }
    }
}
