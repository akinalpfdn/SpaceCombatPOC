using UnityEngine;

namespace SpaceCombat.Environment
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform _target;
        [SerializeField] private bool _findPlayerOnStart = true;

        [Header("Follow Settings")]
        [SerializeField] private float _smoothSpeed = 20f;  // Increased for tighter follow
        [SerializeField] private Vector3 _offset = new Vector3(0, 0, -225);
        [SerializeField] private bool _useDirectFollow = true;  // Direct follow for interpolated rigidbodies

        private Vector3 _velocity;

        private void Start()
        {
            if (_findPlayerOnStart && _target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    _target = player.transform;
                }
            }
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    _target = player.transform;
                }
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
    }
}