// ============================================
// ENEMY AI - State Machine Pattern
// Modular AI with different behavior states
// ============================================

using UnityEngine;
using SpaceCombat.Interfaces;
using SpaceCombat.Events;
using SpaceCombat.ScriptableObjects;

namespace SpaceCombat.Entities
{
    /// <summary>
    /// Base enemy class with state machine AI
    /// Implements IEnemy and IPoolable for performance
    /// 3D Version - Movement locked to XZ plane
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class Enemy : BaseEntity, IEnemy, IPoolable
    {
        [Header("Configuration")]
        [SerializeField] private EnemyConfig _config;

        [Header("AI Settings")]
        [SerializeField] private float _detectionRange = 12f;
        [SerializeField] private float _attackRange = 8f;
        [SerializeField] private float _patrolRadius = 5f;

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _rotationSpeed = 120f;

        [Header("Combat")]
        [SerializeField] private Combat.WeaponController _weaponController;

        // Components
        private Rigidbody _rigidbody;

        // State
        private EnemyState _currentState = EnemyState.Idle;
        private Transform _target;
        private Vector3 _patrolPoint;
        private Vector3 _spawnPosition;
        private float _stateTimer;
        private float _lastFireTime;
        private int _strafeDirection = 1; // 1 = clockwise, -1 = counter-clockwise

        // Properties
        public EnemyState CurrentState => _currentState;
        public Transform Target => _target;
        public bool IsActive => gameObject.activeInHierarchy;
        public float TargetIndicatorScale => _config?.targetIndicatorScale ?? 1f;

        protected override void Awake()
        {
            base.Awake();

            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.useGravity = false;
            _rigidbody.linearDamping = 5f; // Higher = less drift, more responsive
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate; // Smooth movement
            // Lock Y axis and X/Z rotation for 2.5D movement on XZ plane
            _rigidbody.constraints = RigidbodyConstraints.FreezePositionY |
                                     RigidbodyConstraints.FreezeRotationX |
                                     RigidbodyConstraints.FreezeRotationZ;

            if (_weaponController == null)
                _weaponController = GetComponent<Combat.WeaponController>();
        }

        private void Start()
        {
            _spawnPosition = transform.position;

            if (_config != null)
            {
                ApplyConfig(_config);
            }

            FindTarget();
            SetState(EnemyState.Patrol);
        }

        protected override void Update()
        {
            base.Update();
            UpdateBehavior();
        }

        public void ApplyConfig(EnemyConfig config)
        {
            _config = config;
            
            Initialize(config.maxHealth, config.maxShield);
            
            _moveSpeed = config.maxSpeed;
            _rotationSpeed = config.rotationSpeed;
            _detectionRange = config.detectionRange;
            _attackRange = config.attackRange;
            _patrolRadius = config.patrolRadius;

            if (_spriteRenderer != null && config.enemySprite != null)
            {
                _spriteRenderer.sprite = config.enemySprite;
            }

            if (_weaponController != null && config.weapon != null)
            {
                _weaponController.Initialize(config.weapon, transform);
                _weaponController.SetTargetLayers(LayerMask.GetMask("Player"));
            }
        }

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        public void UpdateBehavior()
        {
            if (!IsAlive) return;

            switch (_currentState)
            {
                case EnemyState.Idle:
                    UpdateIdleState();
                    break;
                case EnemyState.Patrol:
                    UpdatePatrolState();
                    break;
                case EnemyState.Chase:
                    UpdateChaseState();
                    break;
                case EnemyState.Attack:
                    UpdateAttackState();
                    break;
                case EnemyState.Flee:
                    UpdateFleeState();
                    break;
            }
        }

        private void SetState(EnemyState newState)
        {
            if (_currentState == newState) return;

            _currentState = newState;
            _stateTimer = 0f;

            switch (newState)
            {
                case EnemyState.Idle:
                    _stateTimer = _config?.idleTime ?? 2f;
                    break;
                case EnemyState.Patrol:
                    _patrolPoint = GetRandomPatrolPoint();
                    break;
            }
        }

        private void UpdateIdleState()
        {
            _stateTimer -= Time.deltaTime;

            if (IsTargetInRange(_detectionRange))
            {
                SetState(EnemyState.Chase);
                return;
            }

            if (_stateTimer <= 0)
            {
                SetState(EnemyState.Patrol);
            }
        }

        private void UpdatePatrolState()
        {
            if (IsTargetInRange(_detectionRange))
            {
                SetState(EnemyState.Chase);
                return;
            }

            MoveTowards(_patrolPoint);

            if (Vector2.Distance(transform.position, _patrolPoint) < 1f)
            {
                SetState(EnemyState.Idle);
            }
        }

        private void UpdateChaseState()
        {
            if (_target == null)
            {
                FindTarget();
                if (_target == null)
                {
                    SetState(EnemyState.Patrol);
                    return;
                }
            }

            float distance = Vector2.Distance(transform.position, _target.position);

            if (distance > _detectionRange * 1.5f)
            {
                SetState(EnemyState.Patrol);
                return;
            }

            if (distance <= _attackRange)
            {
                SetState(EnemyState.Attack);
                return;
            }

            if (ShouldFlee())
            {
                SetState(EnemyState.Flee);
                return;
            }

            MoveTowards(_target.position);
            RotateTowards(_target.position);
        }

        private void UpdateAttackState()
        {
            if (_target == null)
            {
                SetState(EnemyState.Patrol);
                return;
            }

            float distance = Vector2.Distance(transform.position, _target.position);

            if (distance > _attackRange * 1.3f)
            {
                SetState(EnemyState.Chase);
                return;
            }

            if (ShouldFlee())
            {
                SetState(EnemyState.Flee);
                return;
            }

            // Strafe around player to maintain optimal combat distance
            float optimalDistance = _attackRange * 0.75f;
            StrafeAround(_target.position, optimalDistance);

            TryFire();
        }

        private void UpdateFleeState()
        {
            if (_target == null)
            {
                SetState(EnemyState.Patrol);
                return;
            }

            MoveAwayFrom(_target.position);

            float distance = Vector2.Distance(transform.position, _target.position);
            if (distance > _detectionRange * 2f || !ShouldFlee())
            {
                SetState(EnemyState.Patrol);
            }
        }

        private void MoveTowards(Vector2 position)
        {
            Vector3 targetPos = new Vector3(position.x, 0, position.y);
            Vector3 direction = (targetPos - transform.position).normalized;

            // Direct velocity control - strictly enforce max speed
            Vector3 targetVelocity = direction * _moveSpeed;

            float lerpSpeed = 5f;
            _rigidbody.linearVelocity = Vector3.Lerp(
                _rigidbody.linearVelocity,
                targetVelocity,
                lerpSpeed * Time.deltaTime
            );

            RotateTowards(position);
        }

        private void MoveAwayFrom(Vector2 position)
        {
            Vector3 targetPos = new Vector3(position.x, 0, position.y);
            Vector3 direction = (transform.position - targetPos).normalized;

            // Direct velocity control - strictly enforce max speed
            Vector3 targetVelocity = direction * _moveSpeed;

            float lerpSpeed = 5f;
            _rigidbody.linearVelocity = Vector3.Lerp(
                _rigidbody.linearVelocity,
                targetVelocity,
                lerpSpeed * Time.deltaTime
            );
        }

        /// <summary>
        /// Strafe movement - move perpendicular to target while keeping distance
        /// Creates orbiting behavior around the player
        /// Uses direct velocity control to strictly enforce speed limits
        /// 3D Version on XZ plane
        /// </summary>
        private void StrafeAround(Vector2 targetPosition, float desiredDistance)
        {
            Vector3 targetPos = new Vector3(targetPosition.x, 0, targetPosition.y);
            Vector3 toTarget = targetPos - transform.position;
            float currentDistance = toTarget.magnitude;
            Vector3 toTargetDir = toTarget.normalized;

            // Always face the target first (important for shooting)
            RotateTowards(targetPosition);

            Vector3 moveDirection = Vector3.zero;

            // Perpendicular direction (for strafing/orbiting) on XZ plane
            // Use _strafeDirection to alternate left/right
            Vector3 strafeDir = new Vector3(-toTargetDir.z, 0, toTargetDir.x) * _strafeDirection;

            // Randomly flip direction occasionally for more variety
            if (Random.value < 0.005f)
            {
                _strafeDirection *= -1;
            }

            float speedModifier = 1f;

            if (currentDistance < desiredDistance * 0.8f)
            {
                // Too close - back away with slight strafe
                moveDirection = (-toTargetDir * 0.7f + strafeDir * 0.3f).normalized;
                speedModifier = 0.8f; // Move slower when backing away
            }
            else if (currentDistance > desiredDistance * 1.2f)
            {
                // Too far - move closer with slight strafe
                moveDirection = (toTargetDir * 0.7f + strafeDir * 0.3f).normalized;
                speedModifier = 0.6f; // Move slower when chasing (can't outrun player)
            }
            else
            {
                // At good distance - mostly strafe/orbit
                moveDirection = strafeDir;
                speedModifier = 0.5f; // Orbit speed
            }

            // Direct velocity control - strictly enforce max speed, no spikes
            float targetSpeed = _moveSpeed * speedModifier;
            Vector3 targetVelocity = moveDirection * targetSpeed;

            // Smoothly interpolate to target velocity (prevents instant snapping)
            float lerpSpeed = 5f;
            _rigidbody.linearVelocity = Vector3.Lerp(
                _rigidbody.linearVelocity,
                targetVelocity,
                lerpSpeed * Time.deltaTime
            );
        }

        private void RotateTowards(Vector2 position)
        {
            // For 3D XZ plane, we rotate around Y axis
            Vector3 direction = new Vector3(position.x, 0, position.y) - transform.position;
            direction.y = 0; // Keep flat on XZ plane
            direction.Normalize();

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                Quaternion currentRotation = transform.rotation;

                // Smoothly rotate towards target
                transform.rotation = Quaternion.Slerp(
                    currentRotation,
                    targetRotation,
                    _rotationSpeed * Time.deltaTime
                );
            }
        }

