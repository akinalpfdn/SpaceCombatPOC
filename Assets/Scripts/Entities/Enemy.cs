// ============================================
// ENEMY - Entity Data + Pooling + Config
// AI behavior delegated to EnemyStateMachine
// Movement delegated to AI.EnemyMovement
// ============================================

using UnityEngine;
using SpaceCombat.Interfaces;
using SpaceCombat.Events;
using SpaceCombat.ScriptableObjects;
using SpaceCombat.AI;

namespace SpaceCombat.Entities
{
    /// <summary>
    /// Enemy entity - handles health, config, pooling and death.
    /// AI behavior is managed by EnemyStateMachine component.
    /// 3D Version - Movement locked to XZ plane.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(EnemyStateMachine))]
    public class Enemy : BaseEntity, IEnemy, IPoolable
    {
        [Header("Configuration")]
        [SerializeField] private EnemyConfig _config;

        [Header("Combat")]
        [SerializeField] private Combat.WeaponController _weaponController;

        // Components
        private Rigidbody _rigidbody;
        private EnemyStateMachine _stateMachine;

        // IEnemy - read from state machine
        public EnemyState CurrentState
        {
            get
            {
                if (_stateMachine == null) return EnemyState.Idle;
                return _stateMachine.CurrentStateName switch
                {
                    "Idle" => EnemyState.Idle,
                    "Patrol" => EnemyState.Patrol,
                    "Chase" => EnemyState.Chase,
                    "Attack" => EnemyState.Attack,
                    "Flee" => EnemyState.Flee,
                    _ => EnemyState.Idle
                };
            }
        }

        public Transform Target => _stateMachine?.Context?.Target;
        public bool IsActive => gameObject.activeInHierarchy;
        public float TargetIndicatorScale => _config?.targetIndicatorScale ?? 1f;

        protected override void Awake()
        {
            base.Awake();

            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.useGravity = false;
            _rigidbody.linearDamping = 5f;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.constraints = RigidbodyConstraints.FreezePositionY |
                                     RigidbodyConstraints.FreezeRotationX |
                                     RigidbodyConstraints.FreezeRotationZ;

            if (_weaponController == null)
                _weaponController = GetComponent<Combat.WeaponController>();

            _stateMachine = GetComponent<EnemyStateMachine>();
        }

        private void Start()
        {
            if (_config != null)
            {
                ApplyConfig(_config);
            }

            InitializeStateMachine();
        }

        protected override void Update()
        {
            base.Update();

            // Update state machine with current health info
            _stateMachine.UpdateHealth(_currentHealth / _maxHealth, IsAlive);
            _stateMachine.Tick();
        }

        // ============================================
        // CONFIGURATION
        // ============================================

        public void ApplyConfig(EnemyConfig config)
        {
            _config = config;

            Initialize(config.maxHealth, config.maxShield);

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

        private void InitializeStateMachine()
        {
            Transform target = FindPlayerTarget();
            _stateMachine.Initialize(_rigidbody, _weaponController, _config, target);
        }

        // ============================================
        // TARGET
        // ============================================

        public void SetTarget(Transform target)
        {
            _stateMachine?.SetTarget(target);
        }

        public void UpdateBehavior()
        {
            // Kept for IEnemy interface compatibility - logic now in StateMachine.Tick()
        }

        private Transform FindPlayerTarget()
        {
            var gameManager = Core.GameManager.Instance;
            if (gameManager != null && gameManager.Player != null)
            {
                return gameManager.Player.transform;
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            return player != null ? player.transform : null;
        }

        // ============================================
        // DEATH & SCORING
        // ============================================

        protected override int GetScoreValue()
        {
            return _config?.scoreValue ?? 100;
        }

        protected override void OnDeathEffect()
        {
            base.OnDeathEffect();

            if (_config != null)
            {
                Vector3 pos = transform.position;
                EventBus.Publish(new PlaySFXEvent(_config.deathSoundId, new Vector2(pos.x, pos.z)));
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

        // ============================================
        // IPoolable
        // ============================================

        public void OnSpawn()
        {
            _currentHealth = _maxHealth;
            _currentShield = _maxShield;

            NotifyHealthChanged();
            NotifyShieldChanged();

            // Ensure state machine is initialized (Start may not have run yet for pooled objects)
            if (!_stateMachine.IsInitialized)
            {
                if (_config != null) ApplyConfig(_config);
                InitializeStateMachine();
            }

            // Reset state machine for new life
            _stateMachine.ResetToPatrol();

            // Try to set target
            Transform target = FindPlayerTarget();
            if (target != null)
            {
                _stateMachine.SetTarget(target);
            }
        }

        public void OnDespawn()
        {
            _rigidbody.linearVelocity = Vector3.zero;
        }

        public void ResetState()
        {
            _currentHealth = _maxHealth;
            _currentShield = _maxShield;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            float detectionRange = _config?.detectionRange ?? 12f;
            float attackRange = _config?.attackRange ?? 8f;
            float patrolRadius = _config?.patrolRadius ?? 5f;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, patrolRadius);
        }
#endif
    }
}
