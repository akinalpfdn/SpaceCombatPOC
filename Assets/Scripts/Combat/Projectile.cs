// ============================================
// PROJECTILE - Poolable high-performance projectile
// Handles movement, collision, and damage dealing
// Visual rendering delegated to IProjectileVisual
// ============================================

using UnityEngine;
using SpaceCombat.Entities;
using SpaceCombat.Interfaces;
using SpaceCombat.Events;

namespace SpaceCombat.Combat
{
    /// <summary>
    /// Base projectile class - supports object pooling.
    /// Handles movement, collision detection, and damage.
    /// 3D Version - Movement on XZ plane.
    ///
    /// Design Patterns:
    /// - Strategy: Visual rendering delegated to IProjectileVisual
    /// - Object Pool: Implements IPoolable for efficient reuse
    ///
    /// SOLID Principles:
    /// - Single Responsibility: Only physics/damage, visuals delegated
    /// - Open/Closed: New visual styles via IProjectileVisual implementations
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class Projectile : MonoBehaviour, IProjectile
    {
        // ============================================
        // SERIALIZED FIELDS
        // ============================================

        [Header("Configuration")]
        [SerializeField] private float _damage = 10f;
        [SerializeField] private float _speed = 20f;
        [SerializeField] private float _lifetime = 3f;
        [SerializeField] private DamageType _damageType = DamageType.Normal;

        [Header("Effects")]
        [SerializeField] private GameObject _hitEffectPrefab;
        [SerializeField] private string _hitSoundId = "projectile_hit";

        // ============================================
        // RUNTIME STATE
        // ============================================

        private Rigidbody _rigidbody;
        private Collider _collider;
        private IProjectileVisual _projectileVisual;

        private Vector3 _direction;
        private LayerMask _targetLayers;
        private float _spawnTime;
        private GameObject _owner;
        private bool _isActive;

        // Pool reference for proper return
        private Utilities.ObjectPool<Projectile> _pool;

        // ============================================
        // PUBLIC PROPERTIES
        // ============================================

        public bool IsActive => _isActive;
        public float Damage => _damage;
        public float Speed => _speed;
        public DamageType DamageType => _damageType;

        // ============================================
        // UNITY LIFECYCLE
        // ============================================

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();

            // Detect visual component (Strategy pattern)
            _projectileVisual = GetComponent<IProjectileVisual>();

            // Configure rigidbody for 3D
            _rigidbody.useGravity = false;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Make collider a trigger
            _collider.isTrigger = true;
        }

        private void Update()
        {
            if (_isActive && Time.time - _spawnTime >= _lifetime)
            {
                Despawn();
            }
        }

        private void FixedUpdate()
        {
            if (_isActive)
            {
                _rigidbody.linearVelocity = _direction * _speed;
                CheckRaycastCollision();
            }
        }

        // ============================================
        // PUBLIC METHODS
        // ============================================

        /// <summary>
        /// Initialize projectile with direction and stats.
        /// Converts 2D direction to XZ plane.
        /// </summary>
        public void Initialize(Vector2 direction, float damage, float speed, LayerMask targetLayers)
        {
            _direction = new Vector3(direction.x, 0, direction.y).normalized;
            _damage = damage;
            _speed = speed;
            _targetLayers = targetLayers;
            _spawnTime = Time.time;
            _isActive = true;

            if (_direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(_direction);
            }

            _rigidbody.linearVelocity = _direction * _speed;
        }

        /// <summary>
        /// Set projectile color with emission intensity for HDR bloom.
        /// </summary>
        public void SetColor(Color color, float emissionIntensity)
        {
            _projectileVisual?.SetColor(color, emissionIntensity);
        }

        /// <summary>
        /// Set projectile color (uses config's default emission intensity).
        /// </summary>
        public void SetColor(Color color)
        {
            _projectileVisual?.SetColor(color, 0f);
        }

        /// <summary>
        /// Set projectile visual scale.
        /// </summary>
        public void SetScale(float scale)
        {
            if (_projectileVisual != null)
            {
                _projectileVisual.SetScale(scale);
                return;
            }

            transform.localScale = Vector3.one * scale;
        }

        /// <summary>
        /// Set damage type.
        /// </summary>
        public void SetDamageType(DamageType type)
        {
            _damageType = type;
        }

