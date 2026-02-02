using UnityEditor;
using UnityEngine;
using SpaceCombat.ScriptableObjects;
using SpaceCombat.VFX;

namespace SpaceCombat.Editor
{
    /// <summary>
    /// One-click setup tool for laser visual upgrade.
    /// Creates ProjectileVisualConfig asset and configures Projectile prefab.
    /// DELETE THIS FILE after setup is complete.
    /// </summary>
    public static class LaserVisualSetup
    {
        [MenuItem("Tools/SpaceCombat/Setup Laser Visuals")]
        public static void SetupLaserVisuals()
        {
            SetupProjectilePrefab();
            CreateLaserVisualConfig();
            LinkWeaponConfigs();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[LaserVisualSetup] Setup complete! You can delete Assets/Scripts/Editor/LaserVisualSetup.cs now.");
        }

        private static void SetupProjectilePrefab()
        {
            string prefabPath = "Assets/Prefabs/Projectiles/Projectile.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[LaserVisualSetup] Prefab not found at {prefabPath}");
                return;
            }

            // Open prefab for editing
            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

            // Remove SpriteRenderer if still exists (no longer needed in 3D)
            var spriteRenderer = prefabRoot.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                Object.DestroyImmediate(spriteRenderer);
            }

            // Ensure MeshFilter + MeshRenderer exist (no RequireComponent on MeshProjectileVisual)
            var meshFilter = prefabRoot.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = prefabRoot.AddComponent<MeshFilter>();
            }

            var meshRenderer = prefabRoot.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = prefabRoot.AddComponent<MeshRenderer>();
            }

            // Add TrailRenderer if missing
            var trailRenderer = prefabRoot.GetComponent<TrailRenderer>();
            if (trailRenderer == null)
            {
                trailRenderer = prefabRoot.AddComponent<TrailRenderer>();
            }

            // Add MeshProjectileVisual
            var meshVisual = prefabRoot.GetComponent<MeshProjectileVisual>();
            if (meshVisual == null)
            {
                meshVisual = prefabRoot.AddComponent<MeshProjectileVisual>();
            }

            // Configure MeshFilter with built-in Capsule mesh
            if (meshFilter != null)
            {
                // Use Unity's built-in capsule mesh
                var tempCapsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                meshFilter.sharedMesh = tempCapsule.GetComponent<MeshFilter>().sharedMesh;
                Object.DestroyImmediate(tempCapsule);
            }

            // Configure MeshRenderer
            if (meshRenderer != null)
            {
                var bodyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Projectiles/LaserBody.mat");
                if (bodyMat != null)
                {
                    meshRenderer.sharedMaterial = bodyMat;
                }
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
            }

            // Configure TrailRenderer
            if (trailRenderer != null)
            {
                var trailMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Projectiles/LaserTrail.mat");
                if (trailMat != null)
                {
                    trailRenderer.sharedMaterial = trailMat;
                }
                trailRenderer.enabled = true;
                trailRenderer.time = 0.12f;
                trailRenderer.startWidth = 0.1f;
                trailRenderer.endWidth = 0f;
                trailRenderer.numCornerVertices = 2;
                trailRenderer.minVertexDistance = 0.1f;
                trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trailRenderer.receiveShadows = false;

                // Set color gradient (red fade)
                trailRenderer.startColor = new Color(1f, 0.3f, 0.1f, 0.8f);
                trailRenderer.endColor = new Color(1f, 0.3f, 0.1f, 0f);
            }

            // Wire serialized fields on MeshProjectileVisual via SerializedObject
            var so = new SerializedObject(meshVisual);
            so.FindProperty("_meshFilter").objectReferenceValue = meshFilter;
            so.FindProperty("_meshRenderer").objectReferenceValue = meshRenderer;
            so.FindProperty("_trailRenderer").objectReferenceValue = trailRenderer;
            so.ApplyModifiedProperties();

            // Scale handled by MeshProjectileVisual via config
            prefabRoot.transform.localScale = Vector3.one;

            // Save prefab
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            Debug.Log("[LaserVisualSetup] Projectile prefab configured with MeshProjectileVisual + TrailRenderer.");
        }

        private static void CreateLaserVisualConfig()
        {
            // Ensure folder exists
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/Visuals"))
            {
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Visuals");
            }

            string configPath = "Assets/ScriptableObjects/Visuals/LaserVisual_Red.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ProjectileVisualConfig>(configPath);
            if (existing != null)
            {
                Debug.Log("[LaserVisualSetup] LaserVisual_Red already exists, skipping.");
                return;
            }

            var config = ScriptableObject.CreateInstance<ProjectileVisualConfig>();

            // Use SerializedObject to set private [SerializeField] fields
            var so = new SerializedObject(config);

            // Body mesh - Capsule
            var tempCapsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            so.FindProperty("_bodyMesh").objectReferenceValue = tempCapsule.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tempCapsule);

            // Body material
            var bodyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Projectiles/LaserBody.mat");
            so.FindProperty("_bodyMaterial").objectReferenceValue = bodyMat;

            // Mesh scale - thin elongated bolt
            so.FindProperty("_meshScale").vector3Value = new Vector3(0.08f, 0.08f, 0.4f);

            // Emission - red HDR
            so.FindProperty("_emissionColor").colorValue = new Color(1f, 0.3f, 0.1f, 1f);
            so.FindProperty("_emissionIntensity").floatValue = 4f;

            // Trail
            var trailMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Projectiles/LaserTrail.mat");
            so.FindProperty("_trailMaterial").objectReferenceValue = trailMat;
            so.FindProperty("_trailStartWidth").floatValue = 0.1f;
            so.FindProperty("_trailEndWidth").floatValue = 0f;
            so.FindProperty("_trailTime").floatValue = 0.12f;
            so.FindProperty("_trailCornerVertices").intValue = 2;
            so.FindProperty("_trailMinVertexDistance").floatValue = 0.1f;

            so.ApplyModifiedProperties();

            AssetDatabase.CreateAsset(config, configPath);
            Debug.Log($"[LaserVisualSetup] Created {configPath}");
        }

        private static void LinkWeaponConfigs()
        {
            var visualConfig = AssetDatabase.LoadAssetAtPath<ProjectileVisualConfig>(
                "Assets/ScriptableObjects/Visuals/LaserVisual_Red.asset");

            if (visualConfig == null)
            {
                Debug.LogWarning("[LaserVisualSetup] LaserVisual_Red not found, skipping weapon config linking.");
                return;
            }

            // Find all WeaponConfig assets and link the visual config
            string[] guids = AssetDatabase.FindAssets("t:WeaponConfig");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var weaponConfig = AssetDatabase.LoadAssetAtPath<WeaponConfig>(path);
                if (weaponConfig != null && weaponConfig.projectileVisualConfig == null)
                {
                    weaponConfig.projectileVisualConfig = visualConfig;
                    EditorUtility.SetDirty(weaponConfig);
                    Debug.Log($"[LaserVisualSetup] Linked visual config to {weaponConfig.weaponName} at {path}");
                }
            }
        }

        [MenuItem("Tools/SpaceCombat/Setup Laser Visuals - Preview (No Save)")]
        public static void PreviewLaserVisuals()
        {
            Debug.Log("[LaserVisualSetup] This would configure:");
            Debug.Log("  1. Projectile.prefab: Add MeshProjectileVisual + configure TrailRenderer");
            Debug.Log("  2. Create LaserVisual_Red.asset ScriptableObject");
            Debug.Log("  3. Link visual config to all WeaponConfig assets");
        }
    }
}
