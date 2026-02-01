// ============================================
// BASE ENTITY - Template Method Pattern
// Common functionality for all damageable entities
// ============================================

using System;
using UnityEngine;
using SpaceCombat.Interfaces;
using SpaceCombat.Events;

namespace SpaceCombat.Entities
{
    /// <summary>
    /// Base class for all game entities (player, enemies)
    /// Implements common health/damage functionality
    /// Uses Template Method pattern for customizable behavior
    /// </summary>
    public abstract class BaseEntity : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] protected float _maxHealth = 100f;
        [SerializeField] protected float _currentHealth;

        [Header("Shield Settings")]
        [SerializeField] protected float _maxShield = 0f;
        [SerializeField] protected float _currentShield;
        [SerializeField] protected float _shieldRegenRate = 5f;
        [SerializeField] protected float _shieldRegenDelay = 3f;

        [Header("Damage Feedback")]
        [SerializeField] protected SpriteRenderer _spriteRenderer;
        [SerializeField] protected float _flashDuration = 0.1f;
        [SerializeField] protected Color _damageFlashColor = Color.red;

        // Properties
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public float CurrentShield => _currentShield;
        public float MaxShield => _maxShield;
        public bool IsAlive => _currentHealth > 0;
        public bool HasShield => _maxShield > 0;
        public bool IsAtFullHealth => _currentHealth >= _maxHealth;

        // Events
        public event Action<float, float> OnHealthChanged;
        public event Action OnDeath;
        public event Action<float, float> OnShieldChanged;

        // Internal state
        protected float _lastDamageTime;
        protected bool _isInvincible;
        protected Color _originalColor;

        protected virtual void Awake()
        {
            _currentHealth = _maxHealth;
            _currentShield = _maxShield;
            
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();
            
            if (_spriteRenderer != null)
                _originalColor = _spriteRenderer.color;
        }

        protected virtual void Update()
        {
            RegenerateShield();
        }

        /// <summary>
        /// Take damage with shield absorption
        /// </summary>
        public virtual void TakeDamage(float amount, DamageType damageType = DamageType.Normal)
        {
            if (!IsAlive || _isInvincible) return;

            float damageToHealth = amount;
            float damageToShield = 0f;

            // Shield absorbs damage first
            if (_currentShield > 0)
            {
                damageToShield = Mathf.Min(_currentShield, amount);
                _currentShield -= damageToShield;
                damageToHealth = amount - damageToShield;
                
                OnShieldChanged?.Invoke(_currentShield, _maxShield);
                OnShieldDamaged(damageToShield, damageType);
            }

            if (damageToHealth > 0)
            {
                _currentHealth = Mathf.Max(0, _currentHealth - damageToHealth);
                OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
                OnHealthDamaged(damageToHealth, damageType);
            }

            _lastDamageTime = Time.time;

            // Visual feedback
            ShowDamageFlash();

            // Publish damage event - 3D: convert Vector3 to Vector2 (x, z)
            Vector3 pos = transform.position;
            EventBus.Publish(new DamageEvent(
                gameObject, null, amount, damageType, new Vector2(pos.x, pos.z)
            ));

            if (!IsAlive)
            {
                Die();
            }
        }

        /// <summary>
        /// Heal the entity
        /// </summary>
        public virtual void Heal(float amount)
        {
            if (!IsAlive) return;

            float oldHealth = _currentHealth;
            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            
            if (_currentHealth != oldHealth)
            {
                OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
                OnHealed(amount);
            }
        }

        /// <summary>
        /// Restore shield
        /// </summary>
        public virtual void RestoreShield(float amount)
        {
            if (!HasShield || !IsAlive) return;

            float oldShield = _currentShield;
            _currentShield = Mathf.Min(_maxShield, _currentShield + amount);
            
            if (_currentShield != oldShield)
            {
                OnShieldChanged?.Invoke(_currentShield, _maxShield);
            }
        }

        /// <summary>
        /// Set invincibility state
        /// </summary>
        public void SetInvincible(bool invincible)
        {
            _isInvincible = invincible;
        }

        /// <summary>
        /// Handle death - Template Method
        /// </summary>
        protected virtual void Die()
        {
            OnDeath?.Invoke();
            OnDeathEffect();

            // 3D: convert Vector3 position to Vector2 (x, z)
            Vector3 pos = transform.position;
            EventBus.Publish(new EntityDeathEvent(
                gameObject, new Vector2(pos.x, pos.z), this is PlayerShip, GetScoreValue()
            ));
        }

        /// <summary>
        /// Regenerate shield over time
        /// </summary>
        protected virtual void RegenerateShield()
        {
            if (!HasShield || !IsAlive) return;
            if (_currentShield >= _maxShield) return;
            if (Time.time - _lastDamageTime < _shieldRegenDelay) return;

            _currentShield = Mathf.Min(_maxShield, _currentShield + _shieldRegenRate * Time.deltaTime);
            OnShieldChanged?.Invoke(_currentShield, _maxShield);
        }

        /// <summary>
        /// Show damage flash effect (DISABLED - hit effects only)
        /// </summary>
        protected virtual void ShowDamageFlash()
        {
            // Damage flash disabled - using hit effect prefab instead
            // if (_spriteRenderer != null)
            // {
            //     StopAllCoroutines();
            //     StartCoroutine(DamageFlashCoroutine());
            // }
        }

        private System.Collections.IEnumerator DamageFlashCoroutine()
        {
            _spriteRenderer.color = _damageFlashColor;
            yield return new WaitForSeconds(_flashDuration);
            _spriteRenderer.color = _originalColor;
        }

        // ============================================
        // TEMPLATE METHODS - Override in subclasses
        // ============================================

        /// <summary>
        /// Called when shield takes damage
        /// </summary>
        protected virtual void OnShieldDamaged(float amount, DamageType damageType)
        {
            // Play shield hit sound/effect - 3D: convert Vector3 to Vector2 (x, z)
            Vector3 pos = transform.position;
            EventBus.Publish(new PlaySFXEvent("shield_hit", new Vector2(pos.x, pos.z)));
        }

        /// <summary>
        /// Called when health takes damage
        /// </summary>
        protected virtual void OnHealthDamaged(float amount, DamageType damageType)
        {
            // Play hull hit sound/effect - 3D: convert Vector3 to Vector2 (x, z)
            Vector3 pos = transform.position;
            EventBus.Publish(new PlaySFXEvent("hull_hit", new Vector2(pos.x, pos.z)));
        }

        /// <summary>
        /// Called when entity is healed
        /// </summary>
        protected virtual void OnHealed(float amount)
        {
            // Override for heal effects
        }

        /// <summary>
        /// Called when entity dies - spawn effects
        /// </summary>
        protected virtual void OnDeathEffect()
        {
            // Override to spawn explosion effects - 3D: convert Vector3 to Vector2 (x, z)
            Vector3 pos = transform.position;
            EventBus.Publish(new ExplosionEvent(new Vector2(pos.x, pos.z), 1f, ExplosionSize.Medium));
        }

        /// <summary>
        /// Get score value for this entity
        /// </summary>
        protected virtual int GetScoreValue()
        {
            return 0;
        }
        protected void NotifyHealthChanged()
        {
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        protected void NotifyShieldChanged()
        {
            OnShieldChanged?.Invoke(_currentShield, _maxShield);
        }
        /// <summary>
        /// Initialize entity with config
        /// </summary>
        public virtual void Initialize(float maxHealth, float maxShield = 0)
        {
            _maxHealth = maxHealth;
            _maxShield = maxShield;
            _currentHealth = _maxHealth;
            _currentShield = _maxShield;
            
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            OnShieldChanged?.Invoke(_currentShield, _maxShield);
        }
    }
}
