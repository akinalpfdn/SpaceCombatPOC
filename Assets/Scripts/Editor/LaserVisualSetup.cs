#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using StarReapers.ScriptableObjects;
using StarReapers.VFX;

namespace StarReapers.Editor
{
    /// <summary>
    /// One-click setup tool for laser visual upgrade.
    /// Creates ProjectileVisualConfig asset and configures Projectile prefab.
    /// DELETE THIS FILE after setup is complete.
    /// </summary>
    public static class LaserVisualSetup
    {
        [MenuItem("Tools/StarReapers/Setup Laser Visuals")]
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

        // ============================================
        // PHASE 2: VISUAL VARIANTS (Player Blue + Enemy Red)
        // ============================================

        [MenuItem("Tools/StarReapers/Setup Laser Visual Variants")]
        public static void SetupLaserVisualVariants()
        {
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/Visuals"))
            {
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Visuals");
            }

            // 1. Create player cyan visual config
            CreateVisualConfig(
                path: "Assets/ScriptableObjects/Visuals/LaserVisual_PlayerCyan.asset",
                meshScale: new Vector3(0.08f, 0.08f, 0.4f),
                emissionColor: new Color(0.2f, 0.8f, 1f, 1f), // Cyan/blue
                emissionIntensity: 5f,
                trailStartWidth: 0.12f,
                trailTime: 0.15f
            );

            // 2. Update existing LaserVisual_Red for enemy use (smaller, dimmer)
            UpdateEnemyVisualConfig();

            // 3. Create separate enemy weapon config
            CreateEnemyWeaponConfig();

            // 4. Link configs: Player weapon = cyan, Enemy = red
            LinkVariantConfigs();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[LaserVisualSetup] Visual variants setup complete!");
            Debug.Log("  Player: Cyan laser, wide trail, bright glow");
            Debug.Log("  Enemy: Red laser, thin trail, dimmer glow");
        }

        private static void CreateVisualConfig(
            string path, Vector3 meshScale, Color emissionColor,
            float emissionIntensity, float trailStartWidth, float trailTime)
        {
            var existing = AssetDatabase.LoadAssetAtPath<ProjectileVisualConfig>(path);
            if (existing != null)
            {
                Debug.Log($"[LaserVisualSetup] {path} already exists, skipping.");
                return;
            }

            var config = ScriptableObject.CreateInstance<ProjectileVisualConfig>();
            var so = new SerializedObject(config);

            // Body mesh - Capsule
            var tempCapsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            so.FindProperty("_bodyMesh").objectReferenceValue =
                tempCapsule.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tempCapsule);

            // Materials (shared between variants - color comes from MaterialPropertyBlock)
            var bodyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Projectiles/LaserBody.mat");
            var trailMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Projectiles/LaserTrail.mat");
            so.FindProperty("_bodyMaterial").objectReferenceValue = bodyMat;
            so.FindProperty("_trailMaterial").objectReferenceValue = trailMat;

            // Config-specific values
            so.FindProperty("_meshScale").vector3Value = meshScale;
            so.FindProperty("_emissionColor").colorValue = emissionColor;
            so.FindProperty("_emissionIntensity").floatValue = emissionIntensity;
            so.FindProperty("_trailStartWidth").floatValue = trailStartWidth;
            so.FindProperty("_trailEndWidth").floatValue = 0f;
            so.FindProperty("_trailTime").floatValue = trailTime;
            so.FindProperty("_trailCornerVertices").intValue = 2;
            so.FindProperty("_trailMinVertexDistance").floatValue = 0.1f;

            so.ApplyModifiedProperties();

            AssetDatabase.CreateAsset(config, path);
            Debug.Log($"[LaserVisualSetup] Created {path}");
        }

        private static void UpdateEnemyVisualConfig()
        {
            string path = "Assets/ScriptableObjects/Visuals/LaserVisual_Red.asset";
            var config = AssetDatabase.LoadAssetAtPath<ProjectileVisualConfig>(path);
            if (config == null)
            {
                // Create it if it doesn't exist
                CreateVisualConfig(
                    path: path,
                    meshScale: new Vector3(0.05f, 0.05f, 0.25f), // Smaller bolt
                    emissionColor: new Color(1f, 0.3f, 0.1f, 1f), // Red/orange
                    emissionIntensity: 3f, // Dimmer than player
                    trailStartWidth: 0.05f, // Thinner trail
                    trailTime: 0.08f // Shorter trail
                );
                return;
            }

            // Update existing config with enemy-appropriate values
            var so = new SerializedObject(config);
            so.FindProperty("_meshScale").vector3Value = new Vector3(0.05f, 0.05f, 0.25f);
            so.FindProperty("_emissionColor").colorValue = new Color(1f, 0.3f, 0.1f, 1f);
            so.FindProperty("_emissionIntensity").floatValue = 3f;
            so.FindProperty("_trailStartWidth").floatValue = 0.05f;
            so.FindProperty("_trailTime").floatValue = 0.08f;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(config);
            Debug.Log($"[LaserVisualSetup] Updated {path} with enemy visual values (smaller, dimmer)");
        }

