// ============================================
// MINIMAP BOUNDS DISPLAY - Shows map boundaries on minimap
// Uses simple Image objects as line indicators
// ============================================

using UnityEngine;
using UnityEngine.UI;
using StarReapers.Environment;

namespace StarReapers.UI.Mobile
{
    /// <summary>
    /// Draws map boundary lines on the minimap as the player approaches them.
    /// Creates 4 thin Image objects (one per edge) and positions them each frame.
    /// Lines are clipped by the minimap's circular UI Mask automatically.
    ///
    /// Setup: Place on the same GameObject as the minimap icon container
    /// (must be inside the circular Mask hierarchy).
    /// </summary>
    public class MinimapBoundsDisplay : MonoBehaviour
    {
        // ============================================
        // CONFIGURATION
        // ============================================

        [Header("References")]
        [SerializeField] private MapBounds _mapBounds;

        [Header("Display Settings")]
        [Tooltip("World units radius - should match Minimap scan radius")]
        [SerializeField] private float _scanRadius = 100f;

        [Tooltip("Line thickness in UI pixels")]
        [SerializeField] private float _lineThickness = 2f;

        [Tooltip("Color of boundary lines")]
        [SerializeField] private Color _boundsColor = new Color(1f, 0.3f, 0.3f, 0.8f);

        [Tooltip("Edge padding - should match Minimap edge padding")]
        [SerializeField] private float _edgePadding = 5f;

        // ============================================
        // RUNTIME STATE
        // ============================================

        private Transform _playerTransform;
        private float _minimapRadius;
        private RectTransform _ownRect;

        // 4 line images: 0=left, 1=right, 2=bottom, 3=top
        private RectTransform[] _lineRects = new RectTransform[4];

        // ============================================
        // UNITY LIFECYCLE
        // ============================================

        private void Start()
        {
            _ownRect = GetComponent<RectTransform>();
            CreateLineObjects();
            TryFindPlayer();
        }

        private void Update()
        {
            if (_playerTransform == null)
            {
                TryFindPlayer();
                return;
            }

            UpdateLines();
        }

        // ============================================
        // PRIVATE METHODS
        // ============================================

        private void CreateLineObjects()
        {
            string[] names = { "BoundLeft", "BoundRight", "BoundBottom", "BoundTop" };

            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject(names[i]);
                go.transform.SetParent(transform, false);

                var rect = go.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);

                var img = go.AddComponent<Image>();
                img.color = _boundsColor;
                img.raycastTarget = false;

                _lineRects[i] = rect;
                go.SetActive(false);
            }
        }

        private void UpdateLines()
        {
            if (_mapBounds == null || _ownRect == null) return;

            // Calculate minimap radius from own RectTransform (stretched to fill minimap)
            _minimapRadius = Mathf.Min(_ownRect.rect.width, _ownRect.rect.height) * 0.5f - _edgePadding;
            if (_minimapRadius <= 0f) return;

            Vector3 playerPos = _playerTransform.position;

            // Get padded map bounds (where player actually gets clamped)
            Vector3 boundsMin = _mapBounds.MinBounds;
            Vector3 boundsMax = _mapBounds.MaxBounds;

            // Convert each edge's world position to minimap coordinates
            float leftX = (boundsMin.x - playerPos.x) / _scanRadius * _minimapRadius;
            float rightX = (boundsMax.x - playerPos.x) / _scanRadius * _minimapRadius;
            float bottomY = (boundsMin.z - playerPos.z) / _scanRadius * _minimapRadius;
            float topY = (boundsMax.z - playerPos.z) / _scanRadius * _minimapRadius;

            // Lines span full minimap diameter - circular mask clips them
            float fullSpan = _minimapRadius * 2f;

            // Left edge (vertical line)
            SetLine(0, leftX, fullSpan, true);
            // Right edge (vertical line)
            SetLine(1, rightX, fullSpan, true);
            // Bottom edge (horizontal line)
            SetLine(2, bottomY, fullSpan, false);
            // Top edge (horizontal line)
            SetLine(3, topY, fullSpan, false);
        }

        /// <summary>
        /// Positions a line Image. Vertical lines have fixed X, horizontal lines have fixed Y.
        /// Lines are hidden when their edge position is outside the minimap radius.
        /// </summary>
        private void SetLine(int index, float edgePosition, float span, bool vertical)
        {
            bool visible = Mathf.Abs(edgePosition) < _minimapRadius + _lineThickness;
            _lineRects[index].gameObject.SetActive(visible);

            if (!visible) return;

            if (vertical)
            {
                _lineRects[index].sizeDelta = new Vector2(_lineThickness, span);
                _lineRects[index].anchoredPosition = new Vector2(edgePosition, 0f);
            }
            else
            {
                _lineRects[index].sizeDelta = new Vector2(span, _lineThickness);
                _lineRects[index].anchoredPosition = new Vector2(0f, edgePosition);
            }
        }

        private void TryFindPlayer()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
            }
        }

        // ============================================
        // PUBLIC METHODS
        // ============================================

        /// <summary>
        /// Set the player transform reference.
        /// </summary>
        public void SetPlayer(Transform player)
        {
            _playerTransform = player;
        }

        /// <summary>
        /// Set the scan radius at runtime. Should match Minimap scan radius.
        /// </summary>
        public void SetScanRadius(float radius)
        {
            _scanRadius = Mathf.Max(10f, radius);
        }
    }
}
