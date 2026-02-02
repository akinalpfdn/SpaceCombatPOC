// ============================================
// MESH PROJECTILE VISUAL - Strategy Pattern Implementation
// Renders projectile as 3D mesh with emission glow + trail
// DarkOrbit-style energy bolt visual
// ============================================

using UnityEngine;
using SpaceCombat.Interfaces;
using SpaceCombat.ScriptableObjects;

namespace SpaceCombat.VFX
{
    /// <summary>
    /// Mesh-based projectile visual using MeshRenderer + TrailRenderer.
    /// Creates DarkOrbit-style energy bolts with emission glow and trailing effect.
    ///
    /// Design Patterns:
    /// - Strategy: Implements IProjectileVisual for visual abstraction
    ///
    /// SOLID Principles:
    /// - Single Responsibility: Only handles mesh/trail rendering
    /// - Open/Closed: New visual styles via new IProjectileVisual implementations
    /// - Dependency Inversion: Projectile depends on IProjectileVisual abstraction
    ///
    /// Performance Notes:
    /// - Uses MaterialPropertyBlock to avoid material instancing (SRP Batcher friendly)
    /// - No per-frame allocations
    /// - Trail Renderer is lightweight on GPU
    /// </summary>
    public class MeshProjectileVisual : MonoBehaviour, IProjectileVisual
    {
        // ============================================
        // SERIALIZED FIELDS
        // ============================================

        [Header("Configuration")]
        [SerializeField] private ProjectileVisualConfig _config;

        [Header("Components")]
        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private TrailRenderer _trailRenderer;

        // ============================================
        // RUNTIME STATE
        // ============================================

        private MaterialPropertyBlock _propertyBlock;
        private Color _currentColor;
        private float _currentEmissionIntensity;
        private Vector3 _baseScale;
        private bool _isInitialized;

        // Deferred trail activation: waits 2 frames so FixedUpdate applies velocity
        // before trail starts recording. Prevents "ghost trail" artifact on spawn.
        private int _trailActivationFrame;

        private static readonly int BASE_COLOR_ID = Shader.PropertyToID("_BaseColor");

        // ============================================
        // UNITY LIFECYCLE
        // ============================================

        private void Awake()
        {
            CacheComponents();
            _propertyBlock = new MaterialPropertyBlock();

            if (_config != null)
            {
                ApplyConfig(_config);
            }
        }

        /// <summary>
        /// Deferred trail activation - waits 2 frames so FixedUpdate moves the
        /// projectile to its correct position before trail starts recording.
        /// Frame 0: Spawn + velocity set (projectile still at fire point)
        /// Frame 1: FixedUpdate moves projectile forward
        /// Frame 2: Trail starts emitting from correct moving position
        /// </summary>
        private void LateUpdate()
        {
            if (_trailActivationFrame > 0 && _trailRenderer != null
                && Time.frameCount >= _trailActivationFrame)
            {
                _trailActivationFrame = 0;
                _trailRenderer.Clear();
                _trailRenderer.emitting = true;
            }
        }

        // ============================================
        // IProjectileVisual IMPLEMENTATION
        // ============================================

        /// <summary>
        /// Apply HDR color to mesh body and trail.
        /// Color is multiplied by emission intensity for bloom glow.
        /// </summary>
        public void SetColor(Color color, float emissionIntensity)
        {
            _currentColor = color;
            _currentEmissionIntensity = emissionIntensity;

            // HDR color for bloom: multiply base color by intensity
            Color hdrColor = color * emissionIntensity;
            hdrColor.a = 1f;

            // Apply to mesh via MaterialPropertyBlock (no material instancing)
            _meshRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BASE_COLOR_ID, hdrColor);
            _meshRenderer.SetPropertyBlock(_propertyBlock);

            // Apply to trail with alpha fade
            if (_trailRenderer != null)
            {
                Color trailStart = hdrColor;
                trailStart.a = 0.8f;
                Color trailEnd = hdrColor;
                trailEnd.a = 0f;

                _trailRenderer.startColor = trailStart;
                _trailRenderer.endColor = trailEnd;
            }
        }

        /// <summary>
        /// Apply uniform scale multiplier to the mesh visual.
        /// </summary>
        public void SetScale(float scale)
        {
            if (_baseScale == Vector3.zero)
                _baseScale = _config != null ? _config.MeshScale : Vector3.one;

            transform.localScale = _baseScale * scale;
        }

        /// <summary>
        /// Called when projectile is spawned from pool.
        /// Enables visuals and clears trail to prevent artifacts.
        /// </summary>
        public void OnSpawn()
        {
            _meshRenderer.enabled = true;

            if (_trailRenderer != null)
            {
                _trailRenderer.Clear();
                _trailRenderer.enabled = true;
                _trailRenderer.emitting = false;
                // Wait 2 frames: next FixedUpdate moves projectile, then trail starts
                _trailActivationFrame = Time.frameCount + 2;
            }
        }

        /// <summary>
        /// Called when projectile is returned to pool.
        /// Disables visuals to prevent ghost rendering.
        /// </summary>
        public void OnDespawn()
        {
            _meshRenderer.enabled = false;
            _trailActivationFrame = 0;

            if (_trailRenderer != null)
            {
                _trailRenderer.emitting = false;
                _trailRenderer.enabled = false;
            }
        }

        /// <summary>
        /// Full state reset for pool reuse.
        /// </summary>
        public void ResetVisual()
        {
            if (_trailRenderer != null)
            {
                _trailRenderer.Clear();
            }

            // Reset to config defaults if available
            if (_config != null)
            {
                SetColor(_config.EmissionColor, _config.EmissionIntensity);
                _baseScale = _config.MeshScale;
                transform.localScale = _baseScale;
            }
        }

        // ============================================
        // CONFIGURATION
        // ============================================

        /// <summary>
        /// Apply a visual config asset. Can be called at runtime to swap visual style.
        /// </summary>
        public void ApplyConfig(ProjectileVisualConfig config)
        {
            if (config == null) return;

            _config = config;
            _isInitialized = true;

            // Setup mesh
            if (_meshFilter != null && config.BodyMesh != null)
            {
                _meshFilter.sharedMesh = config.BodyMesh;
            }

            // Setup mesh material
            if (_meshRenderer != null && config.BodyMaterial != null)
            {
                _meshRenderer.sharedMaterial = config.BodyMaterial;
                _meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _meshRenderer.receiveShadows = false;
            }

            // Setup trail
            if (_trailRenderer != null)
            {
                if (config.TrailMaterial != null)
                {
                    _trailRenderer.sharedMaterial = config.TrailMaterial;
                }
                _trailRenderer.startWidth = config.TrailStartWidth;
                _trailRenderer.endWidth = config.TrailEndWidth;
                _trailRenderer.time = config.TrailTime;
                _trailRenderer.numCornerVertices = config.TrailCornerVertices;
                _trailRenderer.minVertexDistance = config.TrailMinVertexDistance;
            }

            // Apply default visual state
            _baseScale = config.MeshScale;
            transform.localScale = _baseScale;
            SetColor(config.EmissionColor, config.EmissionIntensity);
        }

        // ============================================
        // PRIVATE METHODS
        // ============================================

        private void CacheComponents()
        {
            if (_meshFilter == null)
                _meshFilter = GetComponent<MeshFilter>();
            if (_meshRenderer == null)
                _meshRenderer = GetComponent<MeshRenderer>();
            if (_trailRenderer == null)
                _trailRenderer = GetComponent<TrailRenderer>();
        }
    }
}
