// ============================================
// PARALLAX BACKGROUND - Depth layer system
// Multiple layers move at different speeds
// ============================================

using UnityEngine;

namespace SpaceCombat.Environment
{
   

    /// <summary>
    /// Parallax scrolling background for space environment
    /// Multiple layers move at different speeds based on camera movement
    /// 3D Version - Camera moves on XZ plane
    /// </summary>
    public class ParallaxBackground : MonoBehaviour
    { 
        [System.Serializable]
    public class ParallaxLayer
    {
        public Transform transform;
        [Range(0f, 1f)] public float parallaxFactor = 0.5f;
        public bool infiniteHorizontal = true;
        public bool infiniteVertical = true;
    }
        [Header("Settings")]
        [SerializeField] private ParallaxLayer[] _layers;
        [SerializeField] private Transform _cameraTransform;

        private Vector3 _cameraStartPos;
        private Vector3[] _layerStartPositions;

        private void Start()
        {
            if (_cameraTransform == null)
            {
                _cameraTransform = Camera.main?.transform;
            }

            // Use world origin (0,0,0) as reference instead of camera start position
            _cameraStartPos = Vector3.zero;

            // Store initial positions
            _layerStartPositions = new Vector3[_layers.Length];
            for (int i = 0; i < _layers.Length; i++)
            {
                if (_layers[i].transform != null)
                {
                    _layerStartPositions[i] = _layers[i].transform.position;
                }
            }
        }

        private void LateUpdate()
        {
            if (_cameraTransform == null) return;

            // How far camera has moved from start
            Vector3 cameraDelta = _cameraTransform.position - _cameraStartPos;

            for (int i = 0; i < _layers.Length; i++)
            {
                var layer = _layers[i];
                if (layer.transform == null) continue;

                // Layer moves with camera - creating depth illusion
                // Factor 0.98 = moves 98% with camera (appears very slow on screen - DarkOrbit style)
                // Factor 0.5 = moves 50% with camera (appears medium speed)
                // 3D Version: Use X and Z axes (camera moves on XZ plane)
                Vector3 targetPos = _layerStartPositions[i] + (cameraDelta * layer.parallaxFactor);
                layer.transform.position = new Vector3(targetPos.x, _layerStartPositions[i].y, targetPos.z);
            }
        }
    }
}
