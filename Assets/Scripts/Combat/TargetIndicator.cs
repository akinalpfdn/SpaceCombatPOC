// ============================================
// TARGET INDICATOR - Shows circular reticle around selected target
// Uses your sprite - just assign it in inspector
// ============================================

using UnityEngine;

namespace SpaceCombat.Combat
{
    public class TargetIndicator : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Color _color = Color.red;

        [Header("Animation")]
        [SerializeField] private bool _pulse = true;
        [SerializeField] private float _pulseSpeed = 2f;
        [SerializeField] private float _pulseMin = 0.9f;
        [SerializeField] private float _pulseMax = 1.1f;

        private Transform _target;
        private float _pulseTime;

        private void Start()
        {
            // Get or add SpriteRenderer
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _color;
            }
        }

        private void Update()
        {
            // Pulse effect
            if (_pulse)
            {
                _pulseTime += Time.deltaTime * _pulseSpeed;
                float scale = Mathf.Lerp(_pulseMin, _pulseMax, (Mathf.Sin(_pulseTime) + 1f) / 2f);
                transform.localScale = Vector3.one * scale;
            }

            // Follow target
            if (_target != null)
            {
                transform.position = _target.position;
            }
        }

        public void SetTarget(Transform target)
        {
            _target = target;
            gameObject.SetActive(target != null);
        }

        public void SetColor(Color color)
        {
            _color = color;
            if (_spriteRenderer != null)
                _spriteRenderer.color = color;
        }
    }
}
