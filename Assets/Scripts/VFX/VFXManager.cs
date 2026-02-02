// ============================================
// VFX SYSTEM - Visual Effects Management
// Handles explosions, particles, and screen effects
// ============================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
        public static VFXManager Instance { get; private set; }

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

        [Header("Screen Effects")]
        [SerializeField] private ScreenShake _screenShake;

        // VFX pools - keyed by prefab instance ID
        private readonly Dictionary<int, ObjectPool<PoolableVFX>> _vfxPools = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializePools();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void InitializePools()
        {
            var poolManager = PoolManager.Instance;
            if (poolManager == null) return;

            TryCreateVFXPool(poolManager, _explosionSmall, "VFX_ExplosionSmall", _explosionPoolSize);
            TryCreateVFXPool(poolManager, _explosionMedium, "VFX_ExplosionMedium", _explosionPoolSize);
            TryCreateVFXPool(poolManager, _explosionLarge, "VFX_ExplosionLarge", _explosionPoolSize / 2);
            TryCreateVFXPool(poolManager, _hitEffectDefault, "VFX_HitDefault", _hitEffectPoolSize);
            TryCreateVFXPool(poolManager, _shieldHitEffect, "VFX_ShieldHit", _hitEffectPoolSize / 2);
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

            float shakeIntensity = evt.Size switch
            {
                ExplosionSize.Small => 0.1f,
                ExplosionSize.Medium => 0.2f,
                ExplosionSize.Large => 0.4f,
                _ => 0.1f
            };

            _screenShake?.Shake(shakeIntensity, 0.2f);
        }

        private void OnDamage(DamageEvent evt)
        {
            SpawnHitEffect(evt.HitPosition, evt.DamageType);
        }

        private void OnEntityDeath(EntityDeathEvent evt)
        {
            var size = evt.IsPlayer ? ExplosionSize.Large : ExplosionSize.Medium;
            SpawnExplosion(evt.Position, size);

            if (evt.IsPlayer)
            {
                _screenShake?.Shake(0.5f, 0.5f);
            }
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

    /// <summary>
    /// Camera screen shake effect
    /// </summary>
    public class ScreenShake : MonoBehaviour
    {
        [SerializeField] private float _defaultIntensity = 0.2f;
        [SerializeField] private float _defaultDuration = 0.3f;

        private Vector3 _originalPosition;
        private Coroutine _shakeCoroutine;

        private void Awake()
        {
            _originalPosition = transform.localPosition;
        }

        public void Shake(float intensity = -1f, float duration = -1f)
        {
            if (intensity < 0) intensity = _defaultIntensity;
            if (duration < 0) duration = _defaultDuration;

            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
            }

            _shakeCoroutine = StartCoroutine(ShakeCoroutine(intensity, duration));
        }

        private IEnumerator ShakeCoroutine(float intensity, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float currentIntensity = intensity * (1f - (elapsed / duration));
                
                Vector2 offset = Random.insideUnitCircle * currentIntensity;
                transform.localPosition = _originalPosition + new Vector3(offset.x, offset.y, 0);

                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localPosition = _originalPosition;
            _shakeCoroutine = null;
        }
    }

    /// <summary>
    /// Auto-destroys particle system when complete
    /// Attach to particle effect prefabs
    /// </summary>
    public class AutoDestroyParticle : MonoBehaviour
    {
        private ParticleSystem _particleSystem;

        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
        }

        private void Update()
        {
            if (_particleSystem != null && !_particleSystem.IsAlive())
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// Poolable visual effect for reuse
    /// </summary>
    public class PoolableEffect : MonoBehaviour, IVisualEffect
    {
        [SerializeField] private ParticleSystem _particleSystem;
        [SerializeField] private float _duration = 1f;

        public bool IsActive => gameObject.activeInHierarchy;

        private float _timer;

        private void Update()
        {
            if (IsActive)
            {
                _timer -= Time.deltaTime;
                if (_timer <= 0)
                {
                    gameObject.SetActive(false);
                }
            }
        }

        public void Play(Vector2 position, float scale = 1f)
        {
            transform.position = position;
            transform.localScale = Vector3.one * scale;
            _timer = _duration;

            if (_particleSystem != null)
            {
                _particleSystem.Play();
            }
        }

        public void Stop()
        {
            if (_particleSystem != null)
            {
                _particleSystem.Stop();
            }
        }

        public void OnSpawn()
        {
            _timer = _duration;
        }

        public void OnDespawn()
        {
            Stop();
        }

        public void ResetState()
        {
            _timer = 0;
            transform.localScale = Vector3.one;
        }
    }
}
