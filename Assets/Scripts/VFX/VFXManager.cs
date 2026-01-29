// ============================================
// VFX SYSTEM - Visual Effects Management
// Handles explosions, particles, and screen effects
// ============================================

using System.Collections;
using UnityEngine;
using SpaceCombat.Events;
using SpaceCombat.Interfaces;

namespace SpaceCombat.VFX
{
    /// <summary>
    /// Manages visual effects throughout the game
    /// Subscribes to events and spawns appropriate effects
    /// </summary>
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [Header("Explosion Prefabs")]
        [SerializeField] private GameObject _explosionSmall;
        [SerializeField] private GameObject _explosionMedium;
        [SerializeField] private GameObject _explosionLarge;

        [Header("Hit Effects")]
        [SerializeField] private GameObject _hitEffectDefault;
        [SerializeField] private GameObject _shieldHitEffect;

        [Header("Projectile Effects")]
        [SerializeField] private GameObject _muzzleFlashDefault;

        [Header("Screen Effects")]
        [SerializeField] private ScreenShake _screenShake;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
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
            
            // Screen shake based on size
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
            // Spawn hit effect
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

            if (prefab != null)
            {
                var explosion = Instantiate(prefab, position, Quaternion.identity);
                Destroy(explosion, 2f);
            }
        }

        public void SpawnHitEffect(Vector2 position, DamageType damageType = DamageType.Normal)
        {
            GameObject prefab = _hitEffectDefault;
            
            if (prefab != null)
            {
                var effect = Instantiate(prefab, position, Quaternion.identity);
                Destroy(effect, 1f);
            }
        }

        public void SpawnMuzzleFlash(Vector2 position, Quaternion rotation, Transform parent = null)
        {
            if (_muzzleFlashDefault != null)
            {
                var flash = Instantiate(_muzzleFlashDefault, position, rotation, parent);
                Destroy(flash, 0.5f);
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
