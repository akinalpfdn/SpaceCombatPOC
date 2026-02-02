// ============================================
// WEAPON SYSTEM - Strategy Pattern
// Handles weapon firing, cooldowns, and projectile spawning
// ============================================

using System;
using UnityEngine;
using VContainer;
using SpaceCombat.Interfaces;
using SpaceCombat.Events;
using SpaceCombat.ScriptableObjects;
using SpaceCombat.Utilities;

namespace SpaceCombat.Combat
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

        [Header("Audio Override")]
        [SerializeField] private string _fireSoundId; // Override fire sound (e.g., "enemy_laser" for enemies)

        [Header("State")]
        [SerializeField] private float _lastFireTime;
        [SerializeField] private Vector2 _aimDirection = Vector2.up;

        // Properties
        public bool CanFire => Time.time >= _lastFireTime + (_currentWeaponConfig?.fireRate ?? 0.2f);
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
        /// </summary>
        public void Fire()
        {
            if (_currentWeaponConfig == null) return;

            _lastFireTime = Time.time;

            // Calculate fire direction (use ship's forward if no specific aim)
            // 3D XZ plane: transform.forward gives ship facing direction (x, 0, z) -> convert to (x, z) for Vector2
            Vector2 fireDirection = _aimDirection.magnitude > 0.1f
                ? _aimDirection
                : new Vector2(transform.forward.x, transform.forward.z);

            // Determine which fire points to use
            bool hasMultipleFirePoints = _firePoints != null && _firePoints.Length > 1;

            if (hasMultipleFirePoints)
            {
                // Multi-fire-point: spawn from each mount point
                foreach (var fp in _firePoints)
                {
                    if (fp == null) continue;
                    SpawnProjectileAt(fp, fireDirection);
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
                    SpawnProjectileAt(_firePoint, direction);
                }
            }

            // Effects
            SpawnMuzzleFlash();
            PlayFireSound();

            // Events - 3D: convert Vector3 firePoint position to Vector2 (x, z)
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
        /// Spawn a projectile at the specified fire point
        /// </summary>
        private void SpawnProjectileAt(Transform firePoint, Vector2 direction)
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
                    _currentWeaponConfig.damage,
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
        /// Spawn muzzle flash effect at all active fire points
        /// </summary>
        private void SpawnMuzzleFlash()
        {
            if (_currentWeaponConfig?.muzzleFlashPrefab == null) return;

            bool hasMultipleFirePoints = _firePoints != null && _firePoints.Length > 1;

            if (hasMultipleFirePoints)
            {
                foreach (var fp in _firePoints)
                {
                    if (fp == null) continue;
                    var flash = Instantiate(_currentWeaponConfig.muzzleFlashPrefab,
                        fp.position, fp.rotation, fp);
                    Destroy(flash, 0.5f);
                }
            }
            else
            {
                var flash = Instantiate(_currentWeaponConfig.muzzleFlashPrefab,
                    _firePoint.position, _firePoint.rotation, _firePoint);
                Destroy(flash, 0.5f);
            }
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
                foreach (var fp in _firePoints)
                {
                    if (fp == null) continue;
                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(fp.position, _aimDirection * 2f);
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(fp.position, 0.15f);
                }
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
