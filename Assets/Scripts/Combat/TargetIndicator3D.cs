// ============================================
// TARGET INDICATOR 3D - DarkOrbit-style targeting reticle
// Two segmented rings rotating in opposite directions
// ============================================

using UnityEngine;
using StarReapers.Interfaces;

namespace StarReapers.Combat
{
    /// <summary>
    /// DarkOrbit-style 3D target indicator with:
    /// - Two segmented rings (inner and outer)
    /// - Opposite rotation directions
    /// - Emission glow effect
    /// - Pulse animation
    /// </summary>
    public class TargetIndicator3D : MonoBehaviour, ITargetIndicator
    {
        // ============================================
        // CONFIGURATION
        // ============================================

        [Header("Ring Settings")]
        [SerializeField] private float _innerRadius = 1.2f;
        [SerializeField] private float _outerRadius = 1.5f;
        [SerializeField] private float _ringThickness = 0.06f;
        [SerializeField] private int _segments = 8;
        [SerializeField] private float _segmentGapRatio = 0.3f; // 0-1, gap between segments

        [Header("Color")]
        [SerializeField] private Color _color = new Color(1f, 0.2f, 0.2f, 1f);
        [SerializeField] private float _emissionIntensity = 4f;

        [Header("Rotation")]
        [SerializeField] private float _innerRotationSpeed = 60f;
        [SerializeField] private float _outerRotationSpeed = -40f; // Negative = opposite direction

        [Header("Pulse Animation")]
        [SerializeField] private bool _enablePulse = true;
        [SerializeField] private float _pulseSpeed = 2f;
        [SerializeField] private float _pulseAmount = 0.1f;

        // ============================================
        // RUNTIME STATE
        // ============================================

        private Transform _target;
        private float _baseScale = 1f;
        private float _pulseTime;

        private Transform _innerRing;
        private Transform _outerRing;
        private MeshRenderer[] _innerRenderers;
        private MeshRenderer[] _outerRenderers;
        private MaterialPropertyBlock _propertyBlock;
        private Material _sharedMaterial;

        private static readonly int BASE_COLOR_ID = Shader.PropertyToID("_BaseColor");
        private static readonly int EMISSION_COLOR_ID = Shader.PropertyToID("_EmissionColor");

        // ============================================
        // UNITY LIFECYCLE
        // ============================================

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            CreateIndicator();
        }

        private void Update()
        {
            // Follow target
            if (_target != null)
            {
                transform.position = _target.position;
            }

            // Rotate rings in opposite directions
            if (_innerRing != null)
                _innerRing.Rotate(Vector3.up, _innerRotationSpeed * Time.deltaTime);

            if (_outerRing != null)
                _outerRing.Rotate(Vector3.up, _outerRotationSpeed * Time.deltaTime);

            // Pulse animation
            if (_enablePulse)
            {
                _pulseTime += Time.deltaTime * _pulseSpeed;
                float pulse = 1f + Mathf.Sin(_pulseTime) * _pulseAmount;
                transform.localScale = Vector3.one * (_baseScale * pulse);
            }
        }

        private void OnDestroy()
        {
            if (_sharedMaterial != null)
                Destroy(_sharedMaterial);
        }

        // ============================================
        // PUBLIC METHODS
        // ============================================

        public void SetTarget(Transform target)
        {
            _target = target;
            gameObject.SetActive(target != null);
        }

        public void SetColor(Color color)
        {
            _color = color;
            ApplyColor();
        }

        public void SetBaseScale(float scale)
        {
            _baseScale = scale;
            transform.localScale = Vector3.one * scale;
        }

        // ============================================
        // INDICATOR CREATION
        // ============================================

