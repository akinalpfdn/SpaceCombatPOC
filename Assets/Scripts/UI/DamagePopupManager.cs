// ============================================
// DamagePopupManager.cs
// Manages spawning and pooling of damage popups
// Subscribes to DamagePopupEvent via EventBus
// ============================================

using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using SpaceCombat.Events;
using SpaceCombat.ScriptableObjects;

namespace SpaceCombat.UI
{
    /// <summary>
    /// Service that manages damage popup spawning and object pooling.
    /// Subscribes to DamagePopupEvent and spawns floating damage numbers.
    ///
    /// Design Patterns:
    /// - Service: Central manager for damage popups
    /// - Object Pool: Efficient reuse of popup instances
    /// - Observer: Subscribes to DamagePopupEvent via EventBus
    ///
    /// Usage:
    /// 1. Create DamagePopupConfig asset
    /// 2. Attach this to a GameObject in scene
    /// 3. Assign config in inspector
    /// 4. Publish DamagePopupEvent when damage occurs
    /// </summary>
    public class DamagePopupManager : MonoBehaviour
    {
        // ============================================
        // CONFIGURATION
        // ============================================

        [Header("Configuration")]
        [SerializeField] private DamagePopupConfig _config;

        [Header("Debug")]
        [SerializeField] private bool _debugLog = false;

        // ============================================
        // POOL STATE
        // ============================================

        private Queue<DamagePopup> _pool;
        private List<DamagePopup> _activePopups;
        private GameObject _poolParent;

        // ============================================
        // UNITY LIFECYCLE
        // ============================================

        private void Awake()
        {
            _pool = new Queue<DamagePopup>();
            _activePopups = new List<DamagePopup>();

            // Create parent object for organization
            _poolParent = new GameObject("DamagePopupPool");
            _poolParent.transform.SetParent(transform);
        }

        private void Start()
        {
            if (_config == null)
            {
                Debug.LogError("[DamagePopupManager] No config assigned!");
                return;
            }

            // Pre-warm pool
            InitializePool();

            // Subscribe to damage popup events
            EventBus.Subscribe<DamagePopupEvent>(OnDamagePopupEvent);

            if (_debugLog)
            {
                Debug.Log($"[DamagePopupManager] Initialized with pool size: {_config.InitialPoolSize}");
            }
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<DamagePopupEvent>(OnDamagePopupEvent);
        }

        // ============================================
        // INITIALIZATION
        // ============================================

        private void InitializePool()
        {
            for (int i = 0; i < _config.InitialPoolSize; i++)
            {
                var popup = CreatePopupInstance();
                popup.OnDespawn();
                _pool.Enqueue(popup);
            }
        }

        private DamagePopup CreatePopupInstance()
        {
            // Create GameObject with TextMeshPro
            var go = new GameObject("DamagePopup");
            go.transform.SetParent(_poolParent.transform);

            // Add TextMeshPro component
            var textMesh = go.AddComponent<TMPro.TextMeshPro>();
            textMesh.alignment = TMPro.TextAlignmentOptions.Center;
            textMesh.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            textMesh.sortingOrder = 100; // Render on top

            // Add DamagePopup component
            var popup = go.AddComponent<DamagePopup>();
            popup.OnComplete = ReturnToPool;

            return popup;
        }

        // ============================================
        // EVENT HANDLERS
        // ============================================

        private void OnDamagePopupEvent(DamagePopupEvent evt)
        {
            SpawnPopup(
                evt.WorldPosition,
                evt.DamageAmount,
                evt.IsCritical,
                evt.IsShieldDamage,
                evt.DamageType
            );
        }

        // ============================================
        // SPAWNING
        // ============================================

        /// <summary>
        /// Spawns a damage popup at the specified position.
        /// </summary>
        public void SpawnPopup(Vector3 worldPosition, float damage,
            bool isCritical = false, bool isShieldDamage = false,
            Interfaces.DamageType damageType = Interfaces.DamageType.Normal)
        {
            if (_config == null) return;

            DamagePopup popup = GetFromPool();
            if (popup == null) return;

            popup.OnSpawn();
            popup.Initialize(_config, worldPosition, damage, isCritical, isShieldDamage, damageType);
            _activePopups.Add(popup);

            if (_debugLog)
            {
                Debug.Log($"[DamagePopupManager] Spawned popup: {damage} at {worldPosition}");
            }
        }

        // ============================================
        // POOLING
        // ============================================

        private DamagePopup GetFromPool()
        {
            if (_pool.Count > 0)
            {
                return _pool.Dequeue();
            }

            // Pool empty - create new if under max
            if (_activePopups.Count < _config.MaxPoolSize)
            {
                return CreatePopupInstance();
            }

            // At max capacity - reuse oldest
            if (_activePopups.Count > 0)
            {
                var oldest = _activePopups[0];
                _activePopups.RemoveAt(0);
                oldest.ResetState();
                return oldest;
            }

            return null;
        }

        private void ReturnToPool(DamagePopup popup)
        {
            if (popup == null) return;

            popup.OnDespawn();
            _activePopups.Remove(popup);
            _pool.Enqueue(popup);
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Updates the config at runtime.
        /// </summary>
        public void SetConfig(DamagePopupConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Clears all active popups.
        /// </summary>
        public void ClearAllPopups()
        {
            foreach (var popup in _activePopups.ToArray())
            {
                ReturnToPool(popup);
            }
            _activePopups.Clear();
        }
    }
}
