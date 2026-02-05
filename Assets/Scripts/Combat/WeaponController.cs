// ============================================
// WEAPON SYSTEM - Strategy Pattern
// Handles weapon firing, cooldowns, and projectile spawning
// ============================================

using System;
using System.Collections;
using UnityEngine;
using VContainer;
using StarReapers.Interfaces;
using StarReapers.Events;
using StarReapers.ScriptableObjects;
using StarReapers.Utilities;
using StarReapers.VFX;

namespace StarReapers.Combat
{
    /// <summary>
    /// Controls weapon firing for a ship
    /// Manages multiple weapon slots and weapon switching
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private WeaponConfig _currentWeaponConfig;
        [SerializeField] private Transform _firePoint;
        [Tooltip("Multiple fire points for ships with multiple weapon mounts. If set, overrides single _firePoint.")]
        [SerializeField] private Transform[] _firePoints;
        [SerializeField] private LayerMask _targetLayers;
        [SerializeField] private bool _isPlayerWeapon = true;
        [Tooltip("Fallback convergence distance when no target is set. Used for multi-fire-point.")]
        [SerializeField] private float _fallbackConvergenceDistance = 15f;

        [Header("Audio Override")]
        [SerializeField] private string _fireSoundId; // Override fire sound (e.g., "enemy_laser" for enemies)

        [Header("State")]
        [SerializeField] private float _lastFireTime;
        [SerializeField] private Vector2 _aimDirection = Vector2.up;
        private Vector3? _targetPosition;
        private bool _isBurstFiring;
        private Coroutine _burstCoroutine;

        // Properties
        public bool CanFire => !_isBurstFiring && Time.time >= _lastFireTime + (_currentWeaponConfig?.fireRate ?? 0.2f);
        public WeaponConfig CurrentWeapon => _currentWeaponConfig;

        // Events
        public event Action OnWeaponFired;

        private ObjectPool<Projectile> _projectilePool;
        private PoolManager _poolManager;

        [Inject]
        public void Construct(PoolManager poolManager)
        {
            _poolManager = poolManager;
        }

        private void Start()
        {
            InitializeProjectilePool();
        }

        /// <summary>
        /// Initialize with weapon config and a single fire point.
        /// </summary>
        public void Initialize(WeaponConfig config, Transform firePoint = null)
        {
            _currentWeaponConfig = config;

            if (firePoint != null)
                _firePoint = firePoint;

            if (_firePoint == null)
                _firePoint = transform;

            InitializeProjectilePool();
        }

        /// <summary>
        /// Initialize with weapon config and multiple fire points.
        /// Each fire point spawns a projectile per shot (e.g. 3 fire points = 3 lasers).
        /// </summary>
        public void Initialize(WeaponConfig config, Transform[] firePoints)
        {
            _currentWeaponConfig = config;
            _firePoints = firePoints;

            // Keep single _firePoint as fallback (first in array)
            if (firePoints != null && firePoints.Length > 0)
                _firePoint = firePoints[0];

            if (_firePoint == null)
                _firePoint = transform;

            InitializeProjectilePool();
        }

        /// <summary>
        /// Set the target layers for projectiles
        /// </summary>
        public void SetTargetLayers(LayerMask layers)
        {
            _targetLayers = layers;
        }

        /// <summary>
        /// Set aim direction
        /// </summary>
        public void SetAimDirection(Vector2 direction)
        {
            if (direction.magnitude > 0.1f)
            {
                _aimDirection = direction.normalized;
            }
        }

        /// <summary>
        /// Set target position for multi-fire-point convergence.
        /// All fire points will aim at this world position.
        /// Pass null to clear (falls back to convergence distance).
        /// </summary>
        public void SetTargetPosition(Vector3? position)
        {
            _targetPosition = position;
        }

        /// <summary>
        /// Try to fire the weapon
        /// </summary>
        public bool TryFire()
        {
            if (!CanFire || _currentWeaponConfig == null)
                return false;

            Fire();
            return true;
        }

