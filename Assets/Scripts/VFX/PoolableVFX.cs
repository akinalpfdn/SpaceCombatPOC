using UnityEngine;
using SpaceCombat.Interfaces;

namespace SpaceCombat.VFX
{
    /// <summary>
    /// Lightweight poolable VFX component.
    /// Add this to explosion/hit effect prefabs to enable pooling.
    /// Auto-deactivates after duration, ready for pool return.
    /// </summary>
    public class PoolableVFX : MonoBehaviour, IPoolable
    {
        [SerializeField] private float _duration = 2f;

        private float _timer;
        private ParticleSystem[] _particleSystems;

        public bool IsActive => gameObject.activeInHierarchy;

        private void Awake()
        {
            _particleSystems = GetComponentsInChildren<ParticleSystem>();
        }

        private void OnEnable()
        {
            _timer = _duration;
            PlayParticles();
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                gameObject.SetActive(false);
            }
        }

        private void PlayParticles()
        {
            foreach (var ps in _particleSystems)
            {
                ps.Play();
            }
        }

        private void StopParticles()
        {
            foreach (var ps in _particleSystems)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        public void OnSpawn()
        {
            _timer = _duration;
        }

        public void OnDespawn()
        {
            StopParticles();
        }

        public void ResetState()
        {
            _timer = 0f;
        }
    }
}
