using UnityEngine;

namespace SpaceCombat.Environment
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform _target;
        [SerializeField] private bool _findPlayerOnStart = true;

        [Header("Follow Settings")]
        [SerializeField] private float _smoothSpeed = 5f;
        [SerializeField] private Vector3 _offset = new Vector3(0, 0, -225);

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
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref _velocity,
                1f / _smoothSpeed
            );
        }

        public void SetTarget(Transform target)
        {
            _target = target;
        }
    }
}