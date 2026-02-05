// ============================================
// INFINITE BACKGROUND - Scrolling tile
// Repositions when moving out of view
// ============================================

using UnityEngine;

namespace StarReapers.Environment
{
    /// <summary>
    /// Infinite scrolling background tile
    /// Repositions when moving out of view
    /// 3D Version - Camera moves on XZ plane
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

            // Check depth wrap (Z axis in 3D)
            float distZ = cameraPos.z - myPos.z;
            if (Mathf.Abs(distZ) > _tileSize.y)
            {
                float offset = Mathf.Sign(distZ) * _tileSize.y * 2f;
                myPos.z += offset;
            }

            transform.position = myPos;
        }
    }
}