        /// <summary>
        /// Set owner (to prevent self-damage).
        /// </summary>
        public void SetOwner(GameObject owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// Set pool reference so projectile can return itself properly.
        /// Called by WeaponController after getting from pool.
        /// </summary>
        public void SetPool(Utilities.ObjectPool<Projectile> pool)
        {
            _pool = pool;
        }

        // ============================================
        // COLLISION
        // ============================================

        private void CheckRaycastCollision()
        {
            float distance = _speed * Time.fixedDeltaTime * 2f;

            if (Physics.Raycast(transform.position, _direction, out RaycastHit hit, distance, _targetLayers))
            {
                if (_owner != null && hit.collider.gameObject == _owner)
                    return;

                var damageable = hit.collider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    // Check for shield hit before applying damage
                    var entity = hit.collider.GetComponent<BaseEntity>();
                    bool hadShield = entity != null && entity.HasShield && entity.CurrentShield > 0;

                    damageable.TakeDamage(_damage, _damageType);
                    SpawnHitEffect(hit.point);

                    // Publish shield hit event if entity had shield
                    if (hadShield)
                    {
                        EventBus.Publish(new ShieldHitEvent(
                            hit.collider.gameObject,
                            hit.point,
                            _damage,
                            _damageType
                        ));
                    }

                    Vector3 pos = transform.position;
                    EventBus.Publish(new PlaySFXEvent(_hitSoundId, new Vector2(pos.x, pos.z)));
                }

                Despawn();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive) return;

            if ((_targetLayers.value & (1 << other.gameObject.layer)) == 0)
                return;

            if (_owner != null && other.gameObject == _owner)
                return;

            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                // Check for shield hit before applying damage
                var entity = other.GetComponent<BaseEntity>();
                bool hadShield = entity != null && entity.HasShield && entity.CurrentShield > 0;

                Vector3 hitPoint = other.ClosestPoint(transform.position);

                damageable.TakeDamage(_damage, _damageType);
                SpawnHitEffect(hitPoint);

                // Publish shield hit event if entity had shield
                if (hadShield)
                {
                    EventBus.Publish(new ShieldHitEvent(
                        other.gameObject,
                        hitPoint,
                        _damage,
                        _damageType
                    ));
                }

                Vector3 pos = transform.position;
                EventBus.Publish(new PlaySFXEvent(_hitSoundId, new Vector2(pos.x, pos.z)));
            }

            Despawn();
        }

        // ============================================
        // PRIVATE METHODS
        // ============================================

        private void SpawnHitEffect(Vector3 position)
        {
            if (_hitEffectPrefab != null)
            {
                VFX.HitEffect.Spawn(_hitEffectPrefab, position, Quaternion.identity);
            }
        }

        private void Despawn()
        {
            _isActive = false;

            if (_pool != null)
            {
                _pool.Return(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // ============================================
        // IPoolable IMPLEMENTATION
        // ============================================

        public void OnSpawn()
        {
            _isActive = true;
            _spawnTime = Time.time;

            if (_collider != null)
                _collider.enabled = true;

            _projectileVisual?.OnSpawn();
        }

        public void OnDespawn()
        {
            _isActive = false;
            _rigidbody.linearVelocity = Vector3.zero;

            if (_collider != null)
                _collider.enabled = false;

            _projectileVisual?.OnDespawn();
        }

        public void ResetState()
        {
            _damage = 10f;
            _speed = 20f;
            _direction = Vector3.forward;
            _owner = null;
            _damageType = DamageType.Normal;

            _projectileVisual?.ResetVisual();
        }
    }

    /// <summary>
    /// Homing projectile variant - follows target.
    /// Demonstrates OCP - extends without modifying base.
    /// 3D Version - Works on XZ plane.
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
            if (_target == null && Time.time >= _homingStartTime)
            {
                FindTarget();
            }
        }

        private void FixedUpdate()
        {
            if (!IsActive) return;

            if (_target != null && Time.time >= _homingStartTime)
            {
                Vector3 targetDir = (_target.position - transform.position).normalized;
                targetDir.y = 0;

                Quaternion targetRotation = Quaternion.LookRotation(targetDir);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    _turnSpeed * Time.fixedDeltaTime
                );
            }

            GetComponent<Rigidbody>().linearVelocity = transform.forward * Speed;
        }

        private void FindTarget()
        {
            var colliders = Physics.OverlapSphere(transform.position, _homingRange);
            float closestDist = float.MaxValue;

            foreach (var col in colliders)
            {
                if (col.GetComponent<IDamageable>() == null) continue;

                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    _target = col.transform;
                }
            }
        }
    }
}
