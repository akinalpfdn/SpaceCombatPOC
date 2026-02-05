// ============================================
// MOBILE HUD MANAGER - Main mobile UI controller
// Coordinates all mobile HUD elements
// Uses new Input System for touch detection
// ============================================

using UnityEngine;
using UnityEngine.InputSystem;
using StarReapers.Entities;
using StarReapers.Combat;
using StarReapers.Movement;
using StarReapers.Input;

namespace StarReapers.UI.Mobile
{
    /// <summary>
    /// Central manager for mobile HUD elements.
    /// Coordinates joystick, attack button, weapon slots, and health bars.
    ///
    /// Responsibilities:
    /// - Find and connect to player when spawned
    /// - Initialize all HUD components with player reference
    /// - Handle platform-specific visibility
    /// </summary>
    public class MobileHUDManager : MonoBehaviour
    {
        // ============================================
        // CONFIGURATION
        // ============================================

        [Header("Control References")]
        [SerializeField] private VirtualJoystick _joystick;
        [SerializeField] private AttackButton _attackButton;

        [Header("HUD References")]
        [SerializeField] private WeaponSlotBar _weaponSlotBar;
        [SerializeField] private MobileHealthBar _healthBar;

        [Header("Canvas")]
        [SerializeField] private Canvas _hudCanvas;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Platform Settings")]
        [SerializeField] private bool _enableInEditor = true;
        [SerializeField] private bool _hideOnDesktop = false;

        [Header("Future: Skill Buttons")]
        [SerializeField] private UnityEngine.UI.Button[] _skillButtons;

        // ============================================
        // RUNTIME STATE
        // ============================================

        private PlayerShip _playerShip;
        private ShipMovement _shipMovement;
        private TargetSelector _targetSelector;
        private bool _isInitialized;
        private bool _isMobilePlatform;

        // ============================================
        // UNITY LIFECYCLE
        // ============================================

        private void Awake()
        {
            _isMobilePlatform = IsMobilePlatform();

            // Determine visibility based on platform
            bool shouldShow = _isMobilePlatform || _enableInEditor;

            if (_hideOnDesktop && !_isMobilePlatform && !Application.isEditor)
            {
                shouldShow = false;
            }

            SetHUDVisible(shouldShow);
        }

        private void Start()
        {
            // Try to find player immediately
            TryFindPlayer();
        }

        private void Update()
        {
            // Keep trying to find player if not initialized
            if (!_isInitialized)
            {
                TryFindPlayer();
            }

            // Handle joystick input
            if (_isInitialized && _joystick != null && _shipMovement != null)
            {
                if (_joystick.IsActive && _joystick.Direction.magnitude > 0.01f)
                {
                    _shipMovement.Move(_joystick.Direction);
                }
            }
        }

        // ============================================
        // PUBLIC METHODS
        // ============================================

        /// <summary>
        /// Show or hide the entire mobile HUD.
        /// </summary>
        public void SetHUDVisible(bool visible)
        {
            if (_hudCanvas != null)
            {
                _hudCanvas.enabled = visible;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.interactable = visible;
                _canvasGroup.blocksRaycasts = visible;
            }
        }

        /// <summary>
        /// Manually set player reference (alternative to auto-find).
        /// </summary>
        public void SetPlayer(PlayerShip player)
        {
            _playerShip = player;
            InitializeWithPlayer();
        }

        /// <summary>
        /// Force refresh of player connection.
        /// Call this if player respawns.
        /// </summary>
        public void RefreshPlayerConnection()
        {
            _isInitialized = false;
            TryFindPlayer();
        }

        // ============================================
        // PRIVATE METHODS
        // ============================================

        private void TryFindPlayer()
        {
            if (_isInitialized) return;

            // Find player by tag
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null) return;

            // Get components - check root object first, then try to find in hierarchy
            _playerShip = playerObj.GetComponent<PlayerShip>();
            if (_playerShip == null)
            {
                _playerShip = playerObj.GetComponentInParent<PlayerShip>();
            }
            if (_playerShip == null)
            {
                _playerShip = playerObj.GetComponentInChildren<PlayerShip>();
            }

            if (_playerShip == null)
            {
                Debug.LogWarning("[MobileHUDManager] PlayerShip component not found on Player object");
                return;
            }

            _shipMovement = _playerShip.GetComponent<ShipMovement>();
            _targetSelector = _playerShip.GetComponent<TargetSelector>();

            InitializeWithPlayer();
        }

        private void InitializeWithPlayer()
        {
            if (_playerShip == null) return;

            Debug.Log($"[MobileHUDManager] Initializing with player: {_playerShip.name}");

            // Initialize weapon slot bar
            if (_weaponSlotBar != null)
            {
                _weaponSlotBar.Initialize(_playerShip);
            }

            // Initialize attack button
            if (_attackButton != null && _targetSelector != null)
            {
                _attackButton.SetTargetSelector(_targetSelector);
            }

            // Disable mouse movement if mobile controls are active
            if (_targetSelector != null && (_isMobilePlatform || _enableInEditor))
            {
                _targetSelector.SetMouseMovementEnabled(false);
            }

            _isInitialized = true;

            Debug.Log("[MobileHUDManager] Mobile HUD initialized successfully");
        }

        private bool IsMobilePlatform()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                case RuntimePlatform.IPhonePlayer:
                    return true;

                case RuntimePlatform.WebGLPlayer:
                    // Check for touch support (new Input System)
                    return Touchscreen.current != null;

                default:
                    return false;
            }
        }

        // ============================================
        // EDITOR HELPERS
        // ============================================

#if UNITY_EDITOR
        [ContextMenu("Force Initialize")]
        private void EditorForceInitialize()
        {
            RefreshPlayerConnection();
        }

        [ContextMenu("Toggle HUD Visibility")]
        private void EditorToggleVisibility()
        {
            bool currentVisible = _hudCanvas != null && _hudCanvas.enabled;
            SetHUDVisible(!currentVisible);
        }
#endif
    }
}
