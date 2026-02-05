// ============================================
// ENEMY SPAWN SERVICE - Service Layer Pattern
// Manages all enemy spawning operations with multi-enemy type support
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
using StarReapers.Maps;
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
    ///
    /// Multi-Enemy Support:
    /// - Uses MapConfig for weighted enemy selection
    /// - Creates separate pools for each enemy type
    /// - Supports boss spawning with corner positioning
    /// </summary>
    public class EnemySpawnService : MonoBehaviour, ISpawnService
    {
        [Header("Map Configuration")]
        [Tooltip("Map config with enemy composition (preferred)")]
        [SerializeField] private MapConfig _mapConfig;

        [Header("Legacy Configuration")]
        [Tooltip("Spawn parameters (used if MapConfig has no spawnConfig)")]
        [SerializeField] private SpawnConfig _config;
        [Tooltip("Single enemy prefab (fallback if no MapConfig)")]
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
        private Bounds _spawnBounds;
        private HashSet<Entities.Enemy> _activeEnemies = new HashSet<Entities.Enemy>();

        // Multi-enemy pool management
        private Dictionary<Entities.Enemy, ObjectPool<Entities.Enemy>> _enemyPools = new();
        private Dictionary<Entities.Enemy, int> _activeCountPerType = new();
        private bool _useMultiEnemyMode = false;

        // Legacy single pool (for backwards compatibility)
        private ObjectPool<Entities.Enemy> _enemyPool;

        // Boss state
        private Entities.Enemy _activeBoss;
        private ObjectPool<Entities.Enemy> _bossPool;
        private bool _bossSpawned = false;

        // Events
        public event Action<GameObject, Vector3> OnEnemySpawned;
        public event Action<GameObject> OnEnemyReturned;
        public event Action<GameObject, Vector3> OnBossSpawned;

        // Properties
        public int ActiveEnemyCount => _activeEnemies.Count;
        public bool HasActiveBoss => _activeBoss != null;
        public MapConfig CurrentMapConfig => _mapConfig;

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

        /// <summary>
        /// Initialize with MapConfig (preferred method for multi-enemy support).
        /// </summary>
        public void Initialize(MapConfig mapConfig)
        {
            _mapConfig = mapConfig;
            _config = mapConfig.spawnConfig;
            _useMultiEnemyMode = true;

            InitializeBounds();
            _currentStrategy = SpawnStrategyFactory.CreateFromConfig(_config);
            SetupMultiEnemyPools();
            SetupBossPool();

            if (_debugLogSpawns)
            {
                Debug.Log($"[EnemySpawnService] Initialized with MapConfig: {mapConfig.mapName}");
                Debug.Log($"[EnemySpawnService] Enemy types: {mapConfig.EnemyTypeCount}, Strategy: {_currentStrategy.StrategyName}");
            }
        }

        /// <summary>
        /// Initialize with SpawnConfig only (legacy/backwards compatible).
        /// </summary>
        public void Initialize(SpawnConfig config)
        {
            // Check if we have a MapConfig assigned in Inspector
            if (_mapConfig != null && _mapConfig.IsValid)
            {
                Initialize(_mapConfig);
                return;
            }

            _config = config;
            _useMultiEnemyMode = false;

            InitializeBounds();
            _currentStrategy = SpawnStrategyFactory.CreateFromConfig(config);
            SetupLegacyPool();

            if (_debugLogSpawns)
            {
                Debug.Log($"[EnemySpawnService] Initialized with SpawnConfig (legacy mode)");
                Debug.Log($"[EnemySpawnService] Strategy: {_currentStrategy.StrategyName}");
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
                _spawnBounds = _mapBounds.Bounds3D;

                if (_debugLogSpawns)
                {
                    Debug.Log($"[EnemySpawnService] Using MapBounds: {_mapBounds.MapWidth}x{_mapBounds.MapHeight}");
                }
            }
            else
            {
                Debug.LogWarning("[EnemySpawnService] No MapBounds assigned, using default large area");
                _spawnBounds = new Bounds(Vector3.zero, new Vector3(200f, 0f, 200f));
            }
        }

        private void SetupMultiEnemyPools()
        {
            if (_poolManager == null)
            {
                Debug.LogError("[EnemySpawnService] PoolManager not injected!");
                return;
            }

            if (_mapConfig?.enemies == null || _mapConfig.enemies.Length == 0)
            {
                Debug.LogWarning("[EnemySpawnService] No enemies configured in MapConfig, falling back to legacy");
                SetupLegacyPool();
                return;
            }

            _enemyPools.Clear();
            _activeCountPerType.Clear();

            int maxPerType = (_config?.MaxEnemies ?? 50) / _mapConfig.enemies.Length;
            maxPerType = Mathf.Max(maxPerType, 10);

            foreach (var entry in _mapConfig.enemies)
            {
                if (!entry.IsValid) continue;

                string poolId = $"Enemy_{entry.enemyPrefab.name}";

                if (!_poolManager.HasPool(poolId))
                {
                    var pool = _poolManager.CreatePool(poolId, entry.enemyPrefab, 5, maxPerType);
                    _enemyPools[entry.enemyPrefab] = pool;
                }
                else
                {
                    _enemyPools[entry.enemyPrefab] = _poolManager.GetPool<Entities.Enemy>(poolId);
                }

                _activeCountPerType[entry.enemyPrefab] = 0;

                if (_debugLogSpawns)
                {
                    Debug.Log($"[EnemySpawnService] Created pool for {entry.enemyPrefab.name} (weight: {entry.spawnWeight})");
                }
            }
        }

        private void SetupBossPool()
        {
            if (_mapConfig?.bossSettings == null || !_mapConfig.bossSettings.enabled)
                return;

            if (_mapConfig.bossSettings.bossPrefab == null)
            {
                Debug.LogWarning("[EnemySpawnService] Boss enabled but no prefab assigned!");
                return;
            }

            string bossPoolId = $"Boss_{_mapConfig.bossSettings.bossPrefab.name}";

            if (!_poolManager.HasPool(bossPoolId))
            {
                _bossPool = _poolManager.CreatePool(bossPoolId, _mapConfig.bossSettings.bossPrefab, 1, 2);
            }
            else
            {
                _bossPool = _poolManager.GetPool<Entities.Enemy>(bossPoolId);
            }

            if (_debugLogSpawns)
            {
                Debug.Log($"[EnemySpawnService] Boss pool created for {_mapConfig.bossSettings.bossPrefab.name}");
            }
        }

        private void SetupLegacyPool()
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
            if (_currentStrategy == null)
            {
                Debug.LogError("[EnemySpawnService] Not initialized properly!");
                return;
            }

            if (!_useMultiEnemyMode && _enemyPool == null)
            {
                Debug.LogError("[EnemySpawnService] No enemy pool available!");
                return;
            }

            float minDistance = _config?.MinDistanceFromPlayer ?? 20f;
            float minSpacing = _config?.MinSpacingBetweenEnemies ?? 5f;

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
            if (_currentStrategy == null) return null;

            float minDistance = _config?.MinDistanceFromPlayer ?? 20f;
            Vector3 position = _currentStrategy.GetSpawnPosition(_spawnBounds, playerPosition, minDistance);

            return SpawnEnemyAt(position);
        }

        public GameObject SpawnEnemyAt(Vector3 position)
        {
            position.y = 0f;

            Entities.Enemy enemy;

            if (_useMultiEnemyMode)
            {
                enemy = SpawnRandomEnemyFromConfig(position);
            }
            else
            {
                enemy = SpawnFromLegacyPool(position);
            }

            if (enemy != null)
            {
                FinalizeSpawnedEnemy(enemy, position);
            }

            return enemy?.gameObject;
        }

        private Entities.Enemy SpawnRandomEnemyFromConfig(Vector3 position)
        {
            if (_mapConfig == null) return null;

            // Get valid entries that haven't hit their max concurrent limit
            var availableEntries = _mapConfig.enemies
                .Where(e => e.IsValid && CanSpawnEnemyType(e))
                .ToArray();

            if (availableEntries.Length == 0)
            {
                if (_debugLogSpawns)
                {
                    Debug.Log("[EnemySpawnService] All enemy types at max concurrent limit");
                }
                return null;
            }

            // Weighted random selection
            int totalWeight = availableEntries.Sum(e => e.spawnWeight);
            int randomValue = UnityEngine.Random.Range(0, totalWeight);
            int currentWeight = 0;

            EnemySpawnEntry selectedEntry = availableEntries[0];
            foreach (var entry in availableEntries)
            {
                currentWeight += entry.spawnWeight;
                if (randomValue < currentWeight)
                {
                    selectedEntry = entry;
                    break;
                }
            }

            // Get from pool
            if (_enemyPools.TryGetValue(selectedEntry.enemyPrefab, out var pool))
            {
                var enemy = pool.Get(position, Quaternion.identity);
                if (enemy != null)
                {
                    _activeCountPerType[selectedEntry.enemyPrefab]++;

                    if (_debugLogSpawns)
                    {
                        Debug.Log($"[EnemySpawnService] Spawned {selectedEntry.enemyPrefab.name} at {position}");
                    }
                }
                return enemy;
            }

            return null;
        }

        private bool CanSpawnEnemyType(EnemySpawnEntry entry)
        {
            if (entry.maxConcurrent <= 0) return true; // 0 = unlimited

            if (_activeCountPerType.TryGetValue(entry.enemyPrefab, out int count))
            {
                return count < entry.maxConcurrent;
            }

            return true;
        }

        private Entities.Enemy SpawnFromLegacyPool(Vector3 position)
        {
            if (_enemyPool == null) return null;
            return _enemyPool.Get(position, Quaternion.identity);
        }

        private void FinalizeSpawnedEnemy(Entities.Enemy enemy, Vector3 position)
        {
            _container?.InjectGameObject(enemy.gameObject);

            enemy.gameObject.layer = LayerMask.NameToLayer("Enemy");
            _activeEnemies.Add(enemy);

            var gameManager = _container?.Resolve<GameManager>();
            var player = gameManager?.Player;
            if (player != null)
            {
                enemy.SetTarget(player.transform);
            }

            OnEnemySpawned?.Invoke(enemy.gameObject, position);

            Events.EventBus.Publish(new Events.EnemySpawnedEvent(
                enemy.gameObject,
                new Vector2(position.x, position.z),
                enemy.name
            ));
        }

        // ============================================
        // BOSS SPAWNING
        // ============================================

        /// <summary>
        /// Spawns the boss enemy at the configured corner of the map.
        /// </summary>
        public GameObject SpawnBoss()
        {
            if (_mapConfig?.bossSettings == null || !_mapConfig.bossSettings.enabled)
            {
                Debug.LogWarning("[EnemySpawnService] Boss not configured or disabled");
                return null;
            }

            if (_bossSpawned || _activeBoss != null)
            {
                Debug.LogWarning("[EnemySpawnService] Boss already spawned");
                return null;
            }

            if (_bossPool == null)
            {
                Debug.LogError("[EnemySpawnService] Boss pool not initialized");
                return null;
            }

            Vector3 position = _mapConfig.GetBossSpawnPosition(_spawnBounds);
            var boss = _bossPool.Get(position, Quaternion.identity);

            if (boss != null)
            {
                _container?.InjectGameObject(boss.gameObject);

                boss.gameObject.layer = LayerMask.NameToLayer("Enemy");
                _activeEnemies.Add(boss);
                _activeBoss = boss;
                _bossSpawned = true;

                var gameManager = _container?.Resolve<GameManager>();
                var player = gameManager?.Player;
                if (player != null)
                {
                    boss.SetTarget(player.transform);
                }

                if (_debugLogSpawns)
                {
                    Debug.Log($"[EnemySpawnService] Boss spawned at {position} (corner: {_mapConfig.bossSettings.spawnCorner})");
                }

                OnBossSpawned?.Invoke(boss.gameObject, position);

                Events.EventBus.Publish(new Events.EnemySpawnedEvent(
                    boss.gameObject,
                    new Vector2(position.x, position.z),
                    "Boss"
                ));
            }

            return boss?.gameObject;
        }

        // ============================================
        // RETURN OPERATIONS
        // ============================================

        public void ReturnEnemy(GameObject enemy)
        {
            if (enemy == null) return;

            var enemyComponent = enemy.GetComponent<Entities.Enemy>();
            if (enemyComponent == null) return;

            _activeEnemies.Remove(enemyComponent);

            // Check if this was the boss
            if (enemyComponent == _activeBoss)
            {
                _activeBoss = null;
                _bossPool?.Return(enemyComponent);
                OnEnemyReturned?.Invoke(enemy);

                if (_debugLogSpawns)
                {
                    Debug.Log("[EnemySpawnService] Boss returned to pool");
                }
                return;
            }

            // Return to appropriate pool
            if (_useMultiEnemyMode)
            {
                ReturnToMultiPool(enemyComponent);
            }
            else
            {
                _enemyPool?.Return(enemyComponent);
            }

            OnEnemyReturned?.Invoke(enemy);

            if (_debugLogSpawns)
            {
                Debug.Log($"[EnemySpawnService] Returned enemy to pool. Active: {_activeEnemies.Count}");
            }
        }

        private void ReturnToMultiPool(Entities.Enemy enemy)
        {
            foreach (var kvp in _enemyPools)
            {
                // Match by prefab name (pool tracks original prefab)
                if (enemy.name.StartsWith(kvp.Key.name))
                {
                    kvp.Value.Return(enemy);

                    if (_activeCountPerType.ContainsKey(kvp.Key))
                    {
                        _activeCountPerType[kvp.Key] = Mathf.Max(0, _activeCountPerType[kvp.Key] - 1);
                    }
                    return;
                }
            }

            // Fallback: destroy if no matching pool found
            Debug.LogWarning($"[EnemySpawnService] No pool found for enemy {enemy.name}, destroying");
            Destroy(enemy.gameObject);
        }

        public void ReturnAllEnemies()
        {
            foreach (var enemy in _activeEnemies.ToArray())
            {
                if (enemy != null)
                {
                    ReturnEnemy(enemy.gameObject);
                }
            }

            _activeEnemies.Clear();
            _activeBoss = null;
            _bossSpawned = false;

            foreach (var key in _activeCountPerType.Keys.ToArray())
            {
                _activeCountPerType[key] = 0;
            }

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

            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);

            Bounds boundsToShow = _spawnBounds.size.magnitude > 0.01f
                ? _spawnBounds
                : new Bounds(Vector3.zero, new Vector3(200f, 1f, 200f));

            Vector3 center = boundsToShow.center;
            Vector3 size = boundsToShow.size;
            size.y = 1f;

            Gizmos.DrawWireCube(center, size);

            if (_config != null)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                DrawCircleXZ(center, _config.MinDistanceFromPlayer, 32);
            }

            // Draw boss spawn corners if configured
            if (_mapConfig?.bossSettings != null && _mapConfig.bossSettings.enabled)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
                Vector3 bossPos = _mapConfig.GetBossSpawnPosition(boundsToShow);
                Gizmos.DrawWireSphere(bossPos, 3f);
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
