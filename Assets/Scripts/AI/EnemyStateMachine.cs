using UnityEngine;
using SpaceCombat.AI.States;
using SpaceCombat.Combat;
using SpaceCombat.ScriptableObjects;

namespace SpaceCombat.AI
{
    /// <summary>
    /// State machine controller component for enemy AI.
    /// Manages state transitions and updates the current state each frame.
    /// Attach to enemy GameObjects alongside Enemy component.
    /// </summary>
    public class EnemyStateMachine : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private string _currentStateName;

        private IEnemyState _currentState;
        private EnemyContext _context;
        private bool _initialized;

        public EnemyContext Context => _context;
        public string CurrentStateName => _currentState?.StateName ?? "None";
        public bool IsInitialized => _initialized;

        /// <summary>
        /// Initialize the state machine with enemy data.
        /// Called by Enemy after component setup.
        /// </summary>
        public void Initialize(
            Rigidbody rigidbody,
            WeaponController weaponController,
            EnemyConfig config,
            Transform target)
        {
            _context = new EnemyContext
            {
                Transform = transform,
                Rigidbody = rigidbody,
                WeaponController = weaponController,
                Config = config,
                Target = target,
                SpawnPosition = transform.position,
                IsAlive = true,
                HealthPercent = 1f,
                StrafeDirection = Random.value > 0.5f ? 1 : -1
            };

            TransitionTo(new PatrolState());
            _initialized = true;
        }

        /// <summary>
        /// Update state machine each frame.
        /// Called by Enemy.Update().
        /// </summary>
        public void Tick()
        {
            if (!_initialized || _currentState == null || !_context.IsAlive) return;

            IEnemyState nextState = _currentState.Execute(_context);
            if (nextState != null)
            {
                TransitionTo(nextState);
            }

            // Update debug display
            _currentStateName = _currentState.StateName;
        }

        /// <summary>
        /// Transition to a new state.
        /// </summary>
        public void TransitionTo(IEnemyState newState)
        {
            _currentState?.Exit(_context);
            _currentState = newState;
            _currentState.Enter(_context);
            _currentStateName = _currentState.StateName;
        }

        /// <summary>
        /// Update context with current enemy health data.
        /// </summary>
        public void UpdateHealth(float healthPercent, bool isAlive)
        {
            if (_context == null) return;
            _context.HealthPercent = healthPercent;
            _context.IsAlive = isAlive;
        }

        /// <summary>
        /// Set target (e.g., player).
        /// </summary>
        public void SetTarget(Transform target)
        {
            if (_context != null)
                _context.Target = target;
        }

        /// <summary>
        /// Reset state machine for pool reuse.
        /// </summary>
        public void ResetToPatrol()
        {
            if (_context == null) return;

            _context.SpawnPosition = transform.position;
            _context.IsAlive = true;
            _context.HealthPercent = 1f;
            _context.StrafeDirection = Random.value > 0.5f ? 1 : -1;

            TransitionTo(new PatrolState());
        }
    }
}