        /// <summary>
        /// Force fire (ignores cooldown).
        /// Spawns projectiles from all active fire points.
        /// Supports burst fire mode for DarkOrbit-style multi-shot lasers.
        /// </summary>
        public void Fire()
        {
            if (_currentWeaponConfig == null) return;

            _lastFireTime = Time.time;

            // Check if burst fire mode
            if (_currentWeaponConfig.IsBurstFire)
            {
                // Stop any existing burst
                if (_burstCoroutine != null)
                {
                    StopCoroutine(_burstCoroutine);
                }
                _burstCoroutine = StartCoroutine(FireBurstCoroutine());
            }
            else
            {
                // Normal single fire
                FireSingleVolley(1f);

                // Effects for normal fire (burst handles its own effects)
                SpawnMuzzleFlash();
                PlayFireSound();
            }

            // Events - 3D: convert Vector3 firePoint position to Vector2 (x, z)
            Vector2 fireDirection = _aimDirection.magnitude > 0.1f
                ? _aimDirection
                : new Vector2(transform.forward.x, transform.forward.z);
            Vector3 firePos = _firePoint.position;
            EventBus.Publish(new ProjectileFiredEvent(
                new Vector2(firePos.x, firePos.z),
                fireDirection,
                _currentWeaponConfig.weaponName,
                _isPlayerWeapon
            ));
            OnWeaponFired?.Invoke();
        }

        /// <summary>
        /// Coroutine for burst fire mode.
        /// Fires multiple volleys with delay between each.
        /// </summary>
        private IEnumerator FireBurstCoroutine()
        {
            _isBurstFiring = true;

            int burstCount = _currentWeaponConfig.burstCount;
            float burstDelay = _currentWeaponConfig.burstDelay;

            // Damage multiplier: split damage across burst shots
            // (fire points already split damage further if multiple)
            float burstDamageMultiplier = 1f / burstCount;

            for (int i = 0; i < burstCount; i++)
            {
                FireSingleVolley(burstDamageMultiplier);

                // Effects for each burst shot
                SpawnMuzzleFlash();
                PlayFireSound();

                // Wait between burst shots (except after last one)
                if (i < burstCount - 1)
                {
                    yield return new WaitForSeconds(burstDelay);
                }
            }

            _isBurstFiring = false;
            _burstCoroutine = null;
        }

        /// <summary>
        /// Fires a single volley from all fire points.
        /// damageMultiplier is applied for burst fire (e.g., 0.33 for 3-shot burst).
        /// </summary>
        private void FireSingleVolley(float damageMultiplier)
        {
            // Calculate fire direction (use ship's forward if no specific aim)
            // 3D XZ plane: transform.forward gives ship facing direction (x, 0, z) -> convert to (x, z) for Vector2
            Vector2 fireDirection = _aimDirection.magnitude > 0.1f
                ? _aimDirection
                : new Vector2(transform.forward.x, transform.forward.z);

            // Determine which fire points to use
            bool hasMultipleFirePoints = _firePoints != null && _firePoints.Length > 1;

            if (hasMultipleFirePoints)
            {
                // Multi-fire-point: split damage across fire points so total stays the same
                int activeCount = 0;
                foreach (var fp in _firePoints)
                    if (fp != null) activeCount++;

                // Combined multiplier: burst split * fire point split
                float combinedMultiplier = damageMultiplier * (activeCount > 0 ? 1f / activeCount : 1f);

                // Calculate aim point: use actual target position if available,
                // otherwise use fallback convergence distance ahead of ship
                Vector3 aimPoint;
                if (_targetPosition.HasValue)
                {
                    aimPoint = _targetPosition.Value;
                }
                else
                {
                    Vector3 forward3D = new Vector3(fireDirection.x, 0f, fireDirection.y);
                    aimPoint = transform.position + forward3D.normalized * _fallbackConvergenceDistance;
                }

                foreach (var fp in _firePoints)
                {
                    if (fp == null) continue;

                    // Direction from this fire point toward the aim point
                    Vector3 toTarget = aimPoint - fp.position;
                    Vector2 dir = new Vector2(toTarget.x, toTarget.z).normalized;

                    SpawnProjectileAt(fp, dir, combinedMultiplier);
                }
            }
            else
            {
                // Single fire point: use spread/multi-shot from config
                int projectileCount = _currentWeaponConfig.projectilesPerShot;
                float spreadAngle = _currentWeaponConfig.spreadAngle;

                for (int i = 0; i < projectileCount; i++)
                {
                    Vector2 direction = CalculateSpreadDirection(fireDirection, i, projectileCount, spreadAngle);
                    SpawnProjectileAt(_firePoint, direction, damageMultiplier);
                }
            }
        }

