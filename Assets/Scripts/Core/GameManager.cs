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
    /// Handles game state, spawning, and wave management
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField] private ScriptableObjects.GameBalanceConfig _balanceConfig;

        [Header("References")]
        [SerializeField] private Transform _playerSpawnPoint;
        [SerializeField] private Transform[] _enemySpawnPoints;
        [SerializeField] private Entities.PlayerShip _playerPrefab;
        [SerializeField] private Entities.Enemy _enemyPrefab;

        [Header("State")]
        [SerializeField] private GameState _currentState = GameState.Loading;
        [SerializeField] private int _currentWave = 0;
        [SerializeField] private int _score = 0;
        [SerializeField] private int _enemiesRemaining = 0;

        // Runtime references
        private Entities.PlayerShip _player;
        private ObjectPool<Entities.Enemy> _enemyPool;

        public GameState CurrentState => _currentState;
        public int CurrentWave => _currentWave;
        public int Score => _score;
        public Entities.PlayerShip Player => _player;

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

            // Start playing
            SetState(GameState.Playing);

            // Start first wave after delay
            yield return new WaitForSeconds(1f);
            StartWave(_currentWave + 1);
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
            _currentWave = 0;
            _enemiesRemaining = 0;

            // Reset enemies
            _enemyPool?.ReturnAll();

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
            StartCoroutine(StartWaveDelayed(1f));
        }

        // ============================================
        // SPAWNING
        // ============================================

        private void SpawnPlayer()
        {
            if (_player == null && _playerPrefab != null)
            {
                Vector2 spawnPos = _playerSpawnPoint != null 
                    ? _playerSpawnPoint.position 
                    : Vector2.zero;

                _player = Instantiate(_playerPrefab, spawnPos, Quaternion.identity);
                _player.gameObject.tag = "Player";
                _player.gameObject.layer = LayerMask.NameToLayer("Player");
            }
        }

        public void SpawnEnemy(Vector2 position, ScriptableObjects.EnemyConfig config = null)
        {
            if (_enemyPool == null) return;

            var enemy = _enemyPool.Get(position, Quaternion.identity);
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
        // WAVE MANAGEMENT
        // ============================================

        public void StartWave(int waveNumber)
        {
            _currentWave = waveNumber;
            
            int enemyCount = CalculateEnemiesForWave(waveNumber);
            _enemiesRemaining = enemyCount;

            EventBus.Publish(new WaveStartedEvent(waveNumber, enemyCount));

            StartCoroutine(SpawnWaveEnemies(enemyCount));
        }

        private IEnumerator SpawnWaveEnemies(int count)
        {
            float spawnDelay = _balanceConfig?.timeBetweenSpawns ?? 1f;

            for (int i = 0; i < count; i++)
            {
                if (_currentState != GameState.Playing) yield break;

                Vector2 spawnPos = GetRandomSpawnPosition();
                SpawnEnemy(spawnPos);

                yield return new WaitForSeconds(spawnDelay);
            }
        }

        private IEnumerator StartWaveDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);
            StartWave(_currentWave + 1);
        }

        private int CalculateEnemiesForWave(int wave)
        {
            if (_balanceConfig != null)
            {
                return _balanceConfig.baseEnemiesPerWave + 
                       (_balanceConfig.additionalEnemiesPerWave * (wave - 1));
            }
            return 5 + (wave - 1) * 2;
        }

        private Vector2 GetRandomSpawnPosition()
        {
            if (_enemySpawnPoints != null && _enemySpawnPoints.Length > 0)
            {
                int index = Random.Range(0, _enemySpawnPoints.Length);
                return _enemySpawnPoints[index].position;
            }

            // Random position around edges of screen
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = 15f;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
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
            _enemiesRemaining--;

            // Return enemy to pool
            var enemy = evt.Entity.GetComponent<Entities.Enemy>();
            if (enemy != null && _enemyPool != null)
            {
                _enemyPool.Return(enemy);
            }

            // Check if wave complete
            if (_enemiesRemaining <= 0 && _currentState == GameState.Playing)
            {
                OnWaveComplete();
            }
        }

        private void OnScoreChanged(ScoreChangedEvent evt)
        {
            _score = evt.NewScore;
        }

        private void OnWaveComplete()
        {
            EventBus.Publish(new WaveCompletedEvent(_currentWave, Time.time));

            // Start next wave after delay
            float delay = _balanceConfig?.timeBetweenWaves ?? 3f;
            StartCoroutine(StartWaveDelayed(delay));
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
