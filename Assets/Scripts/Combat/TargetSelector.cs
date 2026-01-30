// ============================================
// TARGET SELECTION - Click to select enemies
// Player auto-faces and shoots at selected target
// DarkOrbit-style: mouse hold moves ship, click targets enemy
// ============================================

using UnityEngine;
using SpaceCombat.Entities;
using SpaceCombat.Movement;

namespace SpaceCombat.Combat
{
    /// <summary>
    /// Attach this to the Player ship.
    /// - Hold mouse button → ship moves toward cursor
    /// - Click enemy → target and auto-attack
    /// </summary>
    public class TargetSelector : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _maxTargetRange = 20f;
        [SerializeField] private LayerMask _enemyLayer = -1;
        [SerializeField] private bool _autoFireWhenTargeted = true;

        [Header("Movement Settings")]
        [SerializeField] private float _stopDistance = 0.5f; // Stop moving when this close to cursor
        [SerializeField] private bool _requireMouseHoldToMove = true; // Require holding mouse to move

        [Header("References")]
        [SerializeField] private WeaponController _weaponController;
        [SerializeField] private ShipMovement _shipMovement;
        [SerializeField] private Camera _mainCamera;

        // State
        private Transform _currentTarget;

        // Properties
        public Transform CurrentTarget => _currentTarget;
        public Vector2 MouseWorldPosition { get; private set; }

        private void Start()
        {
            if (_mainCamera == null)
                _mainCamera = Camera.main;

            if (_weaponController == null)
                _weaponController = GetComponent<WeaponController>();

            if (_shipMovement == null)
                _shipMovement = GetComponent<ShipMovement>();

            // Set target layers so projectiles hit enemies
            if (_weaponController != null)
                _weaponController.SetTargetLayers(_enemyLayer);
        }

        private void Update()
        {
            UpdateMousePosition();

            // Handle mouse click to select target (only if clicking directly on enemy)
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                TrySelectTarget();
            }

            // Handle movement toward mouse (when holding mouse button)
            HandleMovement();

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

        private void UpdateMousePosition()
        {
            Vector3 mousePos = _mainCamera.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
            MouseWorldPosition = new Vector2(mousePos.x, mousePos.y);
        }

        private void HandleMovement()
        {
            if (_shipMovement == null) return;

            // Only move if mouse is held (or always if disabled)
            bool shouldMove = !_requireMouseHoldToMove || UnityEngine.Input.GetMouseButton(0);

            if (shouldMove)
            {
                Vector2 toMouse = MouseWorldPosition - (Vector2)transform.position;
                float distance = toMouse.magnitude;

                // Stop if close enough to cursor
                if (distance > _stopDistance)
                {
                    _shipMovement.Move(toMouse.normalized);
                }
                else
                {
                    _shipMovement.Stop();
                }

                // Always face movement direction if no target
                if (_currentTarget == null && distance > _stopDistance)
                {
                    _shipMovement.RotateTowards(toMouse.normalized);
                }
            }
            else
            {
                _shipMovement.Stop();
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
                    // Toggle target: if already targeting this enemy, clear it
                    if (_currentTarget == enemy.transform)
                    {
                        _currentTarget = null;
                    }
                    else
                    {
                        _currentTarget = enemy.transform;
                    }
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
            if (_shipMovement != null)
            {
                _shipMovement.RotateTowards(direction);
            }
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