        private void TryFire()
        {
            if (_weaponController != null && _config != null)
            {
                // Check enemy-specific fire rate multiplier
                float weaponFireRate = _config.weapon?.fireRate ?? 0.2f;
                float modifiedFireRate = weaponFireRate * _config.fireRateMultiplier;

                if (Time.time >= _lastFireTime + modifiedFireRate)
                {
                    Vector3 aimDir = (_target.position - transform.position).normalized;
                    // Convert 3D direction to 2D for weapon controller (XZ -> XY)
                    Vector2 aimDir2D = new Vector2(aimDir.x, aimDir.z);
                    _weaponController.SetAimDirection(aimDir2D);

                    if (_weaponController.TryFire())
                    {
                        _lastFireTime = Time.time;
                    }
                }
            }
        }

        private Vector3 GetRandomPatrolPoint()
        {
            Vector2 randomOffset = Random.insideUnitCircle * _patrolRadius;
            return _spawnPosition + new Vector3(randomOffset.x, 0, randomOffset.y);
        }

        private bool IsTargetInRange(float range)
        {
            if (_target == null) return false;
            return Vector2.Distance(transform.position, _target.position) <= range;
        }

        private bool ShouldFlee()
        {
            if (_config == null || !_config.canFlee) return false;
            return (_currentHealth / _maxHealth) <= _config.fleeHealthThreshold;
        }

