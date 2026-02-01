// ============================================
// TARGET SELECTION - Click to select enemies
// Player auto-faces selected target, SPACE to toggle attack
// DarkOrbit-style: mouse hold moves ship, click targets enemy
//
// Controls:
// - Click enemy → Select target
// - SPACE once → Start attacking selected target
// - SPACE again → Stop attacking
// - SHIFT → Select closest enemy in range & start attacking
// Auto-stops when target dies or out of range
// ============================================

using UnityEngine;
using SpaceCombat.Entities;
using SpaceCombat.Movement;
using SpaceCombat.UI;

namespace SpaceCombat.Combat
{
    /// <summary>
    /// Attach this to the Player ship.
    /// - Hold mouse button → ship moves toward cursor
    /// - Click enemy → select target (shows indicator)
    /// - SPACE → toggle attack on selected target
    /// - SHIFT → select closest enemy and start attacking
    /// </summary>
    public class TargetSelector : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _maxTargetRange = 20f;
        [SerializeField] private LayerMask _enemyLayer = -1;
        [SerializeField] private KeyCode _fireToggleKey = KeyCode.Space;
        [SerializeField] private KeyCode _selectClosestKey = KeyCode.LeftShift;

        [Header("Movement Settings")]
        [SerializeField] private float _stopDistance = 0.5f; // Stop moving when this close to cursor
        [SerializeField] private bool _requireMouseHoldToMove = true; // Require holding mouse to move

        [Header("References")]
        [SerializeField] private WeaponController _weaponController;
        [SerializeField] private ShipMovement _shipMovement;
        [SerializeField] private Camera _mainCamera;

        [Header("Target Indicator")]
        [SerializeField] private GameObject _targetIndicatorPrefab;
        [SerializeField] private GameObject _healthBarPrefab;

        // State
        private Transform _currentTarget;
        private TargetIndicator _currentIndicator;
        private HealthBar _targetHealthBar;
        private bool _isFiringEnabled = false;
        private bool _wasSpacePressed = false;

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

            // Handle SPACE key toggle for firing
            bool isSpacePressed = UnityEngine.Input.GetKey(_fireToggleKey);
            if (isSpacePressed && !_wasSpacePressed && _currentTarget != null)
            {
                _isFiringEnabled = !_isFiringEnabled; // Toggle state
            }
            _wasSpacePressed = isSpacePressed;

            // Handle SHIFT key - select closest enemy and start attacking
            if (UnityEngine.Input.GetKeyDown(_selectClosestKey))
            {
                SelectClosestEnemy();
            }

            // First check if current target is still valid, clear if not
            if (_currentTarget != null && !IsValidTarget(_currentTarget))
            {
                ClearTarget();
            }

            // Then handle aiming and firing (only if target is still valid)
            if (_currentTarget != null)
            {
                UpdateAimAtTarget();

                // Fire continuously if toggle is enabled
                if (_isFiringEnabled && _weaponController != null)
                {
                    _weaponController.TryFire();
                }
            }
            else
            {
                // No target, stop firing
                _isFiringEnabled = false;
            }
        }

        private void OnDestroy()
        {
            // Clean up indicator when selector is destroyed
            ClearIndicator();
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
                        ClearTarget();
                    }
                    else
                    {
                        // Clear previous indicator
                        ClearIndicator();

                        _currentTarget = enemy.transform;
                        // Disable auto-rotation when targeting so we can face the target
                        if (_shipMovement != null)
                            _shipMovement.SetAutoRotate(false);

                        // Spawn indicator on target
                        SpawnTargetIndicator(_currentTarget);
                    }
                }
            }
        }

        private void SelectClosestEnemy()
        {
            // Find all enemies within range
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _maxTargetRange, _enemyLayer);

            Transform closestEnemy = null;
            float closestDistance = Mathf.Infinity;

            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<Enemy>();
                if (enemy != null && enemy.IsAlive)
                {
                    float distance = Vector2.Distance(transform.position, enemy.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestEnemy = enemy.transform;
                    }
                }
            }

            // If found closest enemy, select it and start firing
            if (closestEnemy != null)
            {
                // Clear previous indicator
                ClearIndicator();

                _currentTarget = closestEnemy;
                _isFiringEnabled = true; // Auto-start attacking

                // Disable auto-rotation when targeting
                if (_shipMovement != null)
                    _shipMovement.SetAutoRotate(false);

                // Spawn indicator on target
                SpawnTargetIndicator(_currentTarget);
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

            // Check if target GameObject is still active (not pooled/destroyed)
            if (!target.gameObject.activeInHierarchy) return false;

            var enemy = target.GetComponent<Enemy>();
            if (enemy == null) return false;

            // Check if alive and in range
            return enemy.IsAlive && IsInRange(target);
        }

        public void ClearTarget()
        {
            _currentTarget = null;
            _isFiringEnabled = false;
            ClearIndicator();
            // Re-enable auto-rotation when clearing target
            if (_shipMovement != null)
                _shipMovement.SetAutoRotate(true);
        }

        private void SpawnTargetIndicator(Transform target)
        {
            if (_targetIndicatorPrefab == null) return;

            var indicatorObj = Instantiate(_targetIndicatorPrefab, target.position, Quaternion.identity);
            _currentIndicator = indicatorObj.GetComponent<TargetIndicator>();

            // Get target indicator scale from enemy config
            var enemy = target.GetComponent<Enemy>();
            float scale = 1f;
            if (enemy != null)
            {
                scale = enemy.TargetIndicatorScale;
            }

            // Apply scale to indicator
            indicatorObj.transform.localScale = Vector3.one * scale;

            if (_currentIndicator != null)
            {
                _currentIndicator.SetTarget(target);
                // Pass the base scale so pulse animation works correctly
                _currentIndicator.SetBaseScale(scale);
            }

            // Spawn health bar - use prefab if assigned, otherwise fallback to AddComponent
            if (_healthBarPrefab != null)
            {
                var healthBarObj = Instantiate(_healthBarPrefab, target.position, Quaternion.identity);
                _targetHealthBar = healthBarObj.GetComponent<HealthBar>();
            }
            else
            {
                _targetHealthBar = indicatorObj.AddComponent<HealthBar>();
                _targetHealthBar.SetOffset(new Vector2(0, -1.5f));
            }

            if (_targetHealthBar != null)
            {
                _targetHealthBar.SetAlwaysShow(true);

                var targetEntity = target.GetComponent<BaseEntity>();
                if (targetEntity != null)
                {
                    _targetHealthBar.SetTarget(targetEntity);
                }
            }
        }

        private void ClearIndicator()
        {
            if (_currentIndicator != null)
            {
                Destroy(_currentIndicator.gameObject);
                _currentIndicator = null;
            }

            // Clean up health bar if it's a separate prefab instance
            if (_targetHealthBar != null && _healthBarPrefab != null)
            {
                if (_targetHealthBar.transform.parent == null)
                {
                    Destroy(_targetHealthBar.gameObject);
                }
            }
            _targetHealthBar = null;
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
