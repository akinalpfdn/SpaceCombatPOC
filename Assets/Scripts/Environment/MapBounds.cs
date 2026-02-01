// ============================================
// MAP BOUNDS - Limits player movement to map area
// ============================================

using UnityEngine;

namespace SpaceCombat.Environment
{
    /// <summary>
    /// Defines map boundaries and clamps player position
    /// Attach to an empty GameObject or place in scene
    /// 3D Version - XZ plane movement
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

        private Vector3 _minBounds;
        private Vector3 _maxBounds;

        public Rect Bounds => new Rect(-_mapWidth / 2, -_mapHeight / 2, _mapWidth, _mapHeight);
        public Vector3 MinBounds => _minBounds;
        public Vector3 MaxBounds => _maxBounds;

        private void Start()
        {
            CalculateBounds();
        }

        private void CalculateBounds()
        {
            // For 3D XZ plane: X is width, Z is height (converted from 2D Y)
            _minBounds = new Vector3(-_mapWidth / 2 + _padding, 0, -_mapHeight / 2 + _padding);
            _maxBounds = new Vector3(_mapWidth / 2 - _padding, 0, _mapHeight / 2 - _padding);
        }

        private void OnValidate()
        {
            CalculateBounds();
        }

        /// <summary>
        /// Clamp a position to stay within bounds (2D version for backward compatibility)
        /// </summary>
        public Vector2 ClampPosition(Vector2 position)
        {
            return new Vector2(
                Mathf.Clamp(position.x, _minBounds.x, _maxBounds.x),
                Mathf.Clamp(position.y, _minBounds.z, _maxBounds.z)
            );
        }

        /// <summary>
        /// Clamp a position to stay within bounds (3D version for XZ plane)
        /// </summary>
        public Vector3 ClampPosition3D(Vector3 position)
        {
            return new Vector3(
                Mathf.Clamp(position.x, _minBounds.x, _maxBounds.x),
                0, // Lock Y to 0
                Mathf.Clamp(position.z, _minBounds.z, _maxBounds.z)
            );
        }

        /// <summary>
        /// Check if a position is within bounds
        /// </summary>
        public bool IsInBounds(Vector2 position)
        {
            return position.x >= _minBounds.x && position.x <= _maxBounds.x &&
                   position.y >= _minBounds.z && position.y <= _maxBounds.z;
        }

        /// <summary>
        /// Check if a position is within bounds (3D version)
        /// </summary>
        public bool IsInBounds(Vector3 position)
        {
            return position.x >= _minBounds.x && position.x <= _maxBounds.x &&
                   position.z >= _minBounds.z && position.z <= _maxBounds.z;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!_showBorder) return;

            // Calculate center and size for 3D (XZ plane)
            Vector3 center = transform.position;
            Vector3 size = new Vector3(_mapWidth, 1f, _mapHeight); // Y is small since we're on XZ plane

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
