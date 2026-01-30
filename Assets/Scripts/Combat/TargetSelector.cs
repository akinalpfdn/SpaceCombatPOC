// ============================================
// TARGET SELECTION - Click to select enemies
// Player auto-faces and shoots at selected target
// ============================================

using UnityEngine;
using SpaceCombat.Entities;

namespace SpaceCombat.Combat
{
    /// <summary>
    /// Attach this to the Player ship.
    /// Click on enemies to target them - ship auto-faces and shoots.
    /// </summary>
    public class TargetSelector : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _maxTargetRange = 20f;
        [SerializeField] private LayerMask _enemyLayer = -1;
        [SerializeField] private bool _autoFireWhenTargeted = true;

        [Header("References")]
        [SerializeField] private WeaponController _weaponController;
        [SerializeField] private PlayerShip _playerShip;

        // State
        private Transform _currentTarget;
        private Camera _mainCamera;

        // Properties
        public Transform CurrentTarget => _currentTarget;

        private void Start()
        {
            _mainCamera = Camera.main;

            if (_weaponController == null)
                _weaponController = GetComponent<WeaponController>();

            if (_playerShip == null)
                _playerShip = GetComponent<PlayerShip>();

            // Set target layers so projectiles hit enemies
            _weaponController.SetTargetLayers(_enemyLayer);
        }

        private void Update()
        {
            // Handle mouse click to select target
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                TrySelectTarget();
            }

            // Update aim at target
            if (_currentTarget != null)
            {
                UpdateAimAtTarget();

                // Auto-fire if targeting
                if (_autoFireWhenTargeted && _weaponController != null)
                {
                    _weaponController.TryFire();
                }

                // Clear target if dead or too far
                if (!IsValidTarget(_currentTarget))
                {
                    _currentTarget = null;
                }
            }
        }

        private void TrySelectTarget()
        {
            // Raycast from mouse position
            Ray ray = _mainCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity, _enemyLayer);

            if (hit.collider != null)
            {
                var enemy = hit.collider.GetComponent<Enemy>();
                if (enemy != null && IsInRange(enemy.transform))
                {
                    _currentTarget = enemy.transform;
                }
            }
        }

        private void UpdateAimAtTarget()
        {
            if (_currentTarget == null || _weaponController == null) return;

            // Calculate direction to target
            Vector2 direction = (_currentTarget.position - transform.position).normalized;

            // Set weapon aim direction
            _weaponController.SetAimDirection(direction);

            // Also rotate ship to face target
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                20f * Time.deltaTime
            );
        }

        private bool IsInRange(Transform target)
        {
            if (target == null) return false;
            return Vector2.Distance(transform.position, target.position) <= _maxTargetRange;
        }

        private bool IsValidTarget(Transform target)
        {
            if (target == null) return false;

            var enemy = target.GetComponent<Enemy>();
            if (enemy == null) return false;

            // Check if alive and in range
            return enemy.IsAlive && IsInRange(target);
        }

        public void ClearTarget()
        {
            _currentTarget = null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw targeting range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _maxTargetRange);

            // Draw line to current target
            if (_currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, _currentTarget.position);
            }
        }
#endif
    }
}
