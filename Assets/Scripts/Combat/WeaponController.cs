// ============================================
// WEAPON SYSTEM - Strategy Pattern
// Handles weapon firing, cooldowns, and projectile spawning
// ============================================

using System;
using UnityEngine;
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
        [SerializeField] private LayerMask _targetLayers;
        [SerializeField] private bool _isPlayerWeapon = true;

        [Header("Projectile Appearance Override")]
        [SerializeField] private Sprite _projectileSprite; // Assign blue sprite for player, red for enemy

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

        private void Start()
        {
            InitializeProjectilePool();
        }

        /// <summary>
        /// Initialize with weapon config
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
        /// Force fire (ignores cooldown)
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

            // Fire multiple projectiles if configured
            int projectileCount = _currentWeaponConfig.projectilesPerShot;
            float spreadAngle = _currentWeaponConfig.spreadAngle;

            for (int i = 0; i < projectileCount; i++)
            {
                Vector2 direction = CalculateSpreadDirection(fireDirection, i, projectileCount, spreadAngle);
                SpawnProjectile(direction);
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
        /// Spawn a projectile
        /// </summary>
        private void SpawnProjectile(Vector2 direction)
        {
            Projectile projectile = null;

            // Try to get from pool
            if (_projectilePool != null)
            {
                projectile = _projectilePool.Get(_firePoint.position, Quaternion.identity);
            }
            else if (_currentWeaponConfig.projectilePrefab != null)
            {
                // Fallback to instantiate
                var go = Instantiate(_currentWeaponConfig.projectilePrefab, 
                    _firePoint.position, Quaternion.identity);
                projectile = go.GetComponent<Projectile>();
            }

            if (projectile != null)
            {
                projectile.Initialize(
                    direction,
                    _currentWeaponConfig.damage,
                    _currentWeaponConfig.projectileSpeed,
                    _targetLayers
                );

                // Set visual properties
                if (_projectileSprite != null)
                    projectile.SetSprite(_projectileSprite);
                projectile.SetColor(_currentWeaponConfig.projectileColor);
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

            var poolManager = PoolManager.Instance;
            if (poolManager != null)
            {
                string poolId = $"Projectile_{_currentWeaponConfig.weaponName}_{GetInstanceID()}";
                
                if (!poolManager.HasPool(poolId))
                {
                    _projectilePool = poolManager.CreatePool(poolId, projectilePrefab, 20, 100);
                }
                else
                {
                    _projectilePool = poolManager.GetPool<Projectile>(poolId);
                }
            }
        }

        /// <summary>
        /// Spawn muzzle flash effect
        /// </summary>
        private void SpawnMuzzleFlash()
        {
            if (_currentWeaponConfig?.muzzleFlashPrefab != null)
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
            if (_firePoint == null) return;

            // Draw fire direction
            Gizmos.color = Color.red;
            Gizmos.DrawRay(_firePoint.position, _aimDirection * 2f);

            // Draw weapon range
            if (_currentWeaponConfig != null)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
                Gizmos.DrawWireSphere(_firePoint.position, _currentWeaponConfig.range);
            }
        }
#endif
    }
}
