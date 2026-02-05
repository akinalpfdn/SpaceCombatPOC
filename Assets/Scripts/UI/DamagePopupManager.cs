// ============================================
// DamagePopupManager.cs
// Manages spawning and pooling of damage popups
// Subscribes to DamagePopupEvent via EventBus
// ============================================

using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using StarReapers.Events;
using StarReapers.ScriptableObjects;

namespace StarReapers.UI
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
        // DAMAGE AGGREGATION
        // ============================================

        /// <summary>
        /// Tracks pending damage from a specific source to aggregate multi-hit attacks.
        /// Key: SourceId (attacker's instance ID)
        /// </summary>
        private Dictionary<int, PendingDamage> _pendingDamage;

        /// <summary>
        /// Default aggregation window if config is not set.
        /// </summary>
        private const float DEFAULT_AGGREGATION_WINDOW = 0.1f;

        /// <summary>
        /// Stores accumulated damage waiting to be displayed.
        /// </summary>
        private struct PendingDamage
        {
            public Vector3 Position;
            public float TotalDamage;
            public bool IsCritical;
            public bool IsShieldDamage;
            public Interfaces.DamageType DamageType;
            public float Timestamp;
        }

        // ============================================
        // UNITY LIFECYCLE
        // ============================================

        private void Awake()
        {
            _pool = new Queue<DamagePopup>();
            _activePopups = new List<DamagePopup>();
            _pendingDamage = new Dictionary<int, PendingDamage>();

            // Create parent object for organization
            _poolParent = new GameObject("DamagePopupPool");
            _poolParent.transform.SetParent(transform);
        }

        private void Update()
        {
            // Process pending damage that has exceeded the aggregation window
            ProcessPendingDamage();
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
            // If no source ID, spawn immediately (no aggregation)
            if (evt.SourceId == 0)
            {
                SpawnPopup(
                    evt.WorldPosition,
                    evt.DamageAmount,
                    evt.IsCritical,
                    evt.IsShieldDamage,
                    evt.DamageType
                );
                return;
            }

            // Aggregate damage from the same source
            if (_pendingDamage.TryGetValue(evt.SourceId, out var pending))
            {
                // Add to existing pending damage
                pending.TotalDamage += evt.DamageAmount;
                pending.IsCritical |= evt.IsCritical;  // Any crit makes it a crit
                pending.Position = evt.WorldPosition;   // Update to latest hit position
                _pendingDamage[evt.SourceId] = pending;

                if (_debugLog)
                {
                    Debug.Log($"[DamagePopupManager] Aggregated damage from source {evt.SourceId}: +{evt.DamageAmount} = {pending.TotalDamage}");
                }
            }
            else
            {
                // Start new pending damage
                _pendingDamage[evt.SourceId] = new PendingDamage
                {
                    Position = evt.WorldPosition,
                    TotalDamage = evt.DamageAmount,
                    IsCritical = evt.IsCritical,
                    IsShieldDamage = evt.IsShieldDamage,
                    DamageType = evt.DamageType,
                    Timestamp = Time.time
                };

                if (_debugLog)
                {
                    Debug.Log($"[DamagePopupManager] Started aggregating damage from source {evt.SourceId}: {evt.DamageAmount}");
                }
            }
        }

        // ============================================
        // DAMAGE AGGREGATION
        // ============================================

        /// <summary>
        /// Processes pending damage and spawns popups when aggregation window expires.
        /// </summary>
        private void ProcessPendingDamage()
        {
            if (_pendingDamage.Count == 0) return;

            float currentTime = Time.time;
            float aggregationWindow = _config != null ? _config.AggregationWindow : DEFAULT_AGGREGATION_WINDOW;
            var keysToRemove = new List<int>();

            foreach (var kvp in _pendingDamage)
            {
                if (currentTime - kvp.Value.Timestamp >= aggregationWindow)
                {
                    // Aggregation window expired - spawn the popup
                    var pending = kvp.Value;
                    SpawnPopup(
                        pending.Position,
                        pending.TotalDamage,
                        pending.IsCritical,
                        pending.IsShieldDamage,
                        pending.DamageType
                    );
                    keysToRemove.Add(kvp.Key);

                    if (_debugLog)
                    {
                        Debug.Log($"[DamagePopupManager] Spawned aggregated popup for source {kvp.Key}: {pending.TotalDamage}");
                    }
                }
            }

            // Clean up processed entries
            foreach (var key in keysToRemove)
            {
                _pendingDamage.Remove(key);
            }
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
