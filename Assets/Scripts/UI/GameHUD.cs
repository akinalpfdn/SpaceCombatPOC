// ============================================
// MINIMAL HUD - POC UI Elements
// Health bar, shield bar, score display
// ============================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VContainer;
using StarReapers.Events;

namespace StarReapers.UI
{
    /// <summary>
    /// Minimal HUD for POC demonstration
    /// Shows player health, shield, and score
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        [Header("Health Display")]
        [SerializeField] private Slider _healthBar;
        [SerializeField] private Image _healthFill;
        [SerializeField] private Color _healthHighColor = Color.green;
        [SerializeField] private Color _healthLowColor = Color.red;

        [Header("Shield Display")]
        [SerializeField] private Slider _shieldBar;
        [SerializeField] private GameObject _shieldContainer;

        [Header("Score Display")]
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private string _scoreFormat = "Score: {0:N0}";

        [Header("Enemy Count Display")]
        [SerializeField] private TextMeshProUGUI _enemyCountText;
        [SerializeField] private string _enemyCountFormat = "Enemies: {0}";

        [Header("Game Over")]
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private TextMeshProUGUI _finalScoreText;

        [Header("Force Position (Runtime)")]
        [SerializeField] private bool _forcePositionAtRuntime = true;

        private Core.GameManager _gameManager;

        [Inject]
        public void Construct(Core.GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        private void Awake()
        {
            SubscribeToEvents();

            if (_gameOverPanel != null)
                _gameOverPanel.SetActive(false);
        }

        private void Start()
        {
            if (_forcePositionAtRuntime)
            {
                ForcePositionUI();
            }
        }

        private void ForcePositionUI()
        {
            RectTransform canvasRect = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
            if (canvasRect == null) return;

            float halfWidth = canvasRect.rect.width / 2f;
            float halfHeight = canvasRect.rect.height / 2f;

            // Position relative to canvas center, using local coordinates
            // Top-left corner is (-halfWidth, halfHeight)
            // Top-right corner is (halfWidth, halfHeight)

            if (_healthBar != null)
            {
                var rect = _healthBar.GetComponent<RectTransform>();
                rect.localPosition = new Vector3(-halfWidth + 150, halfHeight - 50, 0);
            }

            if (_shieldBar != null)
            {
                var rect = _shieldBar.GetComponent<RectTransform>();
                rect.localPosition = new Vector3(-halfWidth + 150, halfHeight - 100, 0);
            }

            if (_scoreText != null)
            {
                var rect = _scoreText.GetComponent<RectTransform>();
                rect.localPosition = new Vector3(halfWidth - 200, halfHeight - 50, 0);
            }

            if (_enemyCountText != null)
            {
                var rect = _enemyCountText.GetComponent<RectTransform>();
                rect.localPosition = new Vector3(halfWidth - 200, halfHeight - 100, 0);
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<PlayerHealthChangedEvent>(OnHealthChanged);
            EventBus.Subscribe<PlayerShieldChangedEvent>(OnShieldChanged);
            EventBus.Subscribe<ScoreChangedEvent>(OnScoreChanged);
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<PlayerHealthChangedEvent>(OnHealthChanged);
            EventBus.Unsubscribe<PlayerShieldChangedEvent>(OnShieldChanged);
            EventBus.Unsubscribe<ScoreChangedEvent>(OnScoreChanged);
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnHealthChanged(PlayerHealthChangedEvent evt)
        {
            if (_healthBar != null)
            {
                _healthBar.value = evt.Percentage;
                
                if (_healthFill != null)
                {
                    _healthFill.color = Color.Lerp(_healthLowColor, _healthHighColor, evt.Percentage);
                }
            }
        }

        private void OnShieldChanged(PlayerShieldChangedEvent evt)
        {
            if (_shieldBar != null)
            {
                float percentage = evt.MaxShield > 0 ? evt.CurrentShield / evt.MaxShield : 0;
                _shieldBar.value = percentage;
            }

            if (_shieldContainer != null)
            {
                _shieldContainer.SetActive(evt.MaxShield > 0);
            }
        }

        private void OnScoreChanged(ScoreChangedEvent evt)
        {
            if (_scoreText != null)
            {
                _scoreText.text = string.Format(_scoreFormat, evt.NewScore);
            }
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            if (evt.NewState == GameState.GameOver)
            {
                ShowGameOver();
            }
            else if (evt.NewState == GameState.Playing)
            {
                HideGameOver();
            }
        }

        private void ShowGameOver()
        {
            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(true);
                
                if (_finalScoreText != null)
                {
                    var gm = _gameManager;
                    _finalScoreText.text = $"Final Score: {gm?.Score ?? 0:N0}";
                }
            }
        }

        private void HideGameOver()
        {
            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(false);
            }
        }

        private void Update()
        {
            // Update enemy count display
            if (_enemyCountText != null)
            {
                var gm = _gameManager;
                if (gm != null)
                {
                    _enemyCountText.text = string.Format(_enemyCountFormat, gm.EnemiesAlive);
                }
            }
        }

        // Button callbacks
        public void OnRestartClicked()
        {
            _gameManager?.RestartGame();
        }

        public void OnPauseClicked()
        {
            var gm = _gameManager;
            if (gm != null)
            {
                if (gm.CurrentState == GameState.Playing)
                    gm.PauseGame();
                else if (gm.CurrentState == GameState.Paused)
                    gm.ResumeGame();
            }
        }
    }

    /// <summary>
    /// Floating damage numbers
    /// Spawn when damage is dealt
    /// </summary>
    public class DamageNumber : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private float _floatSpeed = 1f;
        [SerializeField] private float _lifetime = 1f;
        [SerializeField] private AnimationCurve _scaleCurve;
        [SerializeField] private AnimationCurve _alphaCurve;

        private float _timer;
        private Color _startColor;
        private Vector3 _startScale;

        public void Initialize(float damage, Color color)
        {
            if (_text != null)
            {
                _text.text = Mathf.RoundToInt(damage).ToString();
                _text.color = color;
                _startColor = color;
            }
            
            _startScale = transform.localScale;
            _timer = 0;
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            float t = _timer / _lifetime;

            // Float upward
            transform.position += Vector3.up * _floatSpeed * Time.deltaTime;

            // Scale animation
            if (_scaleCurve != null)
            {
                float scale = _scaleCurve.Evaluate(t);
                transform.localScale = _startScale * scale;
            }

            // Fade out
            if (_text != null && _alphaCurve != null)
            {
                float alpha = _alphaCurve.Evaluate(t);
                var color = _startColor;
                color.a = alpha;
                _text.color = color;
            }

            // Destroy when done
            if (_timer >= _lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
