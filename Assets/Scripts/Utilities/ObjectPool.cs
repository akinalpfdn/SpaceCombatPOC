// ============================================
// OBJECT POOL - Performance Optimization
// Generic, reusable pool for projectiles, enemies, effects
// ============================================

using System;
using System.Collections.Generic;
using UnityEngine;
using SpaceCombat.Interfaces;
using SpaceCombat.Core;

namespace SpaceCombat.Utilities
{
    /// <summary>
    /// Generic object pool for Unity GameObjects
    /// Reduces garbage collection and instantiation overhead
    /// </summary>
    /// <typeparam name="T">Component type that implements IPoolable</typeparam>
    public class ObjectPool<T> where T : Component, IPoolable
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Queue<T> _availableObjects;
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
            }
            else if (_autoExpand && _allObjects.Count < _maxSize)
            {
                obj = CreateNewObject();
            }
            else if (_availableObjects.Count == 0)
            {
                Debug.LogWarning($"Pool exhausted for {typeof(T).Name}! Consider increasing pool size.");
                return null;
            }
            else
            {
                obj = _availableObjects.Dequeue();
            }

            obj.gameObject.SetActive(true);
            obj.OnSpawn();
            return obj;
        }

        /// <summary>
        /// Get an object and position it
        /// </summary>
        public T Get(Vector3 position, Quaternion rotation)
        {
            var obj = Get();
            if (obj != null)
            {
                obj.transform.SetPositionAndRotation(position, rotation);
            }
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
            
            if (!_availableObjects.Contains(obj))
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
            var obj = UnityEngine.Object.Instantiate(_prefab, _parent);
            obj.gameObject.SetActive(false);
            obj.ResetState();
            _allObjects.Add(obj);
            _availableObjects.Enqueue(obj);
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
        }
    }
 
}
