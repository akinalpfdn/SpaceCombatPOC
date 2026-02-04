// ============================================
// MuzzleFlash.cs
// Laser/weapon muzzle flash visual effect
// Multi-layer star burst with rotation animation
// ============================================

using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceCombat.VFX
{
    /// <summary>
    /// Professional muzzle flash effect for weapons.
    /// Creates a multi-layer star burst with:
    /// - Outer rotating star shape
    /// - Inner bright glow core
    /// - Smooth fade and scale animation
    /// Auto-destroys after duration.
    ///
    /// Design Pattern: Component-based VFX
    /// - Procedural mesh generation for star shape
    /// - Additive blending for bright glow effect
    /// - Billboard to always face camera
    /// </summary>
    public class MuzzleFlash : MonoBehaviour
    {
        // ============================================
        // CONFIGURATION
        // ============================================

        [Header("Flash Settings")]
        [Tooltip("Duration of the flash effect")]
        [SerializeField] private float _duration = 0.12f;

        [Tooltip("Starting scale of the outer flash")]
        [SerializeField] private float _startScale = 1.0f;

        [Tooltip("End scale (shrinks as it fades)")]
        [SerializeField] private float _endScale = 0.3f;

        [Tooltip("Flash color (will be set by weapon)")]
        [SerializeField] private Color _flashColor = Color.white;

        [Tooltip("Emission intensity multiplier")]
        [SerializeField] private float _emissionIntensity = 8f;

        [Header("Star Shape")]
        [Tooltip("Number of points on the star")]
        [SerializeField] private int _starPoints = 4;

        [Tooltip("Inner radius ratio (0-1, smaller = sharper points)")]
        [Range(0.1f, 0.8f)]
        [SerializeField] private float _innerRadiusRatio = 0.3f;

        [Header("Rotation")]
        [Tooltip("Rotation speed in degrees per second")]
        [SerializeField] private float _rotationSpeed = 720f;

        [Header("Inner Glow")]
        [Tooltip("Enable inner glow layer")]
        [SerializeField] private bool _enableInnerGlow = true;

        [Tooltip("Inner glow scale relative to outer")]
        [Range(0.2f, 0.8f)]
        [SerializeField] private float _innerGlowScale = 0.5f;

        [Tooltip("Inner glow intensity multiplier")]
        [Range(1f, 3f)]
        [SerializeField] private float _innerGlowIntensity = 2f;

        [Header("Spark Particles")]
        [Tooltip("Enable spark burst particles")]
        [SerializeField] private bool _enableSparks = true;

        [Tooltip("Number of spark particles")]
        [Range(3, 20)]
        [SerializeField] private int _sparkCount = 8;

        [Tooltip("Spark speed")]
        [Range(1f, 10f)]
        [SerializeField] private float _sparkSpeed = 4f;

        [Tooltip("Spark size")]
        [Range(0.02f, 0.2f)]
        [SerializeField] private float _sparkSize = 0.08f;

        [Header("Components")]
        [Tooltip("Optional externally assigned particle system")]
        [SerializeField] private ParticleSystem _particleSystem;

        // ============================================
        // RUNTIME STATE
        // ============================================

        private float _elapsedTime;
        private Material _outerMaterial;
        private Material _innerMaterial;
        private Transform _outerTransform;
        private Transform _innerTransform;
        private bool _isInitialized;
        private float _currentRotation;

        // Material property IDs for performance
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        // ============================================
        // UNITY LIFECYCLE
        // ============================================

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized) return;

            // Create outer star layer
            CreateStarLayer("OuterStar", 1f, out _outerTransform, out _outerMaterial);

            // Create inner glow layer (smaller, brighter)
            if (_enableInnerGlow)
            {
                CreateStarLayer("InnerGlow", _innerGlowScale, out _innerTransform, out _innerMaterial);
            }

            // Create spark particle system if enabled and not externally assigned
            if (_enableSparks && _particleSystem == null)
            {
                CreateSparkParticles();
            }

            // Setup particle system color if present
            if (_particleSystem != null)
            {
                var main = _particleSystem.main;
                main.startColor = _flashColor;
                _particleSystem.Play();
            }

            // Initial billboard facing
            UpdateBillboard();

            _isInitialized = true;
        }

        private void Update()
        {
            _elapsedTime += Time.deltaTime;

            if (_elapsedTime >= _duration)
            {
                Destroy(gameObject);
                return;
            }

            // Animate flash
            float normalizedTime = _elapsedTime / _duration;
            AnimateFlash(normalizedTime);
        }

        private void OnDestroy()
        {
            // Clean up material instances
            if (_outerMaterial != null)
            {
                Destroy(_outerMaterial);
            }
            if (_innerMaterial != null)
            {
                Destroy(_innerMaterial);
            }
        }

        // ============================================
        // ANIMATION
        // ============================================

        private void AnimateFlash(float t)
        {
            // Billboard - always face camera
            UpdateBillboard();

            // Ease-out curve for snappy start, smooth end
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            // Scale animation
            float scale = Mathf.Lerp(_startScale, _endScale, easedT);

            // Rotation animation (outer star rotates)
            _currentRotation += _rotationSpeed * Time.deltaTime;

            // Alpha/Intensity fade
            float alpha = 1f - easedT;

            // Animate outer layer
            if (_outerTransform != null)
            {
                _outerTransform.localScale = Vector3.one * scale;
                _outerTransform.localRotation = Quaternion.Euler(0, 0, _currentRotation);

                UpdateMaterialColor(_outerMaterial, alpha, 1f);
            }

            // Animate inner layer (no rotation, different fade curve)
            if (_innerTransform != null)
            {
                // Inner layer shrinks slower, stays bright longer
                float innerEasedT = 1f - Mathf.Pow(1f - t, 2f);
                float innerScale = Mathf.Lerp(_startScale * _innerGlowScale, _endScale * _innerGlowScale * 0.5f, innerEasedT);
                _innerTransform.localScale = Vector3.one * innerScale;

                // Inner glow stays bright longer, then fades quickly at end
                float innerAlpha = t < 0.7f ? 1f : 1f - ((t - 0.7f) / 0.3f);
                UpdateMaterialColor(_innerMaterial, innerAlpha, _innerGlowIntensity);
            }
        }

        private void UpdateBillboard()
        {
            if (Camera.main == null) return;

            // Face camera
            Quaternion cameraRotation = Camera.main.transform.rotation;

            if (_outerTransform != null)
            {
                // Keep local rotation for spinning, but billboard the parent
                transform.rotation = cameraRotation;
            }
        }

        private void UpdateMaterialColor(Material material, float alpha, float intensityMultiplier)
        {
            if (material == null) return;

            // HDR color with intensity that fades
            Color hdrColor = _flashColor * _emissionIntensity * intensityMultiplier * alpha;
            hdrColor.a = alpha;
            material.SetColor(BaseColorId, hdrColor);

            // Emission also fades
            if (material.HasProperty(EmissionColorId))
            {
                material.SetColor(EmissionColorId, hdrColor);
            }
        }

        // ============================================
        // LAYER CREATION
        // ============================================

        /// <summary>
        /// Creates a procedural particle system for spark burst effect.
        /// Particles emit once in a burst pattern radiating outward.
        /// </summary>
        private void CreateSparkParticles()
        {
            var sparkObj = new GameObject("SparkParticles");
            sparkObj.transform.SetParent(transform);
            sparkObj.transform.localPosition = Vector3.zero;
            sparkObj.transform.localRotation = Quaternion.identity;

            _particleSystem = sparkObj.AddComponent<ParticleSystem>();

            // Stop the particle system before configuring (Unity auto-plays on AddComponent)
            _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // Main module - configure lifetime and simulation
            var main = _particleSystem.main;
            main.duration = _duration;
            main.loop = false;
            main.startLifetime = _duration * 0.8f;
            main.startSpeed = _sparkSpeed;
            main.startSize = _sparkSize;
            main.startColor = _flashColor;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = _sparkCount;

            // Emission - single burst at start
            var emission = _particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, _sparkCount)
            });

            // Shape - emit from center point in all directions (cone for 2D spread)
            var shape = _particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 180f; // Full hemisphere
            shape.radius = 0.01f; // Emit from center point
            shape.radiusThickness = 0f;

            // Size over lifetime - shrink particles
            var sizeOverLifetime = _particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            // Color over lifetime - fade out
            var colorOverLifetime = _particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            // Velocity over lifetime - slow down
            var velocityOverLifetime = _particleSystem.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.speedModifier = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.1f));

            // Renderer - configure material for additive blending
            var renderer = _particleSystem.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            // Create particle material
            var sparkMaterial = CreateAdditiveMaterial();
            renderer.material = sparkMaterial;

            // Disable shadows
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void CreateStarLayer(string name, float scaleMultiplier, out Transform layerTransform, out Material material)
        {
            // Create child object
            var layerObj = new GameObject(name);
            layerObj.transform.SetParent(transform);
            layerObj.transform.localPosition = Vector3.zero;
            layerObj.transform.localRotation = Quaternion.identity;
            layerObj.transform.localScale = Vector3.one * scaleMultiplier;

            layerTransform = layerObj.transform;

            // Add mesh filter with star
            var meshFilter = layerObj.AddComponent<MeshFilter>();
            meshFilter.mesh = CreateStarMesh();

            // Add mesh renderer
            var meshRenderer = layerObj.AddComponent<MeshRenderer>();
            material = CreateAdditiveMaterial();
            meshRenderer.material = material;

            // Disable shadow casting for VFX
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        private Material CreateAdditiveMaterial()
        {
            // Try to find a suitable shader - URP Particles/Unlit works well for additive effects
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Particles/Standard Unlit");
            }
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            var material = new Material(shader);

            // Configure for additive blending
            material.SetFloat("_Surface", 1); // Transparent
            material.SetFloat("_Blend", 1);   // Additive (if supported)
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.One);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = 3000; // Transparent queue
            material.EnableKeyword("_ALPHABLEND_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            // Set initial color with emission
            Color hdrColor = _flashColor * _emissionIntensity;
            material.SetColor(BaseColorId, hdrColor);
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor(EmissionColorId, hdrColor);
                material.EnableKeyword("_EMISSION");
            }

            return material;
        }

        // ============================================
        // MESH GENERATION
        // ============================================

        /// <summary>
        /// Creates a star-shaped mesh with configurable points.
        /// Star shape is created by alternating between outer and inner radius vertices.
        /// </summary>
        private Mesh CreateStarMesh()
        {
            var mesh = new Mesh();
            mesh.name = "StarMesh";

            int pointCount = _starPoints;
            int vertexCount = pointCount * 2 + 1; // Points + center
            int triangleCount = pointCount * 2;

            var vertices = new Vector3[vertexCount];
            var uvs = new Vector2[vertexCount];
            var triangles = new int[triangleCount * 3];

            // Center vertex
            vertices[0] = Vector3.zero;
            uvs[0] = new Vector2(0.5f, 0.5f);

            float outerRadius = 0.5f;
            float innerRadius = outerRadius * _innerRadiusRatio;

            // Create star points
            for (int i = 0; i < pointCount * 2; i++)
            {
                float angle = (i * Mathf.PI) / pointCount - Mathf.PI / 2f;
                float radius = (i % 2 == 0) ? outerRadius : innerRadius;

                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius;

                vertices[i + 1] = new Vector3(x, y, 0);

                // UV mapping (radial)
                float uvX = 0.5f + x;
                float uvY = 0.5f + y;
                uvs[i + 1] = new Vector2(uvX, uvY);
            }

            // Create triangles (fan from center)
            for (int i = 0; i < pointCount * 2; i++)
            {
                int triIndex = i * 3;
                triangles[triIndex] = 0; // Center
                triangles[triIndex + 1] = i + 1;
                triangles[triIndex + 2] = ((i + 1) % (pointCount * 2)) + 1;
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Set the flash color. Call before Start or during spawn.
        /// </summary>
        public void SetColor(Color color)
        {
            _flashColor = color;

            Initialize();

            if (_outerMaterial != null)
            {
                Color hdrColor = color * _emissionIntensity;
                _outerMaterial.SetColor(BaseColorId, hdrColor);
                _outerMaterial.SetColor(EmissionColorId, hdrColor);
            }

            if (_innerMaterial != null)
            {
                Color hdrColor = color * _emissionIntensity * _innerGlowIntensity;
                _innerMaterial.SetColor(BaseColorId, hdrColor);
                _innerMaterial.SetColor(EmissionColorId, hdrColor);
            }

            // Update particle system color
            if (_particleSystem != null)
            {
                var main = _particleSystem.main;
                main.startColor = color;
            }
        }

        /// <summary>
        /// Set the flash color with HDR intensity.
        /// </summary>
        public void SetColor(Color color, float intensity)
        {
            _emissionIntensity = intensity;
            SetColor(color);
        }
    }
}
