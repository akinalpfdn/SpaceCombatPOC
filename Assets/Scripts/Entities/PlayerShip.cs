// ============================================
// PLAYER SHIP - Composes multiple systems
// Uses composition over inheritance for flexibility
// ============================================

using UnityEngine;
using VContainer;
using SpaceCombat.Interfaces;
using SpaceCombat.Events;
using SpaceCombat.ScriptableObjects;
using SpaceCombat.Combat;
using SpaceCombat.Movement;
using SpaceCombat.Core;
using SpaceCombat.VFX.Shield;

namespace SpaceCombat.Entities
{
    /// <summary>
    /// Player-controlled ship
    /// Coordinates input, movement, and combat systems
    /// 3D Version - Movement on XZ plane
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class PlayerShip : BaseEntity, IMovable
    {
        [Header("Configuration")]
        [SerializeField] private ShipConfig _config;
        [SerializeField] private bool _useConfigSettings = true;

        [Header("Components")]
        [SerializeField] private ShipMovement _movement;
        [SerializeField] private WeaponController _weaponController;
        [SerializeField] private Transform _weaponMountPoint;

        [Header("Effects")]
        [SerializeField] private ParticleSystem _engineTrail;
        [SerializeField] private GameObject _shieldVisual;
        [SerializeField] private ShieldVisualController _shieldVisualController;

        // Properties from IMovable
        public Vector2 Velocity => _movement?.Velocity ?? Vector2.zero;
        public float MaxSpeed => _config?.maxSpeed ?? 10f;

        // Input provider - injected via VContainer
        private IInputProvider _inputProvider;

        // State
        private bool _isFiring;
        private float _invincibilityEndTime;
        private int _currentWeaponSlot;  // 0-3 for LA-1 to LA-4

        // Public properties for UI
        public int CurrentWeaponSlot => _currentWeaponSlot;
        public WeaponConfig CurrentWeaponConfig => _weaponController?.CurrentWeapon;
        public ShipConfig Config => _config;

        protected override void Awake()
        {
            base.Awake();
            
            // Setup components
            SetupMovement();
            SetupWeapons();
            SetupVisuals();
        }

        [Inject]
        public void Construct(IInputProvider inputProvider)
        {
            SetInputProvider(inputProvider);
        }

        private void Start()
        {

            // Apply config if enabled, otherwise use inspector values
            if (_useConfigSettings && _config != null)
            {
                Initialize(_config.maxHealth, _config.maxShield);
                _shieldRegenRate = _config.shieldRegenRate;
                _shieldRegenDelay = _config.shieldRegenDelay;
            }
            // If disabled, inspector values (from BaseEntity Awake) are used

            // Subscribe to events
            SubscribeToEvents();

            // Publish initial health/shield values for UI
            PublishHealthAndShieldEvents();

            // Publish initial weapon slot for UI (so WeaponSlotBar shows correct selection)
            PublishInitialWeaponEvent();
        }

        /// <summary>
        /// Publishes current health and shield values to EventBus for UI updates.
        /// Called on Start and after any health/shield change.
        /// </summary>
        private void PublishHealthAndShieldEvents()
        {
            EventBus.Publish(new PlayerHealthChangedEvent(_currentHealth, _maxHealth));
            EventBus.Publish(new PlayerShieldChangedEvent(_currentShield, _maxShield));
        }

        /// <summary>
        /// Publishes initial weapon slot to EventBus for UI sync.
        /// Called on Start so WeaponSlotBar shows correct selection from game start.
        /// </summary>
        private void PublishInitialWeaponEvent()
        {
            if (_config != null && _config.availableWeapons != null && _config.availableWeapons.Length > 0)
            {
                var currentWeapon = _config.availableWeapons[_currentWeaponSlot];
                EventBus.Publish(new WeaponSwitchedEvent(_currentWeaponSlot, currentWeapon, null));
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        protected override void Update()
        {
            base.Update();
            
            HandleInput();
            UpdateShieldVisual();
            UpdateInvincibility();
        }

        private void FixedUpdate()
        {
            if (_inputProvider != null && _movement != null)
            {
                _movement.ApplyMovement(_inputProvider.MovementInput);
            }
        }

        // ============================================
        // SETUP METHODS
        // ============================================

        private void SetupMovement()
        {
            if (_movement == null)
            {
                _movement = GetComponent<ShipMovement>();
                if (_movement == null)
                {
                    _movement = gameObject.AddComponent<ShipMovement>();
                }
            }

            if (_useConfigSettings && _config != null)
            {
                _movement.Initialize(_config.maxSpeed, _config.acceleration,
                    _config.deceleration, _config.rotationSpeed);
            }
        }

        private void SetupWeapons()
        {
            if (_weaponController == null)
            {
                _weaponController = GetComponent<WeaponController>();
                if (_weaponController == null)
                {
                    _weaponController = gameObject.AddComponent<WeaponController>();
                }
            }

            if (_useConfigSettings && _config != null && _config.availableWeapons.Length > 0)
            {
                _weaponController.Initialize(_config.availableWeapons[0], _weaponMountPoint);
            }
        }

        private void SetupVisuals()
        {
            if (_useConfigSettings && _config != null && _spriteRenderer != null)
            {
                if (_config.shipSprite != null)
                    _spriteRenderer.sprite = _config.shipSprite;
            }

            // Create shield visual at runtime if config has one and we don't already have a controller
            if (_useConfigSettings && _config != null && _config.shieldVisualConfig != null && _shieldVisualController == null)
            {
                CreateShieldVisual(_config.shieldVisualConfig);
            }

            UpdateShieldVisual();
        }

        /// <summary>
        /// Creates shield visual at runtime from config.
        /// This allows all ships to share the same config without manual prefab setup.
        /// </summary>
        private void CreateShieldVisual(ShieldVisualConfig config)
        {
            // Create ShieldVisual child GameObject
            var shieldGO = new GameObject("ShieldVisual");
            shieldGO.transform.SetParent(transform);
            shieldGO.transform.localPosition = Vector3.zero;
            shieldGO.transform.localRotation = Quaternion.identity;

            // Use ship-specific scale from ShipConfig, fallback to config default
            Vector3 scale = (_config != null && _config.shieldScale != Vector3.zero)
                ? _config.shieldScale
                : config.MeshScale;
            shieldGO.transform.localScale = scale;

            // Add required components
            var meshFilter = shieldGO.AddComponent<MeshFilter>();
            var meshRenderer = shieldGO.AddComponent<MeshRenderer>();
            var controller = shieldGO.AddComponent<ShieldVisualController>();

            // Configure mesh - use config mesh or fallback to built-in sphere
            if (config.ShieldMesh != null)
            {
                meshFilter.mesh = config.ShieldMesh;
            }
            else
            {
                // Fallback: create a sphere mesh if none provided
                var sphereGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                meshFilter.mesh = sphereGO.GetComponent<MeshFilter>().sharedMesh;
                Destroy(sphereGO);
            }

            // Configure renderer
            if (config.ShieldMaterial != null)
            {
                meshRenderer.material = config.ShieldMaterial;
            }
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            // Initialize controller with config
            controller.InitializeWithConfig(config, this);

            _shieldVisualController = controller;
        }

        // ============================================
        // INPUT HANDLING
        // ============================================

        public void SetInputProvider(IInputProvider provider)
        {
            if (_inputProvider != null)
            {
                _inputProvider.OnFirePressed -= OnFirePressed;
                _inputProvider.OnFireReleased -= OnFireReleased;
                _inputProvider.OnWeaponSlotSelected -= OnWeaponSlotSelected;
            }

            _inputProvider = provider;

            if (_inputProvider != null)
            {
                _inputProvider.OnFirePressed += OnFirePressed;
                _inputProvider.OnFireReleased += OnFireReleased;
                _inputProvider.OnWeaponSlotSelected += OnWeaponSlotSelected;
            }
        }

        private void HandleInput()
        {
            if (_inputProvider == null) return;

            // Update aim direction (for visual feedback, not firing)
            if (_weaponController != null)
            {
                _weaponController.SetAimDirection(_inputProvider.AimDirection);
            }

            // Firing is now handled by TargetSelector (click to target, not hold to fire)
            // This old input-based firing is disabled
            // if (_isFiring && _weaponController != null)
            // {
            //     _weaponController.TryFire();
            // }

            // Update engine trail
            UpdateEngineTrail();
        }

        private void OnFirePressed()
        {
            _isFiring = true;
        }

        private void OnFireReleased()
        {
            _isFiring = false;
        }

        /// <summary>
        /// Handles weapon slot selection (keys 1-4 for LA-1 to LA-4).
        /// Switches weapon and publishes WeaponSwitchedEvent for UI.
        /// </summary>
        private void OnWeaponSlotSelected(int slotIndex)
        {
            // Validate slot index and config
            if (_config == null || _config.availableWeapons == null) return;
            if (slotIndex < 0 || slotIndex >= _config.availableWeapons.Length) return;

            // Don't switch if already on this slot
            if (slotIndex == _currentWeaponSlot) return;

            var newWeapon = _config.availableWeapons[slotIndex];
            if (newWeapon == null) return;

            // Store old weapon for event
            var oldWeapon = _weaponController?.CurrentWeapon;
            int oldSlot = _currentWeaponSlot;

            // Switch weapon
            _currentWeaponSlot = slotIndex;
            _weaponController?.SwitchWeapon(newWeapon);

            // Publish event for UI and other systems
            EventBus.Publish(new WeaponSwitchedEvent(slotIndex, newWeapon, oldWeapon));

            // Play switch sound
            Vector3 pos = transform.position;
            EventBus.Publish(new PlaySFXEvent("weapon_switch", new Vector2(pos.x, pos.z)));
        }

        /// <summary>
        /// Public method for UI to trigger weapon slot selection.
        /// Called by mobile WeaponSlotButton.
        /// </summary>
        public void SelectWeaponSlot(int slotIndex)
        {
            OnWeaponSlotSelected(slotIndex);
        }

        // ============================================
        // IMovable IMPLEMENTATION
        // ============================================

        public void Move(Vector2 direction)
        {
            _movement?.ApplyMovement(direction);
        }

        public void SetSpeed(float speed)
        {
            _movement?.SetMaxSpeed(speed);
        }

        public void Stop()
        {
            _movement?.Stop();
        }

        // ============================================
        // VISUAL UPDATES
        // ============================================

        private void UpdateShieldVisual()
        {
            // Use ShieldVisualController if available (DarkOrbit-style effects)
            if (_shieldVisualController != null)
            {
                bool showShield = HasShield && _currentShield > 0;
                _shieldVisualController.SetShieldActive(showShield);
                // Color/health updates are handled automatically via OnShieldChanged subscription
                return;
            }

            // Fallback: Legacy sprite-based shield visual
            if (_shieldVisual != null)
            {
                bool showShield = HasShield && _currentShield > 0;
                _shieldVisual.SetActive(showShield);

                if (showShield)
                {
                    float shieldAlpha = _currentShield / _maxShield;
                    var renderer = _shieldVisual.GetComponent<SpriteRenderer>();
                    if (renderer != null)
                    {
                        var color = renderer.color;
                        color.a = shieldAlpha * 0.5f;
                        renderer.color = color;
                    }
                }
            }
        }

        private void UpdateEngineTrail()
        {
            if (_engineTrail == null) return;

            bool isMoving = _inputProvider.MovementInput.magnitude > 0.1f;
            
            var emission = _engineTrail.emission;
            emission.enabled = isMoving;
        }

        private void UpdateInvincibility()
        {
            if (_isInvincible && Time.time >= _invincibilityEndTime)
            {
                SetInvincible(false);
                
                // Stop blinking
                if (_spriteRenderer != null)
                {
                    _spriteRenderer.color = _originalColor;
                }
            }
        }

        // ============================================
        // DAMAGE AND DEATH
        // ============================================

        protected override void OnShieldDamaged(float amount, DamageType damageType)
        {
            base.OnShieldDamaged(amount, damageType);

            // Additional shield hit effects - 3D: convert Vector3 to Vector2 (x, z)
            if (_config != null)
            {
                Vector3 pos = transform.position;
                EventBus.Publish(new PlaySFXEvent(_config.shieldHitSoundId, new Vector2(pos.x, pos.z)));
            }
            // Note: Shield event is published via HandleShieldChanged subscription
        }

        protected override void OnHealthDamaged(float amount, DamageType damageType)
        {
            base.OnHealthDamaged(amount, damageType);

            // Camera shake, screen flash, etc. - 3D: convert Vector3 to Vector2 (x, z)
            if (_config != null)
            {
                Vector3 pos = transform.position;
                EventBus.Publish(new PlaySFXEvent(_config.hullHitSoundId, new Vector2(pos.x, pos.z)));
            }

            // Publish player health event for UI
            EventBus.Publish(new PlayerHealthChangedEvent(_currentHealth, _maxHealth));
        }

        protected override void OnDeathEffect()
        {
            base.OnDeathEffect();

            if (_config != null)
            {
                // 3D: convert Vector3 to Vector2 (x, z)
                Vector3 pos = transform.position;
                EventBus.Publish(new PlaySFXEvent(_config.explosionSoundId, new Vector2(pos.x, pos.z)));
            }

            // Large explosion - 3D: convert Vector3 to Vector2 (x, z)
            Vector3 deathPos = transform.position;
            EventBus.Publish(new ExplosionEvent(new Vector2(deathPos.x, deathPos.z), 2f, ExplosionSize.Large));
        }

        /// <summary>
        /// Respawn the player
        /// </summary>
        public void Respawn(Vector3 position)
        {
            transform.position = position;

            _currentHealth = _maxHealth;
            _currentShield = _maxShield;

            NotifyHealthChanged();
            NotifyShieldChanged();

            // Publish events for UI
            PublishHealthAndShieldEvents();

            // Reset shield visual
            _shieldVisualController?.OnSpawn();

            // Brief invincibility
            SetInvincible(true);
            _invincibilityEndTime = Time.time + 2f;

            gameObject.SetActive(true);

            // Start blinking effect
            StartCoroutine(InvincibilityBlink());
        }

        private System.Collections.IEnumerator InvincibilityBlink()
        {
            while (_isInvincible)
            {
                if (_spriteRenderer != null)
                {
                    _spriteRenderer.enabled = !_spriteRenderer.enabled;
                }
                yield return new WaitForSeconds(0.1f);
            }
            
            if (_spriteRenderer != null)
            {
                _spriteRenderer.enabled = true;
            }
        }

        // ============================================
        // EVENT SUBSCRIPTIONS
        // ============================================

        private void SubscribeToEvents()
        {
            // Subscribe to BaseEntity Actions to forward to EventBus
            OnShieldChanged += HandleShieldChanged;
        }

        private void UnsubscribeFromEvents()
        {
            // Unsubscribe from BaseEntity Actions
            OnShieldChanged -= HandleShieldChanged;

            if (_inputProvider != null)
            {
                _inputProvider.OnFirePressed -= OnFirePressed;
                _inputProvider.OnFireReleased -= OnFireReleased;
                _inputProvider.OnWeaponSlotSelected -= OnWeaponSlotSelected;
            }
        }

        /// <summary>
        /// Handles shield changes from BaseEntity (including regen) and publishes to EventBus.
        /// </summary>
        private void HandleShieldChanged(float current, float max)
        {
            EventBus.Publish(new PlayerShieldChangedEvent(current, max));
        }
    }
}
