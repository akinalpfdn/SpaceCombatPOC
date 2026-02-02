using UnityEngine;
using VContainer;

namespace SpaceCombat.Environment
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform _target;
        [SerializeField] private bool _findPlayerOnStart = true;

        private Core.GameManager _gameManager;

        [Inject]
        public void Construct(Core.GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        [Header("Follow Settings")]
        [SerializeField] private float _smoothSpeed = 20f;  // Increased for tighter follow
        [SerializeField] private Vector3 _offset = new Vector3(0, 30, -20);  // 3D: Y=height above ground, Z=behind player
        [SerializeField] private bool _useDirectFollow = true;  // Direct follow for interpolated rigidbodies

        private Vector3 _velocity;

        private void Start()
        {
            if (_findPlayerOnStart && _target == null)
            {
                TryFindPlayer();
            }
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                TryFindPlayer();
                return;
            }

            Vector3 targetPosition = _target.position + _offset;

            // When using interpolated rigidbodies, direct follow feels smoother
            // SmoothDamp on top of interpolation causes staggering
            if (_useDirectFollow)
            {
                transform.position = targetPosition;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    targetPosition,
                    ref _velocity,
                    1f / _smoothSpeed
                );
            }
        }

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        private void TryFindPlayer()
        {
            if (_gameManager != null && _gameManager.Player != null)
            {
                _target = _gameManager.Player.transform;
            }
        }
    }
}