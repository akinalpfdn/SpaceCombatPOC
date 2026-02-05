// ============================================
// AUTO DESTROY PARTICLE
// Auto-destroys particle system when complete
// Attach to particle effect prefabs
// ============================================

using UnityEngine;

namespace StarReapers.VFX
{
    /// <summary>
    /// Auto-destroys particle system when complete.
    /// Attach to particle effect prefabs.
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
}
