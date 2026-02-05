// ============================================
// ENEMY SPAWN SERVICE - Service Layer Pattern
// Manages all enemy spawning operations
// Follows Single Responsibility Principle
// ============================================

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using StarReapers.Interfaces;
using StarReapers.Utilities;
using StarReapers.Core;
using Debug = UnityEngine.Debug;

namespace StarReapers.Spawning
{
    /// <summary>
    /// Service responsible for managing enemy spawning.
    /// 
    /// Design Patterns:
    /// - Service Layer: Encapsulates spawn business logic
    /// - Strategy: Uses ISpawnStrategy for distribution
    /// - Object Pool: Integrates with PoolManager for efficiency
    /// 
    /// SOLID Principles:
    /// - Single Responsibility: Only handles spawning
    /// - Open/Closed: New strategies via ISpawnStrategy
    /// - Dependency Inversion: Depends on abstractions
    /// </summary>
    public class EnemySpawnService : MonoBehaviour, ISpawnService
    {
        [Header("Configuration")]
        [SerializeField] private SpawnConfig _config;
        [SerializeField] private Entities.Enemy _enemyPrefab;

        [Header("References")]
        [SerializeField] private Environment.MapBounds _mapBounds;

        [Header("Debug")]
        [SerializeField] private bool _debugDrawSpawnArea = true;
        [SerializeField] private bool _debugLogSpawns = false;

        // Injected dependencies
        private PoolManager _poolManager;
        private IObjectResolver _container;

        [Inject]
        public void Construct(PoolManager poolManager, IObjectResolver container)
        {
            _poolManager = poolManager;
            _container = container;
        }

        // Runtime state
        private ISpawnStrategy _currentStrategy;
        private ObjectPool<Entities.Enemy> _enemyPool;
        private Bounds _spawnBounds;
        private HashSet<Entities.Enemy> _activeEnemies = new HashSet<Entities.Enemy>();

        // Events
        public event Action<GameObject, Vector3> OnEnemySpawned;
        public event Action<GameObject> OnEnemyReturned;

        // Properties
        public int ActiveEnemyCount => _activeEnemies.Count;

        private void Awake()
        {
        }

        private void Start()
        {
            // Initialization is handled by GameManager.InitializeSpawnService()
        }

        private void OnDestroy()
        {
        }

        // ============================================
        // INITIALIZATION
        // ============================================

        public void Initialize(SpawnConfig config)
        {
            _config = config;

            // Setup bounds
            InitializeBounds();

            // Create strategy from config
            _currentStrategy = SpawnStrategyFactory.CreateFromConfig(config);

            // Setup object pool
            SetupObjectPool();

            if (_debugLogSpawns)
            {
                Debug.Log($"[EnemySpawnService] Initialized with strategy: {_currentStrategy.StrategyName}");
                Debug.Log($"[EnemySpawnService] Bounds: Center={_spawnBounds.center}, Size={_spawnBounds.size}");
            }
        }

        private void InitializeBounds()
        {
            if (_config != null && _config.UseCustomBounds)
            {
                _spawnBounds = new Bounds(_config.CustomBoundsCenter, _config.CustomBoundsSize);
            }
            else if (_mapBounds != null)
            {
                // Use the new Bounds3D property directly - already on XZ plane
                _spawnBounds = _mapBounds.Bounds3D;

                if (_debugLogSpawns)
                {
                    Debug.Log($"[EnemySpawnService] Using MapBounds: {_mapBounds.MapWidth}x{_mapBounds.MapHeight}");
                }
            }
            else
            {
                // Fallback: Large default bounds
                Debug.LogWarning("[EnemySpawnService] No MapBounds assigned, using default large area");
                _spawnBounds = new Bounds(Vector3.zero, new Vector3(200f, 0f, 200f));
            }
        }

        private void SetupObjectPool()
        {
            if (_poolManager == null)
            {
                Debug.LogError("[EnemySpawnService] PoolManager not injected!");
                return;
            }

            if (_enemyPrefab != null)
            {
                int maxEnemies = _config?.MaxEnemies ?? 50;
                _enemyPool = _poolManager.CreatePool("Enemies", _enemyPrefab, 10, maxEnemies);
            }
            else
            {
                Debug.LogError("[EnemySpawnService] Enemy prefab not assigned!");
            }
        }

        // ============================================
        // SPAWN OPERATIONS
        // ============================================

        public void SpawnInitialEnemies(int count, Vector3 playerPosition)
        {
            if (_currentStrategy == null || _enemyPool == null)
            {
                Debug.LogError("[EnemySpawnService] Not initialized properly!");
                return;
            }

            float minDistance = _config?.MinDistanceFromPlayer ?? 20f;
            float minSpacing = _config?.MinSpacingBetweenEnemies ?? 5f;

            // Get all positions at once for proper distribution
            Vector3[] positions = _currentStrategy.GetSpawnPositions(
                _spawnBounds,
                count,
                playerPosition,
                minDistance,
                minSpacing
            );

            if (_debugLogSpawns)
            {
                Debug.Log($"[EnemySpawnService] Spawning {count} enemies using {_currentStrategy.StrategyName}");
            }

            foreach (var position in positions)
            {
                SpawnEnemyAt(position);
            }
        }

