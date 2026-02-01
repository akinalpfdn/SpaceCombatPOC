// ============================================
// STAR FIELD - Dynamic star background
// Creates twinkling star field effect
// ============================================

using UnityEngine;

namespace SpaceCombat.Environment
{
    /// <summary>
    /// Spawns and manages background stars
    /// Creates dynamic star field effect with twinkling
    /// 3D Version - Stars positioned on XZ plane
    /// </summary>
    public class StarField : MonoBehaviour
    {
        [Header("Star Settings")]
        [SerializeField] private int _starCount = 200;
        [SerializeField] private float _fieldRadius = 50f;
        [SerializeField] private float _minStarSize = 0.02f;
        [SerializeField] private float _maxStarSize = 0.08f;
        [SerializeField] private Color _starColor = Color.white;
        [SerializeField] private bool _twinkle = true;
        [SerializeField] private float _twinkleSpeed = 2f;

        [Header("References")]
        [SerializeField] private GameObject _starPrefab;
        [SerializeField] private Transform _cameraTransform;

        private Transform[] _stars;
        private SpriteRenderer[] _starRenderers;
        private float[] _twinkleOffsets;

        private void Start()
        {
            if (_cameraTransform == null)
            {
                _cameraTransform = Camera.main?.transform;
            }

            GenerateStars();
        }

        private void Update()
        {
            if (_twinkle)
            {
                UpdateTwinkle();
            }

            RepositionStars();
        }

        private void GenerateStars()
        {
            _stars = new Transform[_starCount];
            _starRenderers = new SpriteRenderer[_starCount];
            _twinkleOffsets = new float[_starCount];

            for (int i = 0; i < _starCount; i++)
            {
                GameObject star;

                if (_starPrefab != null)
                {
                    star = Instantiate(_starPrefab, transform);
                }
                else
                {
                    // Create simple star sprite
                    star = new GameObject($"Star_{i}");
                    star.transform.SetParent(transform);
                    var sr = star.AddComponent<SpriteRenderer>();
                    sr.sprite = CreateStarSprite();
                    sr.color = _starColor;
                    _starRenderers[i] = sr;
                }

                // Random position on XZ plane
                Vector2 randomPos = Random.insideUnitCircle * _fieldRadius;
                star.transform.position = new Vector3(randomPos.x, 0, randomPos.y);  // 3D: XZ plane

                // Random size
                float size = Random.Range(_minStarSize, _maxStarSize);
                star.transform.localScale = Vector3.one * size;

                // Random twinkle offset
                _twinkleOffsets[i] = Random.Range(0f, Mathf.PI * 2f);

                _stars[i] = star.transform;

                if (_starRenderers[i] == null)
                {
                    _starRenderers[i] = star.GetComponent<SpriteRenderer>();
                }
            }
        }

        private void UpdateTwinkle()
        {
            float time = Time.time * _twinkleSpeed;

            for (int i = 0; i < _starCount; i++)
            {
                if (_starRenderers[i] != null)
                {
                    float twinkle = (Mathf.Sin(time + _twinkleOffsets[i]) + 1f) * 0.5f;
                    float alpha = Mathf.Lerp(0.3f, 1f, twinkle);

                    var color = _starColor;
                    color.a = alpha;
                    _starRenderers[i].color = color;
                }
            }
        }

        private void RepositionStars()
        {
            if (_cameraTransform == null) return;

            // 3D XZ plane: Use X and Z coordinates
            Vector3 cameraPos = _cameraTransform.position;

            for (int i = 0; i < _starCount; i++)
            {
                if (_stars[i] == null) continue;

                Vector3 starPos = _stars[i].position;
                Vector3 delta = starPos - cameraPos;

                // Wrap horizontally (X axis)
                if (delta.x > _fieldRadius)
                    starPos.x -= _fieldRadius * 2f;
                else if (delta.x < -_fieldRadius)
                    starPos.x += _fieldRadius * 2f;

                // Wrap depth-wise (Z axis in 3D)
                if (delta.z > _fieldRadius)
                    starPos.z -= _fieldRadius * 2f;
                else if (delta.z < -_fieldRadius)
                    starPos.z += _fieldRadius * 2f;

                _stars[i].position = starPos;
            }
        }

        private Sprite CreateStarSprite()
        {
            // Create a simple white circle texture for stars with soft edges
            int size = 8;
            Texture2D texture = new Texture2D(size, size);

            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(1f - (dist / radius));
                    texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }

            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
