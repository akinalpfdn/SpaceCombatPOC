// ============================================
// POOLABLE EFFECT
// Poolable visual effect for reuse
// ============================================

using UnityEngine;
using SpaceCombat.Interfaces;

namespace SpaceCombat.VFX
{
    /// <summary>
    /// Poolable visual effect for reuse.
    /// Implements IVisualEffect for pool compatibility.
    /// </summary>
    public class PoolableEffect : MonoBehaviour, IVisualEffect
    {
        [Header("Configuration")]
        [SerializeField] private ParticleSystem _particleSystem;
        [SerializeField] private float _duration = 1f;

        // Properties
        public bool IsActive => gameObject.activeInHierarchy;

        // Runtime state
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
