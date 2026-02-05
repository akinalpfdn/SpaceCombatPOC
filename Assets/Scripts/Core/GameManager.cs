// ============================================
// GAME MANAGER - Facade Pattern (Refactored)
// Coordinates all game systems
// Delegates spawning to ISpawnService (SRP)
// ============================================

using System.Collections;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using StarReapers.Events;
using StarReapers.Utilities;
using StarReapers.Interfaces;
using StarReapers.Spawning;
using StarReapers.Maps;

namespace StarReapers.Core
{
    /// <summary>
    /// Main game manager - Facade for all subsystems.
    /// 
    /// Refactored to follow SOLID principles:
    /// - Single Responsibility: Game state management only
    /// - Open/Closed: Extensible via services
    /// - Dependency Inversion: Depends on ISpawnService abstraction
    /// 
    /// Spawning logic moved to EnemySpawnService.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Game Settings")]
        [SerializeField] private ScriptableObjects.GameBalanceConfig _balanceConfig;
        [SerializeField] private MapConfig _mapConfig;
        [SerializeField] private SpawnConfig _spawnConfig;

        [Header("References")]
        [SerializeField] private Transform _playerSpawnPoint;
        [SerializeField] private Entities.PlayerShip _playerPrefab;
        
        [Header("Spawn Service (Auto-assigned if null)")]
        [SerializeField] private EnemySpawnService _spawnService;

        [Header("State")]
        [SerializeField] private GameState _currentState = GameState.Loading;
        [SerializeField] private int _score = 0;

        [Header("Respawn Settings")]
        [SerializeField] private float _respawnDelay = 2f;
        [SerializeField] private bool _enableRespawn = true;

        // Runtime references
        private Entities.PlayerShip _player;
        private ISpawnService _spawnServiceInterface;
        private Coroutine _respawnCoroutine;
        private IObjectResolver _container;

        // Boss spawning state
        private bool _bossSpawned = false;
        private int _killCount = 0;
        private float _gameTimer = 0f;

        [Inject]
        public void Construct(ISpawnService spawnService, IObjectResolver container)
        {
            _spawnServiceInterface = spawnService;
            _container = container;
        }

