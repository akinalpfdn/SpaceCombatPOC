// ============================================
// GAME MANAGER - Facade Pattern
// Coordinates all game systems
// Entry point for game state management
// ============================================

using System.Collections;
using UnityEngine;
using SpaceCombat.Events;
using SpaceCombat.Utilities;

namespace SpaceCombat.Core
{
    /// <summary>
    /// Main game manager - Facade for all subsystems
    /// Handles game state, spawning, and enemy management
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField] private ScriptableObjects.GameBalanceConfig _balanceConfig;

        [Header("References")]
        [SerializeField] private Transform _playerSpawnPoint;
        [SerializeField] private Entities.PlayerShip _playerPrefab;
        [SerializeField] private Entities.Enemy _enemyPrefab;
        [SerializeField] private Environment.MapBounds _mapBounds;

        [Header("Enemy Spawning")]
        [SerializeField] private int _enemyCount = 10;
        [SerializeField] private float _minDistanceFromPlayer = 20f;
        [SerializeField] private float _spawnDelayAfterDeath = 2f;

        [Header("State")]
        [SerializeField] private GameState _currentState = GameState.Loading;
        [SerializeField] private int _score = 0;

        // Runtime references
        private Entities.PlayerShip _player;
        private ObjectPool<Entities.Enemy> _enemyPool;
        private int _enemiesAlive;
        private Coroutine _respawnCoroutine;

        public GameState CurrentState => _currentState;
        public int Score => _score;
        public Entities.PlayerShip Player => _player;
        public int EnemiesAlive => _enemiesAlive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            ServiceLocator.Register(this);
        }

        private void Start()
        {
            InitializeGame();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        // ============================================
        // INITIALIZATION
        // ============================================

        private void InitializeGame()
        {
            SubscribeToEvents();
            SetupObjectPools();
            
            // Start game
            StartCoroutine(GameStartSequence());
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<EntityDeathEvent>(OnEntityDeath);
            EventBus.Subscribe<ScoreChangedEvent>(OnScoreChanged);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<EntityDeathEvent>(OnEntityDeath);
            EventBus.Unsubscribe<ScoreChangedEvent>(OnScoreChanged);
        }

        private void SetupObjectPools()
        {
            var poolManager = PoolManager.Instance;
            if (poolManager == null)
            {
                var poolGO = new GameObject("PoolManager");
                poolManager = poolGO.AddComponent<PoolManager>();
            }

            if (_enemyPrefab != null)
            {
                _enemyPool = poolManager.CreatePool("Enemies", _enemyPrefab, 10, 50);
            }
        }

        // ============================================
        // GAME FLOW
        // ============================================

        private IEnumerator GameStartSequence()
        {
            SetState(GameState.Loading);

            // Wait a frame for other systems to initialize
            yield return null;

            // Spawn player
            SpawnPlayer();

            // Spawn all enemies at once
            SpawnAllEnemiesAtStart();

            // Start playing
            SetState(GameState.Playing);
        }

        public void SetState(GameState newState)
        {
            if (_currentState == newState) return;

            var previousState = _currentState;
            _currentState = newState;

            EventBus.Publish(new GameStateChangedEvent(previousState, newState));

            switch (newState)
            {
                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;
                case GameState.Playing:
                    Time.timeScale = 1f;
                    break;
                case GameState.GameOver:
                    OnGameOver();
                    break;
                case GameState.Victory:
                    OnVictory();
                    break;
            }
        }

        public void PauseGame()
        {
            if (_currentState == GameState.Playing)
            {
                SetState(GameState.Paused);
            }
        }

        public void ResumeGame()
        {
            if (_currentState == GameState.Paused)
            {
                SetState(GameState.Playing);
            }
        }

        public void RestartGame()
        {
            _score = 0;

            // Reset enemies
            _enemyPool?.ReturnAll();
            _enemiesAlive = 0;

            // Reset player
            if (_player != null)
            {
                _player.Respawn(_playerSpawnPoint.position);
            }
            else
            {
                SpawnPlayer();
            }

            SetState(GameState.Playing);
            SpawnAllEnemiesAtStart();
        }

        // ============================================
        // SPAWNING
        // ============================================

        private void SpawnPlayer()
        {
            if (_player == null && _playerPrefab != null)
            {
                // 3D Version: Use Vector3 for spawn position
                Vector3 spawnPos = _playerSpawnPoint != null
                    ? _playerSpawnPoint.position
                    : Vector3.zero;

                _player = Instantiate(_playerPrefab, spawnPos, Quaternion.identity);
                _player.gameObject.tag = "Player";
                _player.gameObject.layer = LayerMask.NameToLayer("Player");
            }
        }

        public void SpawnEnemy(Vector2 position, ScriptableObjects.EnemyConfig config = null)
        {
            if (_enemyPool == null) return;

            // 3D Version: Convert 2D (x,y) to 3D (x, 0, z) for XZ plane
            Vector3 spawnPos3D = new Vector3(position.x, 0f, position.y);

            var enemy = _enemyPool.Get(spawnPos3D, Quaternion.identity);
            if (enemy != null)
            {
                if (config != null)
                {
                    enemy.ApplyConfig(config);
                }

                enemy.gameObject.layer = LayerMask.NameToLayer("Enemy");

                EventBus.Publish(new EnemySpawnedEvent(
                    enemy.gameObject,
                    position,
                    config?.enemyName ?? "Enemy"
                ));
            }
        }

        // ============================================
        // SPAWN MANAGEMENT
        // ============================================

        private void SpawnAllEnemiesAtStart()
        {
            for (int i = 0; i < _enemyCount; i++)
            {
                Vector2 spawnPos = GetRandomSpawnPosition();
                SpawnEnemy(spawnPos);
                _enemiesAlive++;
            }
        }

        private Vector2 GetRandomSpawnPosition()
        {
            if (_mapBounds == null)
            {
                // Fallback: Random position around circle (3D: on XZ plane)
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = 50f; // Further away for fallback
                return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            }

            Rect bounds = _mapBounds.Bounds;

            // Simply spawn randomly across the entire map bounds
            // No minimum distance check - truly random scattering
            float x = Random.Range(bounds.xMin, bounds.xMax);
            float y = Random.Range(bounds.yMin, bounds.yMax);
            return new Vector2(x, y);
        }

        private IEnumerator RespawnEnemyAfterDelay()
        {
            yield return new WaitForSeconds(_spawnDelayAfterDeath);

            if (_currentState == GameState.Playing)
            {
                Vector2 spawnPos = GetRandomSpawnPosition();
                SpawnEnemy(spawnPos);
                _enemiesAlive++;
            }

            _respawnCoroutine = null;
        }

        // ============================================
        // EVENT HANDLERS
        // ============================================

        private void OnEntityDeath(EntityDeathEvent evt)
        {
            if (evt.IsPlayer)
            {
                OnPlayerDeath();
            }
            else
            {
                OnEnemyDeath(evt);
            }
        }

        private void OnPlayerDeath()
        {
            // Check lives remaining (future feature)
            SetState(GameState.GameOver);
        }

        private void OnEnemyDeath(EntityDeathEvent evt)
        {
            _enemiesAlive--;

            // Return enemy to pool
            var enemy = evt.Entity.GetComponent<Entities.Enemy>();
            if (enemy != null && _enemyPool != null)
            {
                _enemyPool.Return(enemy);
            }

            // Schedule respawn to maintain enemy count
            if (_currentState == GameState.Playing)
            {
                if (_respawnCoroutine != null)
                {
                    StopCoroutine(_respawnCoroutine);
                }
                _respawnCoroutine = StartCoroutine(RespawnEnemyAfterDelay());
            }
        }

        private void OnScoreChanged(ScoreChangedEvent evt)
        {
            _score = evt.NewScore;
        }

        private void OnGameOver()
        {
            Debug.Log($"Game Over! Final Score: {_score}");
            // Show game over UI
        }

        private void OnVictory()
        {
            Debug.Log($"Victory! Final Score: {_score}");
            // Show victory UI
        }

        // ============================================
        // PUBLIC API
        // ============================================

        public void AddScore(int amount)
        {
            _score += amount;
            EventBus.Publish(new ScoreChangedEvent(_score, amount));
        }
    }
}
