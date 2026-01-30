// ============================================
// PROJECTILE - Poolable high-performance projectile
// Handles movement, collision, and damage dealing
// ============================================

using UnityEngine;
using SpaceCombat.Interfaces;
using SpaceCombat.Events;

namespace SpaceCombat.Combat
{
    /// <summary>
    /// Base projectile class - supports object pooling
    /// Handles movement, collision detection, and damage
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Projectile : MonoBehaviour, IProjectile
    {
        [Header("Configuration")]
        [SerializeField] private float _damage = 10f;
        [SerializeField] private float _speed = 20f;
        [SerializeField] private float _lifetime = 3f;
        [SerializeField] private DamageType _damageType = DamageType.Normal;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private TrailRenderer _trailRenderer;

        [Header("Effects")]
        [SerializeField] private GameObject _hitEffectPrefab;
        [SerializeField] private string _hitSoundId = "projectile_hit";

        // Components
        private Rigidbody2D _rigidbody;
        private Collider2D _collider;

        // State
        private Vector2 _direction;
        private LayerMask _targetLayers;
        private float _spawnTime;
        private GameObject _owner;
        private bool _isActive;

        // IPoolable
        public bool IsActive => _isActive;

        // IProjectile
        public float Damage => _damage;
        public float Speed => _speed;
        public DamageType DamageType => _damageType;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
            
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            // Configure rigidbody
            _rigidbody.gravityScale = 0f;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            
            // Make collider a trigger
            _collider.isTrigger = true;
        }

        private void Update()
        {
            // Check lifetime
            if (_isActive && Time.time - _spawnTime >= _lifetime)
            {
                Despawn();
            }
        }

        private void FixedUpdate()
        {
            if (_isActive)
            {
                // Move projectile
                _rigidbody.linearVelocity = _direction * _speed;

                // Raycast ahead for fast-moving projectiles to prevent pass-through
                CheckRaycastCollision();
            }
        }