        // Properties
        public GameState CurrentState => _currentState;
        public int Score => _score;
        public Entities.PlayerShip Player => _player;
        public int EnemiesAlive => _spawnServiceInterface?.ActiveEnemyCount ?? 0;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
        }

        private void Start()
        {
            InitializeGame();
        }

        private void Update()
        {
            if (_currentState == GameState.Playing)
            {
                _gameTimer += Time.deltaTime;
                CheckBossSpawnTrigger();
            }
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
            // Get or create spawn service
            InitializeSpawnService();
            
            SubscribeToEvents();
            
            // Start game
            StartCoroutine(GameStartSequence());
        }

        private void InitializeSpawnService()
        {
            // Prefer inspector reference
            if (_spawnService != null)
            {
                _spawnServiceInterface = _spawnService;
            }

            // _spawnServiceInterface already set by [Inject] if available
            if (_spawnServiceInterface == null)
            {
                Debug.LogError("[GameManager] No ISpawnService available! Assign via inspector or VContainer.");
                return;
            }

            // Initialize with MapConfig (preferred) or SpawnConfig (legacy)
            if (_mapConfig != null && _mapConfig.IsValid)
            {
                _spawnService.Initialize(_mapConfig);
                _spawnConfig = _mapConfig.spawnConfig; // Keep reference for spawn counts
            }
            else if (_spawnConfig != null)
            {
                _spawnServiceInterface.Initialize(_spawnConfig);
            }
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

            // Wait another frame for player to be ready
            yield return null;

            // Spawn initial enemies using spawn service
            SpawnInitialEnemies();

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
            _killCount = 0;
            _gameTimer = 0f;
            _bossSpawned = false;

            // Reset enemies via spawn service
            _spawnServiceInterface?.ReturnAllEnemies();

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
            SpawnInitialEnemies();
        }

        // ============================================
        // SPAWNING (Delegated to SpawnService)
        // ============================================

        private void SpawnPlayer()
        {
            if (_player == null && _playerPrefab != null)
            {
                Vector3 spawnPos = _playerSpawnPoint != null
                    ? _playerSpawnPoint.position
                    : Vector3.zero;

                _player = Instantiate(_playerPrefab, spawnPos, Quaternion.identity);
                _container?.InjectGameObject(_player.gameObject);
                _player.gameObject.tag = "Player";
                _player.gameObject.layer = LayerMask.NameToLayer("Player");
            }
        }

        private void SpawnInitialEnemies()
        {
            if (_spawnServiceInterface == null)
            {
                Debug.LogError("[GameManager] SpawnService not available!");
                return;
            }

            Vector3 playerPosition = _player != null ? _player.transform.position : Vector3.zero;
            int enemyCount = _spawnConfig?.InitialEnemyCount ?? 10;

            _spawnServiceInterface.SpawnInitialEnemies(enemyCount, playerPosition);

            // Check for immediate boss spawn (None trigger)
            if (_mapConfig?.bossSettings != null &&
                _mapConfig.bossSettings.enabled &&
                _mapConfig.bossSettings.spawnTrigger == BossSpawnTrigger.None)
            {
                SpawnBoss();
            }
        }

        // ============================================
        // BOSS SPAWNING
        // ============================================

        private void CheckBossSpawnTrigger()
        {
            if (_bossSpawned) return;
            if (_mapConfig?.bossSettings == null || !_mapConfig.bossSettings.enabled) return;

            var settings = _mapConfig.bossSettings;

            switch (settings.spawnTrigger)
            {
                case BossSpawnTrigger.Timer:
                    if (_gameTimer >= settings.triggerValue)
                    {
                        SpawnBoss();
                    }
                    break;

                case BossSpawnTrigger.KillCount:
                    if (_killCount >= (int)settings.triggerValue)
                    {
                        SpawnBoss();
                    }
                    break;

                // None is handled in SpawnInitialEnemies
                // WaveComplete and Manual are triggered externally
            }
        }

        private void SpawnBoss()
        {
            if (_bossSpawned) return;
            if (_spawnService == null) return;

            var boss = _spawnService.SpawnBoss();
            if (boss != null)
            {
                _bossSpawned = true;
                Debug.Log($"[GameManager] Boss spawned! Kill count: {_killCount}, Timer: {_gameTimer:F1}s");
            }
        }

        /// <summary>
        /// Manually trigger boss spawn (for Manual trigger type or testing).
        /// </summary>
        public void TriggerBossSpawn()
        {
            SpawnBoss();
        }

        private void RespawnEnemy()
        {
            if (_spawnServiceInterface == null || _player == null) return;
            
            _spawnServiceInterface.SpawnEnemy(_player.transform.position);
        }

        private IEnumerator RespawnEnemyAfterDelay()
        {
            float delay = _spawnConfig?.RespawnDelay ?? _respawnDelay;
            yield return new WaitForSeconds(delay);

            if (_currentState == GameState.Playing)
            {
                RespawnEnemy();
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
            SetState(GameState.GameOver);
        }

        private void OnEnemyDeath(EntityDeathEvent evt)
        {
            // Return enemy to pool via spawn service
            if (evt.Entity != null)
            {
                _spawnServiceInterface?.ReturnEnemy(evt.Entity);
            }

            // Track kill count for boss trigger
            _killCount++;

            // Schedule respawn if enabled
            bool shouldRespawn = _spawnConfig?.EnableRespawn ?? _enableRespawn;

            if (_currentState == GameState.Playing && shouldRespawn)
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
            // TODO: Show game over UI
        }

        private void OnVictory()
        {
            Debug.Log($"Victory! Final Score: {_score}");
            // TODO: Show victory UI
        }

        // ============================================
        // PUBLIC API
        // ============================================

        public void AddScore(int amount)
        {
            _score += amount;
            EventBus.Publish(new ScoreChangedEvent(_score, amount));
        }

        /// <summary>
        /// Change spawn strategy at runtime.
        /// </summary>
        public void SetSpawnStrategy(SpawnDistributionType strategyType)
        {
            _spawnServiceInterface?.SetStrategy(strategyType);
        }

        /// <summary>
        /// Force spawn an enemy (e.g., for testing or special events).
        /// </summary>
        public void ForceSpawnEnemy()
        {
            if (_player != null)
            {
                _spawnServiceInterface?.SpawnEnemy(_player.transform.position);
            }
        }

        /// <summary>
        /// Force spawn enemy at specific position.
        /// </summary>
        public void ForceSpawnEnemyAt(Vector3 position)
        {
            _spawnServiceInterface?.SpawnEnemyAt(position);
        }
    }
}
