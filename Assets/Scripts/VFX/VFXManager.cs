// ============================================
// VFX SYSTEM - Visual Effects Management
// Handles explosions, particles, and screen effects
// ============================================

using System.Collections.Generic;
using UnityEngine;
using VContainer;
using SpaceCombat.Events;
using SpaceCombat.Interfaces;
using SpaceCombat.Utilities;

namespace SpaceCombat.VFX
{
    /// <summary>
    /// Manages visual effects throughout the game.
    /// Uses object pools for explosion/hit effects to avoid GC spikes.
    /// Prefabs must have PoolableVFX component attached for pooling.
    /// Falls back to Instantiate/Destroy if PoolableVFX is missing.
    /// </summary>
    public class VFXManager : MonoBehaviour
    {
        [Header("Explosion Prefabs (add PoolableVFX component)")]
        [SerializeField] private GameObject _explosionSmall;
        [SerializeField] private GameObject _explosionMedium;
        [SerializeField] private GameObject _explosionLarge;

        [Header("Hit Effects (add PoolableVFX component)")]
        [SerializeField] private GameObject _hitEffectDefault;
        [SerializeField] private GameObject _shieldHitEffect;

        [Header("Projectile Effects")]
        [SerializeField] private GameObject _muzzleFlashDefault;

        [Header("Pool Settings")]
        [SerializeField] private int _explosionPoolSize = 10;
        [SerializeField] private int _hitEffectPoolSize = 20;

        // VFX pools - keyed by prefab instance ID
        private readonly Dictionary<int, ObjectPool<PoolableVFX>> _vfxPools = new();
        private PoolManager _poolManager;

        [Inject]
        public void Construct(PoolManager poolManager)
        {
            _poolManager = poolManager;
        }

        private void Awake()
        {
            SubscribeToEvents();
        }

        private void Start()
        {
            InitializePools();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void InitializePools()
        {
            if (_poolManager == null) return;

            TryCreateVFXPool(_poolManager, _explosionSmall, "VFX_ExplosionSmall", _explosionPoolSize);
            TryCreateVFXPool(_poolManager, _explosionMedium, "VFX_ExplosionMedium", _explosionPoolSize);
            TryCreateVFXPool(_poolManager, _explosionLarge, "VFX_ExplosionLarge", _explosionPoolSize / 2);
            TryCreateVFXPool(_poolManager, _hitEffectDefault, "VFX_HitDefault", _hitEffectPoolSize);
            TryCreateVFXPool(_poolManager, _shieldHitEffect, "VFX_ShieldHit", _hitEffectPoolSize / 2);
        }

        private void TryCreateVFXPool(PoolManager poolManager, GameObject prefab, string poolId, int size)
        {
            if (prefab == null) return;

            var poolableVFX = prefab.GetComponent<PoolableVFX>();
            if (poolableVFX == null) return; // No PoolableVFX = fallback to Instantiate

            if (!poolManager.HasPool(poolId))
            {
                var pool = poolManager.CreatePool(poolId, poolableVFX, size / 2, size);
                _vfxPools[prefab.GetInstanceID()] = pool;
            }
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<ExplosionEvent>(OnExplosion);
            EventBus.Subscribe<DamageEvent>(OnDamage);
            EventBus.Subscribe<EntityDeathEvent>(OnEntityDeath);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<ExplosionEvent>(OnExplosion);
            EventBus.Unsubscribe<DamageEvent>(OnDamage);
            EventBus.Unsubscribe<EntityDeathEvent>(OnEntityDeath);
        }

        private void OnExplosion(ExplosionEvent evt)
        {
            SpawnExplosion(evt.Position, evt.Size);
        }

        private void OnDamage(DamageEvent evt)
        {
            SpawnHitEffect(evt.HitPosition, evt.DamageType);
        }

        private void OnEntityDeath(EntityDeathEvent evt)
        {
            var size = evt.IsPlayer ? ExplosionSize.Large : ExplosionSize.Medium;
            SpawnExplosion(evt.Position, size);
        }

        public void SpawnExplosion(Vector2 position, ExplosionSize size)
        {
            GameObject prefab = size switch
            {
                ExplosionSize.Small => _explosionSmall,
                ExplosionSize.Medium => _explosionMedium,
                ExplosionSize.Large => _explosionLarge,
                _ => _explosionMedium
            };

            SpawnVFX(prefab, position, 2f);
        }

        public void SpawnHitEffect(Vector2 position, DamageType damageType = DamageType.Normal)
        {
            SpawnVFX(_hitEffectDefault, position, 1f);
        }

        public void SpawnMuzzleFlash(Vector2 position, Quaternion rotation, Transform parent = null)
        {
            // Muzzle flash is parented to fire point, pool doesn't work well with reparenting
            if (_muzzleFlashDefault != null)
            {
                var flash = Instantiate(_muzzleFlashDefault, position, rotation, parent);
                Destroy(flash, 0.5f);
            }
        }

        /// <summary>
        /// Spawn a VFX from pool if available, otherwise fallback to Instantiate/Destroy.
        /// </summary>
        private void SpawnVFX(GameObject prefab, Vector2 position, float fallbackDestroyTime)
        {
            if (prefab == null) return;

            int prefabId = prefab.GetInstanceID();
            if (_vfxPools.TryGetValue(prefabId, out var pool))
            {
                Vector3 pos3D = new Vector3(position.x, 0f, position.y);
                pool.Get(pos3D, Quaternion.identity);
                // PoolableVFX auto-deactivates after its duration
            }
            else
            {
                // Fallback: no pool for this prefab
                var instance = Instantiate(prefab, position, Quaternion.identity);
                Destroy(instance, fallbackDestroyTime);
            }
        }
    }
}
