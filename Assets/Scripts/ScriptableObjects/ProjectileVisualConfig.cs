// ============================================
// PROJECTILE VISUAL CONFIG - ScriptableObject
// Data-driven configuration for projectile visuals
// Supports mesh-based energy bolt rendering
// ============================================

using UnityEngine;

namespace StarReapers.ScriptableObjects
{
    /// <summary>
    /// Configuration asset for mesh-based projectile visuals.
    /// Defines mesh, materials, emission, and trail parameters.
    ///
    /// Usage: Create via Assets > Create > StarReapers > Projectile Visual Config
    /// Assign to WeaponConfig.projectileVisualConfig for per-weapon visual customization.
    /// </summary>
    [CreateAssetMenu(fileName = "NewProjectileVisual", menuName = "StarReapers/Projectile Visual Config")]
    public class ProjectileVisualConfig : ScriptableObject
    {
        // ============================================
        // MESH BODY
        // ============================================

        [Header("Mesh Body")]
        [Tooltip("3D mesh for the projectile body (Capsule recommended)")]
        [SerializeField] private Mesh _bodyMesh;

        [Tooltip("Material for the mesh body (URP/Particles/Unlit additive recommended)")]
        [SerializeField] private Material _bodyMaterial;

        [Tooltip("Scale of the mesh - stretch along Z for elongated bolt shape")]
        [SerializeField] private Vector3 _meshScale = new Vector3(0.08f, 0.08f, 0.4f);

        // ============================================
        // EMISSION & COLOR
        // ============================================

        [Header("Emission")]
        [Tooltip("Base HDR color for bloom glow effect")]
        [ColorUsage(true, true)]
        [SerializeField] private Color _emissionColor = new Color(1f, 0.3f, 0.1f, 1f);

        [Tooltip("Emission intensity multiplier - higher values create stronger bloom glow")]
        [Range(1f, 10f)]
        [SerializeField] private float _emissionIntensity = 4f;

        // ============================================
        // TRAIL
        // ============================================

        [Header("Trail")]
        [Tooltip("Material for the trail renderer (URP/Particles/Unlit additive recommended)")]
        [SerializeField] private Material _trailMaterial;

        [Tooltip("Trail width at the projectile origin")]
        [Range(0.01f, 1f)]
        [SerializeField] private float _trailStartWidth = 0.1f;

        [Tooltip("Trail width at the tail end (0 for sharp fade)")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _trailEndWidth = 0f;

        [Tooltip("Duration the trail persists behind the projectile (seconds)")]
        [Range(0.02f, 1f)]
        [SerializeField] private float _trailTime = 0.12f;

        [Tooltip("Number of extra vertices at trail corners for smoother curves")]
        [Range(0, 5)]
        [SerializeField] private int _trailCornerVertices = 2;

        [Tooltip("Minimum distance projectile must move before adding new trail vertex")]
        [Range(0.01f, 1f)]
        [SerializeField] private float _trailMinVertexDistance = 0.1f;

        // ============================================
        // PUBLIC PROPERTIES
        // ============================================

        public Mesh BodyMesh => _bodyMesh;
        public Material BodyMaterial => _bodyMaterial;
        public Vector3 MeshScale => _meshScale;
        public Color EmissionColor => _emissionColor;
        public float EmissionIntensity => _emissionIntensity;
        public Material TrailMaterial => _trailMaterial;
        public float TrailStartWidth => _trailStartWidth;
        public float TrailEndWidth => _trailEndWidth;
        public float TrailTime => _trailTime;
        public int TrailCornerVertices => _trailCornerVertices;
        public float TrailMinVertexDistance => _trailMinVertexDistance;
    }
}