        private void CheckRaycastCollision()
        {
            float distance = _speed * Time.fixedDeltaTime * 2f; // Check 2 frames ahead
            RaycastHit2D hit = Physics2D.Raycast(transform.position, _direction, distance, _targetLayers);

            if (hit.collider != null)
            {
                // Don't hit owner
                if (_owner != null && hit.collider.gameObject == _owner)
                    return;

                // Deal damage
                var damageable = hit.collider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(_damage, _damageType);

                    // Spawn hit effect
                    SpawnHitEffect(hit.point);

                    // Play hit sound
                    EventBus.Publish(new PlaySFXEvent(_hitSoundId, transform.position));
                }

                // Despawn
                Despawn();
            }
        }

        /// <summary>
        /// Initialize projectile with direction and stats
        /// </summary>
        public void Initialize(Vector2 direction, float damage, float speed, LayerMask targetLayers)
        {
            _direction = direction.normalized;
            _damage = damage;
            _speed = speed;
            _targetLayers = targetLayers;
            _spawnTime = Time.time;
            _isActive = true;

            // Set rotation to face direction
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            // Set initial velocity
            _rigidbody.linearVelocity = _direction * _speed;
        }

        /// <summary>
        /// Set projectile color
        /// </summary>
        public void SetColor(Color color)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = color;
            }

            if (_trailRenderer != null)
            {
                _trailRenderer.startColor = color;
                _trailRenderer.endColor = new Color(color.r, color.g, color.b, 0f);
            }
        }

        /// <summary>
        /// Set projectile sprite
        /// </summary>
        public void SetSprite(Sprite sprite)
        {
            if (_spriteRenderer != null && sprite != null)
            {
                _spriteRenderer.sprite = sprite;
            }
        }

        /// <summary>
        /// Set projectile scale
        /// </summary>
        public void SetScale(float scale)
        {
            transform.localScale = Vector3.one * scale;
        }

        /// <summary>
        /// Set damage type
        /// </summary>
        public void SetDamageType(DamageType type)
        {
            _damageType = type;
        }

        /// <summary>
        /// Set owner (to prevent self-damage)
        /// </summary>
        public void SetOwner(GameObject owner)
        {
            _owner = owner;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isActive) return;

            // Check if this is a valid target
            if ((_targetLayers.value & (1 << other.gameObject.layer)) == 0)
                return;

            // Don't hit owner
            if (_owner != null && other.gameObject == _owner)
                return;

            // Try to damage the target
            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(_damage, _damageType);
                
                // Spawn hit effect
                SpawnHitEffect(other.ClosestPoint(transform.position));
                
                // Play hit sound
                EventBus.Publish(new PlaySFXEvent(_hitSoundId, transform.position));
            }

            // Despawn projectile
            Despawn();
        }

        /// <summary>
        /// Spawn hit effect at position
        /// </summary>
        private void SpawnHitEffect(Vector2 position)
        {
            // We only spawn if a prefab is assigned.
            // The old "Color" fallback is removed because HitEffect.cs no longer supports it.
            if (_hitEffectPrefab != null)
            {
                // Use the static Spawn method from your HitEffect script
                // We pass Quaternion.identity for default rotation
                VFX.HitEffect.Spawn(_hitEffectPrefab, position, Quaternion.identity);
            }
        }

        /// <summary>
        /// Return to pool or destroy
        /// </summary>
        private void Despawn()
        {
            _isActive = false;
            
            // Try to return to pool
            var poolManager = Utilities.PoolManager.Instance;
            if (poolManager != null)
            {
                // Find our pool and return
                // This is a simplified approach - in production you'd track the pool reference
                gameObject.SetActive(false);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // ============================================
        // IPoolable Implementation
        // ============================================

        public void OnSpawn()
        {
            _isActive = true;
            _spawnTime = Time.time;
            
            if (_collider != null)
                _collider.enabled = true;
        }

        public void OnDespawn()
        {
            _isActive = false;
            _rigidbody.linearVelocity = Vector2.zero;
            
            if (_collider != null)
                _collider.enabled = false;
        }

        public void ResetState()
        {
            _damage = 10f;
            _speed = 20f;
            _direction = Vector2.up;
            _owner = null;
            _damageType = DamageType.Normal;

            // Reset trail
            if (_trailRenderer != null)
            {
                _trailRenderer.Clear();
            }

            // Reset color
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = Color.white;
            }

            transform.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// Homing projectile variant - follows target
    /// Demonstrates OCP - extends without modifying base
    /// </summary>
    public class HomingProjectile : Projectile
    {
        [Header("Homing Settings")]
        [SerializeField] private float _turnSpeed = 180f;
        [SerializeField] private float _homingRange = 15f;
        [SerializeField] private float _homingDelay = 0.2f;

        private Transform _target;
        private float _homingStartTime;

        private void Start()
        {
            _homingStartTime = Time.time + _homingDelay;
        }

        private void Update()
        {
            // Find target if we don't have one
            if (_target == null && Time.time >= _homingStartTime)
            {
                FindTarget();
            }
        }

        private void FixedUpdate()
        {
            if (!IsActive) return;

            // Home towards target
            if (_target != null && Time.time >= _homingStartTime)
            {
                Vector2 targetDir = ((Vector2)_target.position - (Vector2)transform.position).normalized;
                Vector2 currentDir = transform.up;

                float angle = Vector2.SignedAngle(currentDir, targetDir);
                float maxTurn = _turnSpeed * Time.fixedDeltaTime;
                float actualTurn = Mathf.Clamp(angle, -maxTurn, maxTurn);

                transform.Rotate(0, 0, actualTurn);
            }

            // Apply velocity in forward direction
            GetComponent<Rigidbody2D>().linearVelocity = transform.up * Speed;
        }

        private void FindTarget()
        {
            // Find closest enemy in range
            var colliders = Physics2D.OverlapCircleAll(transform.position, _homingRange);
            float closestDist = float.MaxValue;

            foreach (var col in colliders)
            {
                // Skip if it's the owner or not damageable
                if (col.GetComponent<IDamageable>() == null) continue;

                float dist = Vector2.Distance(transform.position, col.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    _target = col.transform;
                }
            }
        }
    }
}
