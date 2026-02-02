using System.Collections.Generic;
using UnityEngine;
using SpaceCombat.Interfaces;
using Component = UnityEngine.Component;

namespace SpaceCombat.Utilities
{
    /// <summary>
    /// Manager for multiple object pools
    /// Centralized access point for all pools
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        private readonly Dictionary<string, IPool> _pools = new();

        /// <summary>
        /// Create and register a new pool
        /// </summary>
        public ObjectPool<T> CreatePool<T>(string poolId, T prefab, int initialSize,
            int maxSize = 100, bool autoExpand = true) where T : Component, IPoolable
        {
            if (_pools.ContainsKey(poolId))
            {
                Debug.LogWarning($"Pool {poolId} already exists!");
                return GetPool<T>(poolId);
            }

            var poolParent = new GameObject($"Pool_{poolId}");
            poolParent.transform.SetParent(transform);

            var pool = new ObjectPool<T>(prefab, initialSize, maxSize,
                poolParent.transform, autoExpand);

            _pools[poolId] = pool;
            return pool;
        }

        /// <summary>
        /// Get an existing pool
        /// </summary>
        public ObjectPool<T> GetPool<T>(string poolId) where T : Component, IPoolable
        {
            if (_pools.TryGetValue(poolId, out var pool) && pool is ObjectPool<T> typedPool)
            {
                return typedPool;
            }

            Debug.LogError($"Pool {poolId} not found!");
            return null;
        }

        /// <summary>
        /// Check if a pool exists
        /// </summary>
        public bool HasPool(string poolId)
        {
            return _pools.ContainsKey(poolId);
        }

        /// <summary>
        /// Clear a specific pool
        /// </summary>
        public void ClearPool(string poolId)
        {
            if (_pools.TryGetValue(poolId, out var pool))
            {
                pool.Clear();
                _pools.Remove(poolId);
            }
        }

        /// <summary>
        /// Clear all pools
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Clear();
            }
            _pools.Clear();
        }

        private void OnDestroy()
        {
            ClearAllPools();
        }
    }
}