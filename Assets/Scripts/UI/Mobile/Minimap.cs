// ============================================
// MINIMAP - Shows nearby enemies and player position
// Icon-based minimap for top-down space combat
// ============================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using SpaceCombat.Entities;

namespace SpaceCombat.UI.Mobile
{
    /// <summary>
    /// Simple icon-based minimap that shows:
    /// - Player as green dot (center)
    /// - Enemies as red dots (relative position)
    ///
    /// Works with XZ plane (top-down view).
    /// Enemies outside scan range are clamped to edge.
    /// </summary>
    public class Minimap : MonoBehaviour
    {
        // ============================================
        // CONFIGURATION
        // ============================================

        [Header("References")]
        [SerializeField] private RectTransform _minimapRect;
        [SerializeField] private Image _playerIcon;
        [SerializeField] private Image _backgroundImage;

        [Header("Icon Prefab")]
        [SerializeField] private Image _enemyIconPrefab;
        [SerializeField] private Transform _iconContainer;

        [Header("Scan Settings")]
        [Tooltip("World units radius to scan for enemies")]
        [SerializeField] private float _scanRadius = 100f;
        [Tooltip("How often to update enemy positions (seconds)")]
        [SerializeField] private float _updateInterval = 0.1f;

        [Header("Visual Settings")]
        [SerializeField] private Color _playerColor = new Color(0f, 1f, 0.5f, 1f); // Green
        [SerializeField] private Color _enemyColor = new Color(1f, 0.2f, 0.2f, 1f); // Red
        [SerializeField] private float _iconSize = 8f;
        [Tooltip("Show enemies at edge when outside scan range")]
        [SerializeField] private bool _clampToEdge = false;
        [SerializeField] private float _edgePadding = 5f;

        // ============================================
        // RUNTIME STATE
        // ============================================

        private Transform _playerTransform;
        private List<Image> _enemyIcons = new List<Image>();
        private List<Image> _iconPool = new List<Image>();
        private float _updateTimer;
        private float _minimapRadius;
        private Enemy[] _cachedEnemies; // Cache to reduce GC

        // ============================================
        // UNITY LIFECYCLE
        // ============================================

        private void Start()
        {
            // Calculate minimap radius
            if (_minimapRect != null)
            {
                _minimapRadius = Mathf.Min(_minimapRect.rect.width, _minimapRect.rect.height) * 0.5f - _edgePadding;
            }

            // Set player icon color
            if (_playerIcon != null)
            {
                _playerIcon.color = _playerColor;
                _playerIcon.rectTransform.sizeDelta = new Vector2(_iconSize * 1.5f, _iconSize * 1.5f);
            }

            // Try to find player
            TryFindPlayer();
        }

        private void Update()
        {
            // Try to find player if not found
            if (_playerTransform == null)
            {
                TryFindPlayer();
                return;
            }

            // Update on interval for performance
            _updateTimer += Time.deltaTime;
            if (_updateTimer >= _updateInterval)
            {
                _updateTimer = 0f;
                UpdateMinimap();
            }
        }

        // ============================================
        // PRIVATE METHODS
        // ============================================

        private void TryFindPlayer()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
            }
        }

        private void UpdateMinimap()
        {
            // Find all enemies by component (not tag)
            _cachedEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

            // Ensure we have enough icons
            EnsureIconCount(_cachedEnemies.Length);

            // Update each enemy icon
            int activeCount = 0;
            foreach (var enemy in _cachedEnemies)
            {
                if (enemy == null || !enemy.IsActive) continue;

                Vector3 relativePos = enemy.transform.position - _playerTransform.position;

                // Convert world XZ to minimap XY
                Vector2 minimapPos = new Vector2(relativePos.x, relativePos.z);

                // Scale to minimap
                float distance = minimapPos.magnitude;
                float scaledDistance = (distance / _scanRadius) * _minimapRadius;

                // Check if in range
                bool inRange = distance <= _scanRadius;

                if (inRange || _clampToEdge)
                {
                    // Normalize direction and apply scaled distance
                    Vector2 direction = minimapPos.normalized;

                    if (_clampToEdge && scaledDistance > _minimapRadius)
                    {
                        scaledDistance = _minimapRadius;
                    }

                    Vector2 iconPos = direction * scaledDistance;

                    // Position the icon
                    if (activeCount < _enemyIcons.Count)
                    {
                        var icon = _enemyIcons[activeCount];
                        icon.gameObject.SetActive(true);
                        icon.rectTransform.anchoredPosition = iconPos;

                        // Fade out icons at edge
                        if (_clampToEdge && !inRange)
                        {
                            icon.color = new Color(_enemyColor.r, _enemyColor.g, _enemyColor.b, 0.5f);
                        }
                        else
                        {
                            icon.color = _enemyColor;
                        }

                        activeCount++;
                    }
                }
            }

            // Hide unused icons
            for (int i = activeCount; i < _enemyIcons.Count; i++)
            {
                _enemyIcons[i].gameObject.SetActive(false);
            }
        }

        private void EnsureIconCount(int needed)
        {
            // Create more icons if needed
            while (_enemyIcons.Count < needed)
            {
                Image icon = GetOrCreateIcon();
                _enemyIcons.Add(icon);
            }
        }

        private Image GetOrCreateIcon()
        {
            // Try to get from pool
            foreach (var pooled in _iconPool)
            {
                if (!_enemyIcons.Contains(pooled))
                {
                    return pooled;
                }
            }

            // Create new icon
            Image newIcon;
            if (_enemyIconPrefab != null)
            {
                newIcon = Instantiate(_enemyIconPrefab, _iconContainer);
            }
            else
            {
                // Create simple solid color icon (no sprite needed - Image renders color)
                var go = new GameObject("EnemyIcon");
                go.transform.SetParent(_iconContainer, false);

                // Add RectTransform with center anchor
                var rect = go.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);

                newIcon = go.AddComponent<Image>();
                newIcon.color = _enemyColor;
            }

            // Setup icon
            newIcon.rectTransform.sizeDelta = new Vector2(_iconSize, _iconSize);
            newIcon.raycastTarget = false;
            _iconPool.Add(newIcon);

            return newIcon;
        }

        // ============================================
        // PUBLIC METHODS
        // ============================================

        /// <summary>
        /// Set the scan radius at runtime.
        /// </summary>
        public void SetScanRadius(float radius)
        {
            _scanRadius = Mathf.Max(10f, radius);
        }

        /// <summary>
        /// Manually set the player transform reference.
        /// </summary>
        public void SetPlayer(Transform player)
        {
            _playerTransform = player;
        }
    }
}
