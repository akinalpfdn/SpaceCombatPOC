// ============================================
// OBJECT POOL - Performance Optimization
// Generic, reusable pool for projectiles, enemies, effects
// ============================================

using System;
using System.Collections.Generic;
using UnityEngine;
using StarReapers.Interfaces;
using StarReapers.Core;

namespace StarReapers.Utilities
{
    /// <summary>
    /// Non-generic interface for pool operations.
    /// Eliminates reflection in PoolManager.
    /// </summary>
    public interface IPool
    {
        int AvailableCount { get; }
        int TotalCount { get; }
        int ActiveCount { get; }
        void ReturnAll();
        void Clear();
    }

    /// <summary>
    /// Generic object pool for Unity GameObjects
    /// Reduces garbage collection and instantiation overhead
    /// </summary>
    /// <typeparam name="T">Component type that implements IPoolable</typeparam>
    public class ObjectPool<T> : IPool where T : Component, IPoolable
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Queue<T> _availableObjects;
        private readonly HashSet<T> _availableSet; // O(1) contains check
        private readonly List<T> _allObjects;
        private readonly int _maxSize;
        private readonly bool _autoExpand;

        public int AvailableCount => _availableObjects.Count;
        public int TotalCount => _allObjects.Count;
        public int ActiveCount => TotalCount - AvailableCount;

        public ObjectPool(T prefab, int initialSize, int maxSize = 100, 
            Transform parent = null, bool autoExpand = true)
        {
            _prefab = prefab;
            _maxSize = maxSize;
            _parent = parent;
            _autoExpand = autoExpand;
            _availableObjects = new Queue<T>(initialSize);
            _availableSet = new HashSet<T>();
            _allObjects = new List<T>(initialSize);

            Prewarm(initialSize);
        }

        /// <summary>
        /// Pre-instantiate objects to avoid runtime allocation
        /// </summary>
        public void Prewarm(int count)
        {
            for (int i = 0; i < count && _allObjects.Count < _maxSize; i++)
            {
                CreateNewObject();
            }
        }

        /// <summary>
        /// Get an object from the pool
        /// </summary>
        public T Get()
        {
            T obj;

            if (_availableObjects.Count > 0)
            {
                obj = _availableObjects.Dequeue();
                _availableSet.Remove(obj);
            }
            else if (_autoExpand && _allObjects.Count < _maxSize)
            {
                obj = CreateNewObject();
                _availableObjects.Dequeue(); // Remove from available since we're using it
                _availableSet.Remove(obj);
            }
            else
            {
                Debug.LogWarning($"Pool exhausted for {typeof(T).Name}! Consider increasing pool size.");
                return null;
            }

            obj.gameObject.SetActive(true);
            obj.OnSpawn();
            return obj;
        }

        /// <summary>
        /// Get an object and position it
        /// Position is set BEFORE OnSpawn() so components can use their spawn position
        /// </summary>
        public T Get(Vector3 position, Quaternion rotation)
        {
            T obj;

            if (_availableObjects.Count > 0)
            {
                obj = _availableObjects.Dequeue();
                _availableSet.Remove(obj);
            }
            else if (_autoExpand && _allObjects.Count < _maxSize)
            {
                obj = CreateNewObject();
                _availableObjects.Dequeue(); // Remove from available since we're using it
                _availableSet.Remove(obj);
            }
            else
            {
                Debug.LogWarning($"Pool exhausted for {typeof(T).Name}! Consider increasing pool size.");
                return null;
            }

            // Set position BEFORE OnSpawn so components can correctly initialize with spawn position
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.gameObject.SetActive(true);
            obj.OnSpawn();
            return obj;
        }

        /// <summary>
        /// Return an object to the pool
        /// </summary>
        public void Return(T obj)
        {
            if (obj == null) return;

            obj.OnDespawn();
            obj.ResetState();
            obj.gameObject.SetActive(false);

            if (_availableSet.Add(obj)) // O(1) duplicate check
            {
                _availableObjects.Enqueue(obj);
            }
        }

        /// <summary>
        /// Return all active objects to the pool
        /// </summary>
        public void ReturnAll()
        {
            foreach (var obj in _allObjects)
            {
                if (obj.gameObject.activeInHierarchy)
                {
                    Return(obj);
                }
            }
        }

        private T CreateNewObject()
        {
            // IL2CPP fix: Use non-generic Instantiate to avoid cast issues
            var clone = UnityEngine.Object.Instantiate(_prefab as UnityEngine.Object, _parent);

            // Try direct cast first, fallback to GetComponent if clone is GameObject
            T obj = clone as T;
            if (obj == null)
            {
                var go = clone as GameObject;
                if (go != null) obj = go.GetComponent<T>();
            }

            if (obj == null) return null;

            obj.gameObject.SetActive(false);
            obj.ResetState();
            _allObjects.Add(obj);
            _availableObjects.Enqueue(obj);
            _availableSet.Add(obj);
            return obj;
        }

        /// <summary>
        /// Destroy all pooled objects
        /// </summary>
        public void Clear()
        {
            foreach (var obj in _allObjects)
            {
                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj.gameObject);
                }
            }
            _allObjects.Clear();
            _availableObjects.Clear();
            _availableSet.Clear();
        }
    }
 
}