        private void CreateIndicator()
        {
            // Create shared material (Unlit with emission)
            _sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            _sharedMaterial.EnableKeyword("_EMISSION");

            // Create inner ring (rotates clockwise)
            _innerRing = new GameObject("InnerRing").transform;
            _innerRing.SetParent(transform);
            _innerRing.localPosition = Vector3.zero;
            _innerRing.localRotation = Quaternion.identity;
            _innerRenderers = CreateSegmentedRing(_innerRing, _innerRadius, _ringThickness);

            // Create outer ring (rotates counter-clockwise)
            _outerRing = new GameObject("OuterRing").transform;
            _outerRing.SetParent(transform);
            _outerRing.localPosition = Vector3.zero;
            _outerRing.localRotation = Quaternion.Euler(0, 360f / _segments / 2f, 0); // Offset for visual interest
            _outerRenderers = CreateSegmentedRing(_outerRing, _outerRadius, _ringThickness);

            ApplyColor();
        }

        private MeshRenderer[] CreateSegmentedRing(Transform parent, float radius, float thickness)
        {
            var renderers = new MeshRenderer[_segments];
            float segmentAngle = 360f / _segments;
            float gapAngle = segmentAngle * _segmentGapRatio;
            float arcAngle = segmentAngle - gapAngle;

            for (int i = 0; i < _segments; i++)
            {
                float startAngle = i * segmentAngle;

                var segment = new GameObject($"Segment_{i}");
                segment.transform.SetParent(parent);
                segment.transform.localPosition = Vector3.zero;
                segment.transform.localRotation = Quaternion.identity;

                var meshFilter = segment.AddComponent<MeshFilter>();
                var meshRenderer = segment.AddComponent<MeshRenderer>();

                meshFilter.sharedMesh = CreateArcMesh(radius, thickness, startAngle, arcAngle);
                meshRenderer.sharedMaterial = _sharedMaterial;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;

                renderers[i] = meshRenderer;
            }

            return renderers;
        }

        private Mesh CreateArcMesh(float radius, float thickness, float startAngle, float arcAngle)
        {
            var mesh = new Mesh();
            mesh.name = "ArcSegment";

            int arcSegments = Mathf.Max(4, Mathf.CeilToInt(arcAngle / 10f)); // More segments for smoother arc
            int vertCount = (arcSegments + 1) * 2;

            Vector3[] vertices = new Vector3[vertCount];
            int[] triangles = new int[arcSegments * 6];
            Vector2[] uvs = new Vector2[vertCount];
            Vector3[] normals = new Vector3[vertCount];

            float innerR = radius - thickness / 2f;
            float outerR = radius + thickness / 2f;

            for (int i = 0; i <= arcSegments; i++)
            {
                float t = (float)i / arcSegments;
                float angle = (startAngle + arcAngle * t) * Mathf.Deg2Rad;

                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                // Inner vertex
                vertices[i * 2] = new Vector3(cos * innerR, 0, sin * innerR);
                // Outer vertex
                vertices[i * 2 + 1] = new Vector3(cos * outerR, 0, sin * outerR);

                uvs[i * 2] = new Vector2(t, 0);
                uvs[i * 2 + 1] = new Vector2(t, 1);

                normals[i * 2] = Vector3.up;
                normals[i * 2 + 1] = Vector3.up;
            }

            // Create triangles
            for (int i = 0; i < arcSegments; i++)
            {
                int baseIdx = i * 6;
                int vertIdx = i * 2;

                // First triangle
                triangles[baseIdx] = vertIdx;
                triangles[baseIdx + 1] = vertIdx + 2;
                triangles[baseIdx + 2] = vertIdx + 1;

                // Second triangle
                triangles[baseIdx + 3] = vertIdx + 1;
                triangles[baseIdx + 4] = vertIdx + 2;
                triangles[baseIdx + 5] = vertIdx + 3;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.RecalculateBounds();

            return mesh;
        }

        private void ApplyColor()
        {
            Color hdrColor = _color * _emissionIntensity;
            hdrColor.a = 1f;

            ApplyColorToRenderers(_innerRenderers, hdrColor);
            ApplyColorToRenderers(_outerRenderers, hdrColor);
        }

        private void ApplyColorToRenderers(MeshRenderer[] renderers, Color hdrColor)
        {
            if (renderers == null) return;

            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;

                renderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(BASE_COLOR_ID, hdrColor);
                _propertyBlock.SetColor(EMISSION_COLOR_ID, hdrColor);
                renderer.SetPropertyBlock(_propertyBlock);
            }
        }
    }
}
