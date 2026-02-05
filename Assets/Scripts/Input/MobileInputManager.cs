// ============================================
// MOBILE INPUT MANAGER - Connects mobile UI to gameplay
// Bridges virtual joystick to ship movement
// Platform-aware: active on mobile, optional in editor
// Uses new Input System for touch detection
// ============================================

using UnityEngine;
using UnityEngine.InputSystem;
using StarReapers.Combat;
using StarReapers.Entities;
using StarReapers.Movement;
using StarReapers.UI.Mobile;

namespace StarReapers.Input
{
    /// <summary>
    /// Mobile input manager that connects UI controls to gameplay systems.
    ///
    /// Responsibilities:
    /// - Read input from VirtualJoystick
    /// - Send movement commands to ShipMovement
    /// - Handle platform detection (mobile vs editor)
    ///
    /// Usage:
    /// - Add to a persistent GameObject in the scene
    /// - Assign references to joystick and player ship
    /// - Controls will auto-activate on mobile platforms
    /// </summary>
    public class MobileInputManager : MonoBehaviour
    {
        // ============================================
        // CONFIGURATION
        // ============================================

        [Header("References")]
        [SerializeField] private VirtualJoystick _joystick;
        [SerializeField] private ShipMovement _shipMovement;
        [SerializeField] private GameObject _mobileControlsCanvas;
        [SerializeField] private AttackButton _attackButton;
        [SerializeField] private WeaponSlotBar _weaponSlotBar;

        [Header("Platform Settings")]
        [Tooltip("Enable mobile controls in Unity Editor for testing")]
        [SerializeField] private bool _enableInEditor = true;

        [Header("Movement Settings")]
        [Tooltip("Multiply joystick input for faster response")]
        [SerializeField] private float _inputMultiplier = 1f;

        // ============================================
        // RUNTIME STATE
        // ============================================

        private bool _isMobilePlatform;
        private bool _controlsEnabled;
        private TargetSelector _targetSelector;

        // ============================================
        // UNITY LIFECYCLE
        // ============================================

        private void Awake()
        {
            // Detect platform
            _isMobilePlatform = IsMobilePlatform();

            // Enable controls based on platform
            _controlsEnabled = _isMobilePlatform || _enableInEditor;

            // Show/hide mobile controls canvas
            if (_mobileControlsCanvas != null)
            {
                _mobileControlsCanvas.SetActive(_controlsEnabled);
            }
        }

        private void Start()
        {
            // Try to find player references if not assigned
            // Player is spawned at runtime by GameManager, so we search for it
            TryFindPlayer();

            // Log status for debugging
            if (_controlsEnabled)
            {
                Debug.Log($"[MobileInputManager] Mobile controls enabled. Platform: {Application.platform}");
            }
        }

        /// <summary>
        /// Attempts to find the player and set up references.
        /// Called in Start and can be called again if player respawns.
        /// </summary>
        private void TryFindPlayer()
        {
            // Already have all references
            if (_shipMovement != null && _targetSelector != null) return;

            // Find player by tag (spawned at runtime)
            var player = GameObject.FindWithTag("Player");
            if (player == null) return;

            Debug.Log($"[MobileInputManager] Player found: {player.name}");

            // Set up ShipMovement reference
            if (_shipMovement == null)
            {
                _shipMovement = player.GetComponent<ShipMovement>();
            }

            // Get TargetSelector for both AttackButton and mouse control toggle
            if (_targetSelector == null)
            {
                _targetSelector = player.GetComponent<TargetSelector>();

                if (_targetSelector == null)
                {
                    Debug.LogError("[MobileInputManager] TargetSelector not found on Player!");
                    return;
                }

                // Set up AttackButton's TargetSelector reference
                if (_attackButton != null)
                {
                    _attackButton.SetTargetSelector(_targetSelector);
                }

                // Set up WeaponSlotBar with PlayerShip reference
                if (_weaponSlotBar != null)
                {
                    var playerShip = player.GetComponent<PlayerShip>();
                    if (playerShip != null)
                    {
                        _weaponSlotBar.Initialize(playerShip);
                    }
                }

                // Disable mouse movement when mobile controls are active
                if (_controlsEnabled)
                {
                    _targetSelector.SetMouseMovementEnabled(false);
                    Debug.Log("[MobileInputManager] Mouse movement disabled - using joystick");
                }
            }
        }

        private void Update()
        {
            if (!_controlsEnabled) return;

            // Try to find player if references are missing (player spawns at runtime)
            if (_shipMovement == null || _targetSelector == null)
            {
                TryFindPlayer();
                if (_shipMovement == null) return; // Wait until player is found
            }

            if (_joystick == null) return;

            // Read joystick input
            Vector2 input = _joystick.Direction * _inputMultiplier;

            // Only send movement if joystick is active (being touched)
            if (_joystick.IsActive && input.magnitude > 0.01f)
            {
                _shipMovement.Move(input);
            }
            // Note: Don't call Stop() here - let TargetSelector handle mouse movement
            // This allows both joystick and mouse to coexist
        }

        // ============================================
        // PLATFORM DETECTION
        // ============================================

        private bool IsMobilePlatform()
        {
            // Check for mobile platforms
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                case RuntimePlatform.IPhonePlayer:
                    return true;

                // WebGL might be on mobile browser
                case RuntimePlatform.WebGLPlayer:
                    // Check for touch support (new Input System)
                    return Touchscreen.current != null;

                default:
                    return false;
            }
        }

        // ============================================
        // PUBLIC METHODS
        // ============================================

        /// <summary>
        /// Enable or disable mobile controls at runtime.
        /// Also toggles mouse movement accordingly.
        /// </summary>
        public void SetControlsEnabled(bool enabled)
        {
            _controlsEnabled = enabled;

            if (_mobileControlsCanvas != null)
            {
                _mobileControlsCanvas.SetActive(enabled);
            }

            // Toggle mouse movement (inverse of mobile controls)
            if (_targetSelector != null)
            {
                _targetSelector.SetMouseMovementEnabled(!enabled);
            }
        }

        /// <summary>
        /// Set the virtual joystick reference at runtime.
        /// </summary>
        public void SetJoystick(VirtualJoystick joystick)
        {
            _joystick = joystick;
        }

        /// <summary>
        /// Set the ship movement reference at runtime.
        /// </summary>
        public void SetShipMovement(ShipMovement shipMovement)
        {
            _shipMovement = shipMovement;
        }

        /// <summary>
        /// Check if mobile controls are currently enabled.
        /// </summary>
        public bool AreControlsEnabled => _controlsEnabled;

        /// <summary>
        /// Check if running on a mobile platform.
        /// </summary>
        public bool IsMobile => _isMobilePlatform;
    }
}
