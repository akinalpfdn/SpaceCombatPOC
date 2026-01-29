// ============================================
// EDITOR UTILITIES - Quick Setup Helpers
// Place in Assets/Scripts/Editor folder
// ============================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace SpaceCombat.Editor
{
    /// <summary>
    /// Editor window for quick project setup
    /// </summary>
    public class ProjectSetupWindow : EditorWindow
    {
        [MenuItem("SpaceCombat/Project Setup")]
        public static void ShowWindow()
        {
            GetWindow<ProjectSetupWindow>("Space Combat Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Space Combat POC Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("1. Create Folder Structure"))
            {
                CreateFolderStructure();
            }

            if (GUILayout.Button("2. Setup Layers & Tags"))
            {
                SetupLayersAndTags();
            }

            if (GUILayout.Button("3. Create Default ScriptableObjects"))
            {
                CreateDefaultConfigs();
            }

            if (GUILayout.Button("4. Setup Physics Collision Matrix"))
            {
                SetupPhysics();
            }

            GUILayout.Space(20);
            GUILayout.Label("Quick Create", EditorStyles.boldLabel);

            if (GUILayout.Button("Create Player Ship Prefab"))
            {
                CreatePlayerPrefab();
            }

            if (GUILayout.Button("Create Enemy Prefab"))
            {
                CreateEnemyPrefab();
            }

            if (GUILayout.Button("Create Projectile Prefab"))
            {
                CreateProjectilePrefab();
            }

            GUILayout.Space(20);
            if (GUILayout.Button("Create Combat Scene"))
            {
                CreateCombatScene();
            }
        }

        private void CreateFolderStructure()
        {
            string[] folders = new string[]
            {
                "Assets/Prefabs",
                "Assets/Prefabs/Ships",
                "Assets/Prefabs/Enemies",
                "Assets/Prefabs/Projectiles",
                "Assets/Prefabs/Effects",
                "Assets/ScriptableObjects",
                "Assets/ScriptableObjects/Ships",
                "Assets/ScriptableObjects/Weapons",
                "Assets/ScriptableObjects/Enemies",
                "Assets/Scenes",
                "Assets/Audio/SFX",
                "Assets/Audio/Music",
                "Assets/Sprites/Ships",
                "Assets/Sprites/Enemies",
                "Assets/Sprites/Effects",
                "Assets/Sprites/Backgrounds",
                "Assets/Animations"
            };

            foreach (var folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    string[] parts = folder.Split('/');
                    string parent = parts[0];
                    for (int i = 1; i < parts.Length; i++)
                    {
                        string newPath = parent + "/" + parts[i];
                        if (!AssetDatabase.IsValidFolder(newPath))
                        {
                            AssetDatabase.CreateFolder(parent, parts[i]);
                        }
                        parent = newPath;
                    }
                }
            }

            AssetDatabase.Refresh();
            Debug.Log("Folder structure created!");
        }

        private void SetupLayersAndTags()
        {
            Debug.Log("=== MANUAL SETUP REQUIRED ===");
            Debug.Log("Go to: Edit > Project Settings > Tags and Layers");
            Debug.Log("Add these layers:");
            Debug.Log("  Layer 6: Player");
            Debug.Log("  Layer 7: Enemy");
            Debug.Log("  Layer 8: PlayerProjectile");
            Debug.Log("  Layer 9: EnemyProjectile");
            EditorUtility.DisplayDialog("Setup Layers", 
                "Please manually add layers in Project Settings:\n\n" +
                "Layer 6: Player\n" +
                "Layer 7: Enemy\n" +
                "Layer 8: PlayerProjectile\n" +
                "Layer 9: EnemyProjectile", "OK");
        }

        private void CreateDefaultConfigs()
        {
            // Ensure folders exist
            CreateFolderStructure();

            // Create Ship Config
            var shipConfig = ScriptableObject.CreateInstance<ScriptableObjects.ShipConfig>();
            shipConfig.shipName = "Fighter";
            shipConfig.maxHealth = 100;
            shipConfig.maxShield = 50;
            shipConfig.maxSpeed = 10;
            shipConfig.acceleration = 5;
            shipConfig.rotationSpeed = 180;
            AssetDatabase.CreateAsset(shipConfig, "Assets/ScriptableObjects/Ships/PlayerShip_Fighter.asset");

            // Create Weapon Config
            var weaponConfig = ScriptableObject.CreateInstance<ScriptableObjects.WeaponConfig>();
            weaponConfig.weaponName = "Laser";
            weaponConfig.damage = 10;
            weaponConfig.fireRate = 0.15f;
            weaponConfig.projectileSpeed = 30;
            weaponConfig.range = 15;
            weaponConfig.projectileColor = Color.red;
            AssetDatabase.CreateAsset(weaponConfig, "Assets/ScriptableObjects/Weapons/Weapon_Laser.asset");

            // Create Enemy Config
            var enemyConfig = ScriptableObject.CreateInstance<ScriptableObjects.EnemyConfig>();
            enemyConfig.enemyName = "Drone";
            enemyConfig.maxHealth = 30;
            enemyConfig.maxSpeed = 6;
            enemyConfig.attackRange = 8;
            enemyConfig.detectionRange = 12;
            enemyConfig.scoreValue = 100;
            AssetDatabase.CreateAsset(enemyConfig, "Assets/ScriptableObjects/Enemies/Enemy_Drone.asset");

            // Create Game Balance Config
            var balanceConfig = ScriptableObject.CreateInstance<ScriptableObjects.GameBalanceConfig>();
            balanceConfig.baseEnemiesPerWave = 5;
            balanceConfig.additionalEnemiesPerWave = 2;
            balanceConfig.timeBetweenWaves = 3f;
            AssetDatabase.CreateAsset(balanceConfig, "Assets/ScriptableObjects/GameBalanceConfig.asset");

            // Create Sound Library
            var soundLib = ScriptableObject.CreateInstance<Audio.SoundLibrary>();
            AssetDatabase.CreateAsset(soundLib, "Assets/ScriptableObjects/SoundLibrary.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Default ScriptableObjects created!");
        }

        private void SetupPhysics()
        {
            // Player (6) vs Enemy (7) vs PlayerProjectile (8) vs EnemyProjectile (9)
            
            // PlayerProjectile should only hit Enemy
            Physics2D.IgnoreLayerCollision(8, 6, true);  // Ignore Player
            Physics2D.IgnoreLayerCollision(8, 8, true);  // Ignore PlayerProjectile
            Physics2D.IgnoreLayerCollision(8, 9, true);  // Ignore EnemyProjectile
            
            // EnemyProjectile should only hit Player
            Physics2D.IgnoreLayerCollision(9, 7, true);  // Ignore Enemy
            Physics2D.IgnoreLayerCollision(9, 8, true);  // Ignore PlayerProjectile
            Physics2D.IgnoreLayerCollision(9, 9, true);  // Ignore EnemyProjectile
            
            // Player and Enemy can collide
            Physics2D.IgnoreLayerCollision(6, 7, false);
            
            Debug.Log("Physics collision matrix configured!");
        }

        private void CreatePlayerPrefab()
        {
            CreateFolderStructure();
            
            GameObject player = new GameObject("PlayerShip");
            
            var sr = player.AddComponent<SpriteRenderer>();
            sr.color = Color.cyan;
            
            var col = player.AddComponent<CircleCollider2D>();
            col.radius = 0.4f;
            
            var rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.linearDamping = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            
            player.AddComponent<Movement.ShipMovement>();
            player.AddComponent<Combat.WeaponController>();
            player.AddComponent<Entities.PlayerShip>();
            
            // Create weapon mount child
            GameObject weaponMount = new GameObject("WeaponMount");
            weaponMount.transform.SetParent(player.transform);
            weaponMount.transform.localPosition = new Vector3(0, 0.5f, 0);
            
            player.tag = "Player";
            player.layer = 6;
            
            string path = "Assets/Prefabs/Ships/PlayerShip.prefab";
            PrefabUtility.SaveAsPrefabAsset(player, path);
            DestroyImmediate(player);
            
            Debug.Log("Player prefab created at: " + path);
        }

        private void CreateEnemyPrefab()
        {
            CreateFolderStructure();
            
            GameObject enemy = new GameObject("Enemy");
            
            var sr = enemy.AddComponent<SpriteRenderer>();
            sr.color = Color.red;
            
            var col = enemy.AddComponent<CircleCollider2D>();
            col.radius = 0.3f;
            
            var rb = enemy.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.linearDamping = 2;
            
            enemy.AddComponent<Combat.WeaponController>();
            enemy.AddComponent<Entities.Enemy>();
            
            enemy.layer = 7;
            
            string path = "Assets/Prefabs/Enemies/Enemy.prefab";
            PrefabUtility.SaveAsPrefabAsset(enemy, path);
            DestroyImmediate(enemy);
            
            Debug.Log("Enemy prefab created at: " + path);
        }

        private void CreateProjectilePrefab()
        {
            CreateFolderStructure();
            
            GameObject projectile = new GameObject("Projectile");
            
            var sr = projectile.AddComponent<SpriteRenderer>();
            sr.color = Color.yellow;
            
            var col = projectile.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.1f;
            
            var rb = projectile.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            
            var trail = projectile.AddComponent<TrailRenderer>();
            trail.time = 0.1f;
            trail.startWidth = 0.1f;
            trail.endWidth = 0f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            
            projectile.AddComponent<Combat.Projectile>();
            
            string path = "Assets/Prefabs/Projectiles/Projectile.prefab";
            PrefabUtility.SaveAsPrefabAsset(projectile, path);
            DestroyImmediate(projectile);
            
            Debug.Log("Projectile prefab created at: " + path);
        }

        private void CreateCombatScene()
        {
            // Create new scene
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects);
            
            // Create managers parent
            var managers = new GameObject("--- MANAGERS ---");
            
            var bootstrap = new GameObject("GameBootstrap");
            bootstrap.AddComponent<Core.GameBootstrap>();
            
            var gameManager = new GameObject("GameManager");
            gameManager.AddComponent<Core.GameManager>();
            
            var poolManager = new GameObject("PoolManager");
            poolManager.AddComponent<Utilities.PoolManager>();
            
            var inputManager = new GameObject("InputManager");
            inputManager.AddComponent<Input.InputManager>();
            inputManager.AddComponent<Input.KeyboardInputProvider>();
            
            var audioManager = new GameObject("AudioManager");
            audioManager.AddComponent<Audio.AudioManager>();
            
            var vfxManager = new GameObject("VFXManager");
            vfxManager.AddComponent<VFX.VFXManager>();
            
            // Create environment parent
            var environment = new GameObject("--- ENVIRONMENT ---");
            
            // Setup camera
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.gameObject.AddComponent<Environment.CameraFollow>();
                
                var shakeObj = new GameObject("ScreenShake");
                shakeObj.transform.SetParent(mainCam.transform);
                shakeObj.AddComponent<VFX.ScreenShake>();
            }
            
            // Create star field
            var starField = new GameObject("StarField");
            starField.AddComponent<Environment.StarField>();
            
            // Create spawn points parent
            var spawns = new GameObject("--- SPAWN POINTS ---");
            
            var playerSpawn = new GameObject("PlayerSpawnPoint");
            playerSpawn.transform.position = Vector3.zero;
            
            var enemySpawns = new GameObject("EnemySpawnPoints");
            Vector2[] spawnPositions = new Vector2[]
            {
                new Vector2(15, 0),
                new Vector2(-15, 0),
                new Vector2(0, 10),
                new Vector2(0, -10),
                new Vector2(10, 7),
                new Vector2(-10, 7),
                new Vector2(10, -7),
                new Vector2(-10, -7)
            };
            
            for (int i = 0; i < spawnPositions.Length; i++)
            {
                var spawnPoint = new GameObject($"SpawnPoint_{i + 1}");
                spawnPoint.transform.SetParent(enemySpawns.transform);
                spawnPoint.transform.position = spawnPositions[i];
            }
            
            // Create UI parent
            var uiParent = new GameObject("--- UI ---");
            
            // Save scene
            CreateFolderStructure();
            string scenePath = "Assets/Scenes/CombatPOC.unity";
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            
            Debug.Log("Combat scene created at: " + scenePath);
            Debug.Log("Don't forget to assign prefabs to GameManager!");
        }
    }

    /// <summary>
    /// Custom inspector for GameManager
    /// </summary>
    [CustomEditor(typeof(Core.GameManager))]
    public class GameManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Find and Assign Prefabs"))
            {
                FindAndAssignPrefabs();
            }
        }

        private void FindAndAssignPrefabs()
        {
            var gm = target as Core.GameManager;
            var so = new SerializedObject(gm);
            
            // Find player prefab
            string[] playerGuids = AssetDatabase.FindAssets("t:Prefab PlayerShip");
            if (playerGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(playerGuids[0]);
                var prefab = AssetDatabase.LoadAssetAtPath<Entities.PlayerShip>(path);
                so.FindProperty("_playerPrefab").objectReferenceValue = prefab;
            }
            
            // Find enemy prefab
            string[] enemyGuids = AssetDatabase.FindAssets("t:Prefab Enemy");
            if (enemyGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(enemyGuids[0]);
                var prefab = AssetDatabase.LoadAssetAtPath<Entities.Enemy>(path);
                so.FindProperty("_enemyPrefab").objectReferenceValue = prefab;
            }
            
            // Find spawn points
            var playerSpawn = GameObject.Find("PlayerSpawnPoint");
            if (playerSpawn != null)
            {
                so.FindProperty("_playerSpawnPoint").objectReferenceValue = playerSpawn.transform;
            }
            
            var enemySpawnsParent = GameObject.Find("EnemySpawnPoints");
            if (enemySpawnsParent != null)
            {
                var spawns = new Transform[enemySpawnsParent.transform.childCount];
                for (int i = 0; i < spawns.Length; i++)
                {
                    spawns[i] = enemySpawnsParent.transform.GetChild(i);
                }
                
                var spawnsProp = so.FindProperty("_enemySpawnPoints");
                spawnsProp.arraySize = spawns.Length;
                for (int i = 0; i < spawns.Length; i++)
                {
                    spawnsProp.GetArrayElementAtIndex(i).objectReferenceValue = spawns[i];
                }
            }
            
            so.ApplyModifiedProperties();
            Debug.Log("Prefabs and spawn points assigned!");
        }
    }
}
#endif