        private static void CreateEnemyWeaponConfig()
        {
            string path = "Assets/ScriptableObjects/Weapons/Weapon_EnemyLaser.asset";
            var existing = AssetDatabase.LoadAssetAtPath<WeaponConfig>(path);
            if (existing != null)
            {
                Debug.Log("[LaserVisualSetup] Weapon_EnemyLaser already exists, skipping.");
                return;
            }

            // Load player weapon as template for projectile prefab reference
            var playerWeapon = AssetDatabase.LoadAssetAtPath<WeaponConfig>(
                "Assets/ScriptableObjects/Weapons/Weapon_Laser.asset");
            if (playerWeapon == null)
            {
                Debug.LogError("[LaserVisualSetup] Weapon_Laser not found, cannot create enemy variant.");
                return;
            }

            var enemyWeapon = ScriptableObject.CreateInstance<WeaponConfig>();
            enemyWeapon.weaponName = "Enemy Laser";
            enemyWeapon.damageType = playerWeapon.damageType;
            enemyWeapon.damage = 15f; // Lower than player
            enemyWeapon.fireRate = 0.5f;
            enemyWeapon.projectileSpeed = 35f; // Slower than player
            enemyWeapon.range = 20f;
            enemyWeapon.accuracy = 0.85f; // Less accurate than player
            enemyWeapon.projectilePrefab = playerWeapon.projectilePrefab; // Same prefab
            enemyWeapon.projectilesPerShot = 1; // Single shot
            enemyWeapon.spreadAngle = 0f;
            enemyWeapon.projectileColor = new Color(1f, 0.3f, 0.1f, 1f); // Red
            enemyWeapon.projectileScale = 1f; // Smaller than player
            enemyWeapon.fireSoundId = "laser_fire";
            enemyWeapon.hitSoundId = "laser_hit";

            // Link enemy visual config
            var enemyVisual = AssetDatabase.LoadAssetAtPath<ProjectileVisualConfig>(
                "Assets/ScriptableObjects/Visuals/LaserVisual_Red.asset");
            enemyWeapon.projectileVisualConfig = enemyVisual;

            AssetDatabase.CreateAsset(enemyWeapon, path);
            Debug.Log($"[LaserVisualSetup] Created {path}");
        }

        private static void LinkVariantConfigs()
        {
            // Link player weapon to cyan visual
            var playerVisual = AssetDatabase.LoadAssetAtPath<ProjectileVisualConfig>(
                "Assets/ScriptableObjects/Visuals/LaserVisual_PlayerCyan.asset");
            var playerWeapon = AssetDatabase.LoadAssetAtPath<WeaponConfig>(
                "Assets/ScriptableObjects/Weapons/Weapon_Laser.asset");

            if (playerVisual != null && playerWeapon != null)
            {
                playerWeapon.projectileVisualConfig = playerVisual;
                playerWeapon.projectileColor = new Color(0.2f, 0.8f, 1f, 1f); // Cyan
                EditorUtility.SetDirty(playerWeapon);
                Debug.Log("[LaserVisualSetup] Player Weapon_Laser → LaserVisual_PlayerCyan");
            }

            // Link enemy config to enemy weapon
            var enemyWeapon = AssetDatabase.LoadAssetAtPath<WeaponConfig>(
                "Assets/ScriptableObjects/Weapons/Weapon_EnemyLaser.asset");
            var enemyConfig = AssetDatabase.LoadAssetAtPath<EnemyConfig>(
                "Assets/ScriptableObjects/Enemies/Enemy_Drone.asset");

            if (enemyWeapon != null && enemyConfig != null)
            {
                enemyConfig.weapon = enemyWeapon;
                EditorUtility.SetDirty(enemyConfig);
                Debug.Log("[LaserVisualSetup] Enemy_Drone → Weapon_EnemyLaser");
            }
        }

        [MenuItem("Tools/StarReapers/Setup Laser Visuals - Preview (No Save)")]
        public static void PreviewLaserVisuals()
        {
            Debug.Log("[LaserVisualSetup] This would configure:");
            Debug.Log("  1. Projectile.prefab: Add MeshProjectileVisual + configure TrailRenderer");
            Debug.Log("  2. Create LaserVisual_Red.asset ScriptableObject");
            Debug.Log("  3. Link visual config to all WeaponConfig assets");
        }
    }
}
#endif