        public GameObject SpawnEnemy(Vector3 playerPosition)
        {
            if (_currentStrategy == null || _enemyPool == null) return null;

            float minDistance = _config?.MinDistanceFromPlayer ?? 20f;
            Vector3 position = _currentStrategy.GetSpawnPosition(_spawnBounds, playerPosition, minDistance);

            return SpawnEnemyAt(position);
        }

        public GameObject SpawnEnemyAt(Vector3 position)
        {
            if (_enemyPool == null) return null;

            // Ensure Y is correct for XZ plane
            position.y = 0f;

            var enemy = _enemyPool.Get(position, Quaternion.identity);
            if (enemy != null)
            {
                // Inject dependencies into pool-spawned enemy and its components
                _container?.InjectGameObject(enemy.gameObject);

                enemy.gameObject.layer = LayerMask.NameToLayer("Enemy");
                _activeEnemies.Add(enemy);

                // Lazy-resolve GameManager to avoid circular dependency
                var gameManager = _container?.Resolve<GameManager>();
                var player = gameManager?.Player;
                if (player != null)
                {
                    enemy.SetTarget(player.transform);
                }

                if (_debugLogSpawns)
                {
                    Debug.Log($"[EnemySpawnService] Spawned enemy at {position}");
                }

                OnEnemySpawned?.Invoke(enemy.gameObject, position);

                // Publish event for other systems
                Events.EventBus.Publish(new Events.EnemySpawnedEvent(
                    enemy.gameObject,
                    new Vector2(position.x, position.z),
                    "Enemy"
                ));
            }

            return enemy?.gameObject;
        }

        public void ReturnEnemy(GameObject enemy)
        {
            if (enemy == null || _enemyPool == null) return;

            var enemyComponent = enemy.GetComponent<Entities.Enemy>();
            if (enemyComponent != null)
            {
                _activeEnemies.Remove(enemyComponent);
                _enemyPool.Return(enemyComponent);
                OnEnemyReturned?.Invoke(enemy);

                if (_debugLogSpawns)
                {
                    Debug.Log($"[EnemySpawnService] Returned enemy to pool. Active: {_activeEnemies.Count}");
                }
            }
        }

        public void ReturnAllEnemies()
        {
            foreach (var enemy in _activeEnemies.ToArray())
            {
                if (enemy != null)
                {
                    _enemyPool?.Return(enemy);
                }
            }
            _activeEnemies.Clear();

            if (_debugLogSpawns)
            {
                Debug.Log("[EnemySpawnService] All enemies returned to pool");
            }
        }

        // ============================================
        // STRATEGY MANAGEMENT
        // ============================================

        public void SetStrategy(ISpawnStrategy strategy)
        {
            if (strategy == null)
            {
                Debug.LogWarning("[EnemySpawnService] Attempted to set null strategy");
                return;
            }

            _currentStrategy = strategy;

            if (_debugLogSpawns)
            {
                Debug.Log($"[EnemySpawnService] Strategy changed to: {strategy.StrategyName}");
            }
        }

        public void SetStrategy(SpawnDistributionType type)
        {
            var strategy = _config != null
                ? SpawnStrategyFactory.Create(type, _config.StrategyConfig)
                : SpawnStrategyFactory.Create(type);

            SetStrategy(strategy);
        }

        // ============================================
        // BOUNDS MANAGEMENT
        // ============================================

        public void SetBounds(Bounds bounds)
        {
            _spawnBounds = bounds;

            if (_debugLogSpawns)
            {
                Debug.Log($"[EnemySpawnService] Bounds updated: Center={bounds.center}, Size={bounds.size}");
            }
        }

        public Bounds GetBounds()
        {
            return _spawnBounds;
        }

        // ============================================
        // DEBUG VISUALIZATION
        // ============================================

        private void OnDrawGizmos()
        {
            if (!_debugDrawSpawnArea) return;

            // Draw spawn bounds
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);

            // Use runtime bounds if available, otherwise show config/default
            Bounds boundsToShow = _spawnBounds.size.magnitude > 0.01f
                ? _spawnBounds
                : new Bounds(Vector3.zero, new Vector3(200f, 1f, 200f));

            // Draw as wireframe on XZ plane
            Vector3 center = boundsToShow.center;
            Vector3 size = boundsToShow.size;
            size.y = 1f; // Small Y for visibility

            Gizmos.DrawWireCube(center, size);

            // Draw min distance from center (example player position)
            if (_config != null)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                DrawCircleXZ(center, _config.MinDistanceFromPlayer, 32);
            }
        }

        private void DrawCircleXZ(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(radius, 0f, 0f);

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }
        }
    }
}