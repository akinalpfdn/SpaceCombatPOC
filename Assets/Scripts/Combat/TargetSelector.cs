// ============================================
// TARGET SELECTION - Click to select enemies
// Player auto-faces selected target, SPACE to toggle attack
// DarkOrbit-style: mouse hold moves ship, click targets enemy
//
// Controls:
// - Click enemy → Select target
// - Double-click enemy → Select target & toggle attack
// - SPACE once → Start attacking selected target
// - SPACE again → Stop attacking
// - SHIFT → Select closest enemy in range & start attacking
// Auto-stops when target dies or out of range
// ============================================

using UnityEngine;
using SpaceCombat.Entities;
using SpaceCombat.Interfaces;
using SpaceCombat.Movement;
using SpaceCombat.UI;

namespace SpaceCombat.Combat
{
    /// <summary>
    /// Attach this to the Player ship.
    /// - Hold mouse button → ship moves toward cursor
    /// - Click enemy → select target (shows indicator)
    /// - Double-click enemy → select target & toggle attack
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

        [Header("Double-Click")]
        [SerializeField] private float _doubleClickTime = 0.3f;

        [Header("Target Indicator")]
        [SerializeField] private GameObject _targetIndicatorPrefab;
        [SerializeField] private GameObject _healthBarPrefab;

        // State
        private Transform _currentTarget;
        private ITargetIndicator _currentIndicator;
        private HealthBar _targetHealthBar;
        private bool _isFiringEnabled = false;
        private bool _wasSpacePressed = false;
        private float _lastClickTime;
        private Transform _lastClickedTarget;

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
            // For 3D, raycast to find mouse position on XZ plane (Y=0)
            Ray ray = _mainCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            Plane xzPlane = new Plane(Vector3.up, Vector3.zero);

            float enterDistance;
            if (xzPlane.Raycast(ray, out enterDistance))
            {
                Vector3 hitPoint = ray.GetPoint(enterDistance);
                MouseWorldPosition = new Vector2(hitPoint.x, hitPoint.z);
            }
        }

        private void HandleMovement()
        {
            if (_shipMovement == null) return;

            // Only move if mouse is held (or always if disabled)
            bool shouldMove = !_requireMouseHoldToMove || UnityEngine.Input.GetMouseButton(0);

            if (shouldMove)
            {
                Vector3 currentPos = transform.position;
                Vector3 mousePos3D = new Vector3(MouseWorldPosition.x, 0, MouseWorldPosition.y);
                Vector3 toMouse = mousePos3D - currentPos;
                float distance = toMouse.magnitude;

                // Stop if close enough to cursor
                if (distance > _stopDistance)
                {
                    Vector2 direction = new Vector2(toMouse.x, toMouse.z).normalized;
                    _shipMovement.Move(direction);
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
            // Raycast from mouse position for 3D
            Ray ray = _mainCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, _enemyLayer))
            {
                var enemy = hit.collider.GetComponent<Enemy>();
                if (enemy != null && IsInRange(enemy.transform))
                {
                    // Detect double-click on same target
                    bool isDoubleClick = (Time.unscaledTime - _lastClickTime < _doubleClickTime)
                        && _lastClickedTarget == enemy.transform;

                    _lastClickTime = Time.unscaledTime;
                    _lastClickedTarget = enemy.transform;

                    if (isDoubleClick)
                    {
                        // Double-click: toggle firing (same as SPACE)
                        if (_currentTarget == enemy.transform)
                        {
                            _isFiringEnabled = !_isFiringEnabled;
                        }
                        else
                        {
                            // Double-click on new enemy: select & start attacking
                            ClearIndicator();
                            _currentTarget = enemy.transform;
                            _isFiringEnabled = true;

                            if (_shipMovement != null)
                                _shipMovement.SetAutoRotate(false);

                            SpawnTargetIndicator(_currentTarget);
                        }
                    }
                    else
                    {
                        // Single click: toggle target selection (existing behavior)
                        if (_currentTarget == enemy.transform)
                        {
                            ClearTarget();
                        }
                        else
                        {
                            ClearIndicator();

                            _currentTarget = enemy.transform;
                            if (_shipMovement != null)
                                _shipMovement.SetAutoRotate(false);

                            SpawnTargetIndicator(_currentTarget);
                        }
                    }
                }
            }
        }

        private void SelectClosestEnemy()
        {
            // Find all enemies within range using 3D physics
            Collider[] hits = Physics.OverlapSphere(transform.position, _maxTargetRange, _enemyLayer);

            Transform closestEnemy = null;
            float closestDistance = Mathf.Infinity;

            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<Enemy>();
                if (enemy != null && enemy.IsAlive)
                {
                    float distance = Vector3.Distance(transform.position, enemy.transform.position);
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

            // Calculate direction to target (3D to 2D conversion)
            Vector3 direction3D = (_currentTarget.position - transform.position).normalized;
            Vector2 direction = new Vector2(direction3D.x, direction3D.z);

            // Set weapon aim direction + target position for multi-fire-point convergence
            _weaponController.SetAimDirection(direction);
            _weaponController.SetTargetPosition(_currentTarget.position);

            // Also rotate ship to face target
            if (_shipMovement != null)
            {
                _shipMovement.RotateTowards(direction);
            }
        }

        private bool IsInRange(Transform target)
        {
            if (target == null) return false;
            return Vector3.Distance(transform.position, target.position) <= _maxTargetRange;
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
            // Clear weapon target position so it falls back to convergence distance
            if (_weaponController != null)
                _weaponController.SetTargetPosition(null);
            // Re-enable auto-rotation when clearing target
            if (_shipMovement != null)
                _shipMovement.SetAutoRotate(true);
        }

        private void SpawnTargetIndicator(Transform target)
        {
            if (_targetIndicatorPrefab == null) return;

            var indicatorObj = Instantiate(_targetIndicatorPrefab, target.position, Quaternion.identity);

            // Try to get indicator component (supports both 2D and 3D versions)
            _currentIndicator = indicatorObj.GetComponent<ITargetIndicator>();

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
                _targetHealthBar.SetOffset(new Vector3(0, 0, -1.5f));  // 3D: offset on Z for "below"
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
            // Draw targeting range (3D sphere)
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