        private void FindTarget()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _target = player.transform;
            }
        }

        protected override int GetScoreValue()
        {
            return _config?.scoreValue ?? 100;
        }

        protected override void OnDeathEffect()
        {
            base.OnDeathEffect();
            
            if (_config != null)
            {
                EventBus.Publish(new PlaySFXEvent(_config.deathSoundId, transform.position));
            }

            EventBus.Publish(new ScoreChangedEvent(GetScoreValue(), GetScoreValue()));
        }

        protected override void Die()
        {
            base.Die();
            
            if (gameObject.activeInHierarchy)
            {
                gameObject.SetActive(false);
            }
        }

        // IPoolable
        public void OnSpawn()
        {
            _currentHealth = _maxHealth;
            _currentShield = _maxShield;
            SetState(EnemyState.Patrol);
            FindTarget();
            _strafeDirection = Random.value > 0.5f ? 1 : -1; // Random orbit direction
        }

        public void OnDespawn()
        {
            _rigidbody.linearVelocity = Vector3.zero;
        }

        public void ResetState()
        {
            _currentHealth = _maxHealth;
            _currentShield = _maxShield;
            _currentState = EnemyState.Idle;
            _target = null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _detectionRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _attackRange);

            Gizmos.color = Color.blue;
            Vector3 spawnPos = Application.isPlaying ? _spawnPosition : transform.position;
            Gizmos.DrawWireSphere(spawnPos, _patrolRadius);
        }
#endif
    }
}
