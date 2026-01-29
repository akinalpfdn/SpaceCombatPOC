// ============================================
// INFINITE BACKGROUND - Scrolling tile
// Repositions when moving out of view
// ============================================

using UnityEngine;

namespace SpaceCombat.Environment
{
    /// <summary>
    /// Infinite scrolling background tile
    /// Repositions when moving out of view
    /// </summary>
    public class InfiniteBackground : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Vector2 _tileSize = new Vector2(20f, 20f);
        [SerializeField] private Transform _cameraTransform;

        private Vector3 _startPosition;

        private void Start()
        {
            _startPosition = transform.position;

            if (_cameraTransform == null)
            {
                _cameraTransform = Camera.main?.transform;
            }
        }

        private void Update()
        {
            if (_cameraTransform == null) return;

            Vector3 cameraPos = _cameraTransform.position;
            Vector3 myPos = transform.position;

            // Check horizontal wrap
            float distX = cameraPos.x - myPos.x;
            if (Mathf.Abs(distX) > _tileSize.x)
            {
                float offset = Mathf.Sign(distX) * _tileSize.x * 2f;
                myPos.x += offset;
            }

            // Check vertical wrap
            float distY = cameraPos.y - myPos.y;
            if (Mathf.Abs(distY) > _tileSize.y)
            {
                float offset = Mathf.Sign(distY) * _tileSize.y * 2f;
                myPos.y += offset;
            }

            transform.position = myPos;
        }
    }
}
