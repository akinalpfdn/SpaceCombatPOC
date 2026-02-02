// ============================================
// INPUT SYSTEM - Strategy Pattern
// Supports keyboard, mouse, and touch input
// Easy to swap input providers
// ============================================

using System;
using UnityEngine;
using VContainer;
using SpaceCombat.Interfaces;
using SpaceCombat.Core;

namespace SpaceCombat.Input
{
    /// <summary>
    /// Keyboard and mouse input provider
    /// For desktop/testing
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

        private void OnDestroy()
        {
        }
    }

    /// <summary>
    /// Touch input provider for mobile
    /// Supports virtual joystick and tap to fire
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

    /// <summary>
    /// Input manager that handles switching between input types
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        [SerializeField] private InputType _preferredInput = InputType.Auto;
        [SerializeField] private KeyboardInputProvider _keyboardProvider;
        [SerializeField] private TouchInputProvider _touchProvider;

        public enum InputType { Auto, Keyboard, Touch }

        private void Start()
        {
            SetupInput();
        }

        private void SetupInput()
        {
            bool isMobile = Application.isMobilePlatform;
            
            switch (_preferredInput)
            {
                case InputType.Auto:
                    if (isMobile)
                        EnableTouchInput();
                    else
                        EnableKeyboardInput();
                    break;
                case InputType.Keyboard:
                    EnableKeyboardInput();
                    break;
                case InputType.Touch:
                    EnableTouchInput();
                    break;
            }
        }

        private void EnableKeyboardInput()
        {
            if (_keyboardProvider == null)
            {
                _keyboardProvider = gameObject.AddComponent<KeyboardInputProvider>();
            }
            _keyboardProvider.enabled = true;

            if (_touchProvider != null)
                _touchProvider.enabled = false;
        }

        private void EnableTouchInput()
        {
            if (_touchProvider == null)
            {
                _touchProvider = gameObject.AddComponent<TouchInputProvider>();
            }
            _touchProvider.enabled = true;

            if (_keyboardProvider != null)
                _keyboardProvider.enabled = false;
        }
    }
}
