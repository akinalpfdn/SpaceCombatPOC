// ============================================
// PLAYER SHIP - Composes multiple systems
// Uses composition over inheritance for flexibility
// ============================================

using UnityEngine;
using SpaceCombat.Interfaces;
using SpaceCombat.Events;
using SpaceCombat.ScriptableObjects;
using SpaceCombat.Combat;
using SpaceCombat.Movement;
using SpaceCombat.Core;

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

        // Properties from IMovable
        public Vector2 Velocity => _movement?.Velocity ?? Vector2.zero;
        public float MaxSpeed => _config?.maxSpeed ?? 10f;

        // Input provider - set externally or via ServiceLocator
        private IInputProvider _inputProvider;

        // State
        private bool _isFiring;
        private float _invincibilityEndTime;

        protected override void Awake()
        {
            base.Awake();
            
            // Setup components
            SetupMovement();
            SetupWeapons();
            SetupVisuals();
        }

        private void Start()
        {
            // Get input provider
            if (ServiceLocator.TryGet<IInputProvider>(out var inputProvider))
            {
                SetInputProvider(inputProvider);
            }

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

            UpdateShieldVisual();
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
            }

            _inputProvider = provider;

            if (_inputProvider != null)
            {
                _inputProvider.OnFirePressed += OnFirePressed;
                _inputProvider.OnFireReleased += OnFireReleased;
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
            
            // Additional shield hit effects
            if (_config != null)
            {
                EventBus.Publish(new PlaySFXEvent(_config.shieldHitSoundId, transform.position));
            }
        }

        protected override void OnHealthDamaged(float amount, DamageType damageType)
        {
            base.OnHealthDamaged(amount, damageType);
            
            // Camera shake, screen flash, etc.
            if (_config != null)
            {
                EventBus.Publish(new PlaySFXEvent(_config.hullHitSoundId, transform.position));
            }

            // Publish player health event for UI
            EventBus.Publish(new PlayerHealthChangedEvent(_currentHealth, _maxHealth));
        }

        protected override void OnDeathEffect()
        {
            base.OnDeathEffect();
            
            if (_config != null)
            {
                EventBus.Publish(new PlaySFXEvent(_config.explosionSoundId, transform.position));
            }

            // Large explosion
            EventBus.Publish(new ExplosionEvent(transform.position, 2f, ExplosionSize.Large));
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
            // Subscribe to game events if needed
        }

        private void UnsubscribeFromEvents()
        {
            if (_inputProvider != null)
            {
                _inputProvider.OnFirePressed -= OnFirePressed;
                _inputProvider.OnFireReleased -= OnFireReleased;
            }
        }
    }
}