        /// <summary>
        /// Calculate direction with spread
        /// </summary>
        private Vector2 CalculateSpreadDirection(Vector2 baseDirection, int index, int total, float spreadAngle)
        {
            if (total == 1 || spreadAngle == 0)
            {
                // Add small random variation for accuracy
                float accuracy = _currentWeaponConfig?.accuracy ?? 1f;
                float randomAngle = UnityEngine.Random.Range(-1f, 1f) * (1f - accuracy) * 10f;
                return RotateVector(baseDirection, randomAngle);
            }

            // Calculate spread for multiple projectiles
            float startAngle = -spreadAngle / 2f;
            float angleStep = spreadAngle / (total - 1);
            float angle = startAngle + (angleStep * index);

            return RotateVector(baseDirection, angle);
        }

        /// <summary>
        /// Rotate a vector by degrees
        /// </summary>
        private Vector2 RotateVector(Vector2 v, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(
                v.x * cos - v.y * sin,
                v.x * sin + v.y * cos
            );
        }

        /// <summary>
        /// Spawn a projectile at the specified fire point.
        /// damageMultiplier splits damage across multiple fire points (e.g. 0.5 for 2 guns).
        /// </summary>
        private void SpawnProjectileAt(Transform firePoint, Vector2 direction, float damageMultiplier = 1f)
        {
            Projectile projectile = null;

            // Try to get from pool
            if (_projectilePool != null)
            {
                projectile = _projectilePool.Get(firePoint.position, Quaternion.identity);
            }
            else if (_currentWeaponConfig.projectilePrefab != null)
            {
                // Fallback to instantiate
                var go = Instantiate(_currentWeaponConfig.projectilePrefab,
                    firePoint.position, Quaternion.identity);
                projectile = go.GetComponent<Projectile>();
            }

            if (projectile != null)
            {
                // Set pool reference so projectile can return itself
                if (_projectilePool != null)
                    projectile.SetPool(_projectilePool);

                projectile.Initialize(
                    direction,
                    _currentWeaponConfig.damage * damageMultiplier,
                    _currentWeaponConfig.projectileSpeed,
                    _targetLayers
                );

                // Set visual properties
                var visualConfig = _currentWeaponConfig.projectileVisualConfig;
                if (visualConfig != null)
                {
                    // Mesh-based visual: apply config + HDR color
                    var visual = projectile.GetComponentInChildren<Interfaces.IProjectileVisual>();
                    if (visual is VFX.MeshProjectileVisual meshVisual)
                    {
                        meshVisual.ApplyConfig(visualConfig);
                    }
                    projectile.SetColor(_currentWeaponConfig.projectileColor, visualConfig.EmissionIntensity);
                }
                else
                {
                    projectile.SetColor(_currentWeaponConfig.projectileColor);
                }
                projectile.SetScale(_currentWeaponConfig.projectileScale);
                projectile.SetDamageType(_currentWeaponConfig.damageType);
                projectile.SetOwner(gameObject);
            }
        }

        /// <summary>
        /// Initialize projectile pool
        /// </summary>
        private void InitializeProjectilePool()
        {
            if (_currentWeaponConfig?.projectilePrefab == null) return;

            var projectilePrefab = _currentWeaponConfig.projectilePrefab.GetComponent<Projectile>();
            if (projectilePrefab == null) return;

            if (_poolManager != null)
            {
                string poolId = $"Projectile_{_currentWeaponConfig.weaponName}_{GetInstanceID()}";

                if (!_poolManager.HasPool(poolId))
                {
                    _projectilePool = _poolManager.CreatePool(poolId, projectilePrefab, 20, 100);
                }
                else
                {
                    _projectilePool = _poolManager.GetPool<Projectile>(poolId);
                }
            }
        }

