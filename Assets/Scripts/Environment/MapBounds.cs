// ============================================
// MAP BOUNDS - Limits player movement to map area
// ============================================

using UnityEngine;

namespace SpaceCombat.Environment
{
    /// <summary>
    /// Defines map boundaries and clamps player position
    /// Attach to an empty GameObject or place in scene
    /// </summary>
    public class MapBounds : MonoBehaviour
    {
        [Header("Map Size")]
        [SerializeField] private float _mapWidth = 410f;
        [SerializeField] private float _mapHeight = 275f;

        [Header("Visual Border")]
        [SerializeField] private bool _showBorder = true;
        [SerializeField] private Color _borderColor = Color.red;
        [SerializeField] private float _borderThickness = 2f;

        [Header("Padding")]
        [SerializeField] private float _padding = 2f; // Keep player slightly inside

        private Vector2 _minBounds;
        private Vector2 _maxBounds;

        public Rect Bounds => new Rect(-_mapWidth / 2, -_mapHeight / 2, _mapWidth, _mapHeight);
        public Vector2 MinBounds => _minBounds;
        public Vector2 MaxBounds => _maxBounds;

        private void Start()
        {
            CalculateBounds();
        }

        private void CalculateBounds()
        {
            _minBounds = new Vector2(-_mapWidth / 2 + _padding, -_mapHeight / 2 + _padding);
            _maxBounds = new Vector2(_mapWidth / 2 - _padding, _mapHeight / 2 - _padding);
        }

        private void OnValidate()
        {
            CalculateBounds();
        }

        /// <summary>
        /// Clamp a position to stay within bounds
        /// </summary>
        public Vector2 ClampPosition(Vector2 position)
        {
            return new Vector2(
                Mathf.Clamp(position.x, _minBounds.x, _maxBounds.x),
                Mathf.Clamp(position.y, _minBounds.y, _maxBounds.y)
            );
        }

        /// <summary>
        /// Check if a position is within bounds
        /// </summary>
        public bool IsInBounds(Vector2 position)
        {
            return position.x >= _minBounds.x && position.x <= _maxBounds.x &&
                   position.y >= _minBounds.y && position.y <= _maxBounds.y;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!_showBorder) return;

            // Calculate center and size
            Vector3 center = transform.position;
            Vector3 size = new Vector3(_mapWidth, _mapHeight, 0);

            // Draw the border
            Gizmos.color = _borderColor;
            Gizmos.DrawWireCube(center, size);

            // Draw semi-transparent fill
            Gizmos.color = new Color(_borderColor.r, _borderColor.g, _borderColor.b, 0.1f);
            Gizmos.DrawCube(center, size);
        }
#endif
    }
}
