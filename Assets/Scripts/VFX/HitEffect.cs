// ============================================
// HIT EFFECT - Simple explosion on impact
// Creates a burst effect when projectiles hit
// ============================================

using UnityEngine;

namespace SpaceCombat.VFX
{
    /// <summary>
    /// Simple hit explosion effect
    /// Auto-plays and destroys itself
    /// </summary>
    public class HitEffect : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _duration = 0.3f;
        [SerializeField] private float _maxScale = 1.5f;
        [SerializeField] private Color _color = Color.red;

        [Header("Components (auto-found if not set)")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private ParticleSystem _particles;

        private float _spawnTime;
        private Vector3 _startScale;

        private void Start()
        {
            _spawnTime = Time.time;
            _startScale = transform.localScale;

            // Auto-setup if no components assigned
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (_particles == null)
            {
                _particles = GetComponent<ParticleSystem>();
            }

            // Create visual if nothing exists
            if (_spriteRenderer == null && _particles == null)
            {
                CreateSimpleEffect();
            }

            // Set color
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _color;
            }

            // Start particles
            if (_particles != null)
            {
                _particles.Play();
            }
        }

        private void Update()
        {
            float elapsed = Time.time - _spawnTime;
            float progress = elapsed / _duration;

            if (progress >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            // Expand and fade
            if (_spriteRenderer != null)
            {
                float scale = Mathf.Lerp(0.5f, _maxScale, progress);
                transform.localScale = _startScale * scale;

                Color c = _color;
                c.a = 1f - progress;
                _spriteRenderer.color = c;
            }
        }

        private void CreateSimpleEffect()
        {
            // Create a simple sprite renderer
            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            _spriteRenderer.sprite = CreateExplosionSprite();
            _spriteRenderer.color = _color;
            _spriteRenderer.sortingOrder = 100; // Draw on top
        }

        private Sprite CreateExplosionSprite()
        {
            // Create a simple explosion/flash texture
            int size = 64;
            Texture2D texture = new Texture2D(size, size);

            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(1f - (dist / radius));

                    // Create radial gradient
                    float brightness = Mathf.Lerp(1f, 0f, dist / radius);
                    texture.SetPixel(x, y, new Color(brightness, brightness * 0.5f, 0f, alpha));
                }
            }

            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>
        /// Spawn a hit effect at position
        /// </summary>
        public static void Spawn(Vector3 position, Color color)
        {
            GameObject go = new GameObject("HitEffect");
            go.transform.position = position;

            var effect = go.AddComponent<HitEffect>();
            effect._color = color;

            // Auto-destroy after duration
            Destroy(go, 0.5f);
        }
    }
}
