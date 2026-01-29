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
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
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
        private Rigidbody2D _rigidbody;

        // State
        private EnemyState _currentState = EnemyState.Idle;
        private Transform _target;
        private Vector2 _patrolPoint;
        private Vector2 _spawnPosition;
        private float _stateTimer;
        private float _lastFireTime;
        private int _strafeDirection = 1; // 1 = clockwise, -1 = counter-clockwise

        // Properties
        public EnemyState CurrentState => _currentState;
        public Transform Target => _target;
        public bool IsActive => gameObject.activeInHierarchy;

        protected override void Awake()
        {
            base.Awake();

            _rigidbody = GetComponent<Rigidbody2D>();
            _rigidbody.gravityScale = 0f;
            _rigidbody.linearDamping = 5f; // Higher = less drift, more responsive
            _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate; // Smooth movement

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
            Vector2 direction = ((Vector2)position - (Vector2)transform.position).normalized;

            // Direct velocity control - strictly enforce max speed
            Vector2 targetVelocity = direction * _moveSpeed;

            float lerpSpeed = 5f;
            _rigidbody.linearVelocity = Vector2.Lerp(
                _rigidbody.linearVelocity,
                targetVelocity,
                lerpSpeed * Time.deltaTime
            );

            RotateTowards(position);
        }

        private void MoveAwayFrom(Vector2 position)
        {
            Vector2 direction = ((Vector2)transform.position - (Vector2)position).normalized;

            // Direct velocity control - strictly enforce max speed
            Vector2 targetVelocity = direction * _moveSpeed;

            float lerpSpeed = 5f;
            _rigidbody.linearVelocity = Vector2.Lerp(
                _rigidbody.linearVelocity,
                targetVelocity,
                lerpSpeed * Time.deltaTime
            );
        }

        /// <summary>
        /// Strafe movement - move perpendicular to target while keeping distance
        /// Creates orbiting behavior around the player
        /// Uses direct velocity control to strictly enforce speed limits
        /// </summary>
        private void StrafeAround(Vector2 targetPosition, float desiredDistance)
        {
            Vector2 toTarget = (Vector2)targetPosition - (Vector2)transform.position;
            float currentDistance = toTarget.magnitude;
            Vector2 toTargetDir = toTarget.normalized;

            // Always face the target first (important for shooting)
            RotateTowards(targetPosition);

            Vector2 moveDirection = Vector2.zero;

            // Perpendicular direction (for strafing/orbiting)
            // Use _strafeDirection to alternate left/right
            Vector2 strafeDir = new Vector2(-toTargetDir.y, toTargetDir.x) * _strafeDirection;

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
            Vector2 targetVelocity = moveDirection * targetSpeed;

            // Smoothly interpolate to target velocity (prevents instant snapping)
            float lerpSpeed = 5f;
            _rigidbody.linearVelocity = Vector2.Lerp(
                _rigidbody.linearVelocity,
                targetVelocity,
                lerpSpeed * Time.deltaTime
            );
        }

        private void RotateTowards(Vector2 position)
        {
            Vector2 direction = ((Vector2)position - (Vector2)transform.position).normalized;
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            float currentAngle = transform.eulerAngles.z;
            
            float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, _rotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0, 0, newAngle);
        }

        private void TryFire()
        {
            if (_weaponController != null)
            {
                Vector2 aimDir = (_target.position - transform.position).normalized;
                _weaponController.SetAimDirection(aimDir);
                _weaponController.TryFire();
            }
        }

        private Vector2 GetRandomPatrolPoint()
        {
            Vector2 randomOffset = Random.insideUnitCircle * _patrolRadius;
            return _spawnPosition + randomOffset;
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
            _rigidbody.linearVelocity = Vector2.zero;
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
            Vector2 spawnPos = Application.isPlaying ? _spawnPosition : (Vector2)transform.position;
            Gizmos.DrawWireSphere(spawnPos, _patrolRadius);
        }
#endif
    }
}
