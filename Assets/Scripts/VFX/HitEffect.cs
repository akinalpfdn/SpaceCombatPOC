using UnityEngine;

namespace SpaceCombat.VFX
{
    public class HitEffect : MonoBehaviour
    {
        [SerializeField] private float _totalDuration = 2.0f;

        private void Start()
        {
            Destroy(gameObject, _totalDuration);
        }

        // The main spawn method (Requires 3 arguments)
        public static void Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab != null)
            {
                Instantiate(prefab, position, rotation);
            }
        }

        // --- NEW HELPER METHOD ---
        // This fixes your error by adding a default rotation automatically
        public static void Spawn(GameObject prefab, Vector3 position)
        {
            Spawn(prefab, position, Quaternion.identity);
        }
    }
}