        /// <summary>
        /// Spawn muzzle flash effect at all active fire points.
        /// If prefab is assigned, uses it; otherwise creates a default procedural flash.
        /// </summary>
        private void SpawnMuzzleFlash()
        {
            bool hasMultipleFirePoints = _firePoints != null && _firePoints.Length > 1;

            if (hasMultipleFirePoints)
            {
                foreach (var fp in _firePoints)
                {
                    if (fp == null) continue;
                    SpawnSingleMuzzleFlash(fp);
                }
            }
            else if (_firePoint != null)
            {
                SpawnSingleMuzzleFlash(_firePoint);
            }
        }

        /// <summary>
        /// Spawns a single muzzle flash at the given fire point.
        /// </summary>
        private void SpawnSingleMuzzleFlash(Transform firePoint)
        {
            GameObject flashObj;

            if (_currentWeaponConfig?.muzzleFlashPrefab != null)
            {
                // Use configured prefab
                flashObj = Instantiate(_currentWeaponConfig.muzzleFlashPrefab,
                    firePoint.position, firePoint.rotation, firePoint);
            }
            else
            {
                // Create procedural muzzle flash
                flashObj = CreateProceduralMuzzleFlash(firePoint);
            }

            // Set color to match weapon
            var muzzleFlash = flashObj.GetComponent<MuzzleFlash>();
            if (muzzleFlash != null && _currentWeaponConfig != null)
            {
                muzzleFlash.SetColor(_currentWeaponConfig.projectileColor,
                    _currentWeaponConfig.projectileVisualConfig?.EmissionIntensity ?? 3f);
            }

            // Safety destroy (MuzzleFlash handles its own destruction, but just in case)
            Destroy(flashObj, 0.5f);
        }

        /// <summary>
        /// Creates a procedural muzzle flash when no prefab is assigned.
        /// </summary>
        private GameObject CreateProceduralMuzzleFlash(Transform firePoint)
        {
            var flashObj = new GameObject("MuzzleFlash");
            flashObj.transform.SetParent(firePoint);
            flashObj.transform.localPosition = Vector3.zero;
            flashObj.transform.localRotation = Quaternion.identity;

            var muzzleFlash = flashObj.AddComponent<MuzzleFlash>();

            return flashObj;
        }

        /// <summary>
        /// Play weapon fire sound
        /// </summary>
        private void PlayFireSound()
        {
            // Use override sound if set, otherwise use config default
            string soundId = !string.IsNullOrEmpty(_fireSoundId)
                ? _fireSoundId
                : _currentWeaponConfig?.fireSoundId;

            if (!string.IsNullOrEmpty(soundId))
            {
                // 3D: convert Vector3 firePoint position to Vector2 (x, z)
                Vector3 firePos = _firePoint.position;
                EventBus.Publish(new PlaySFXEvent(
                    soundId,
                    new Vector2(firePos.x, firePos.z)
                ));
            }
        }

        /// <summary>
        /// Switch to a different weapon
        /// </summary>
        public void SwitchWeapon(WeaponConfig newWeapon)
        {
            _currentWeaponConfig = newWeapon;
            InitializeProjectilePool();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw all fire points
            bool hasMultipleFirePoints = _firePoints != null && _firePoints.Length > 0;

            if (hasMultipleFirePoints)
            {
                // Calculate aim point for gizmo display
                Vector3 aimPoint;
                if (_targetPosition.HasValue)
                {
                    aimPoint = _targetPosition.Value;
                }
                else
                {
                    Vector3 forward3D = new Vector3(_aimDirection.x, 0f, _aimDirection.y).normalized;
                    if (forward3D.magnitude < 0.1f)
                        forward3D = transform.forward;
                    aimPoint = transform.position + forward3D * _fallbackConvergenceDistance;
                }

                foreach (var fp in _firePoints)
                {
                    if (fp == null) continue;
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(fp.position, aimPoint);
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(fp.position, 0.15f);
                }

                Gizmos.color = _targetPosition.HasValue ? Color.red : Color.cyan;
                Gizmos.DrawWireSphere(aimPoint, 0.3f);
            }
            else if (_firePoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(_firePoint.position, _aimDirection * 2f);
            }

            // Draw weapon range
            if (_currentWeaponConfig != null && _firePoint != null)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
                Gizmos.DrawWireSphere(_firePoint.position, _currentWeaponConfig.range);
            }
        }
#endif
    }
}
