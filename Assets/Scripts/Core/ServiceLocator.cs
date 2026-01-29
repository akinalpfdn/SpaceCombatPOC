// ============================================
// SERVICE LOCATOR - Dependency Inversion Implementation
// Provides runtime dependency resolution without tight coupling
// ============================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceCombat.Core
{
    /// <summary>
    /// Service Locator pattern implementation
    /// Provides a way to access services without direct dependencies
    /// Use sparingly - prefer constructor injection where possible
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();
        private static readonly Dictionary<Type, Func<object>> _factories = new();

        /// <summary>
        /// Register a service instance
        /// </summary>
        public static void Register<T>(T service) where T : class
        {
            var type = typeof(T);
            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"Service {type.Name} already registered. Replacing.");
            }
            _services[type] = service;
        }

        /// <summary>
        /// Register a factory for lazy instantiation
        /// </summary>
        public static void RegisterFactory<T>(Func<T> factory) where T : class
        {
            var type = typeof(T);
            _factories[type] = () => factory();
        }

        /// <summary>
        /// Get a registered service
        /// </summary>
        public static T Get<T>() where T : class
        {
            var type = typeof(T);
            
            if (_services.TryGetValue(type, out var service))
            {
                return service as T;
            }

            if (_factories.TryGetValue(type, out var factory))
            {
                var instance = factory() as T;
                _services[type] = instance;
                return instance;
            }

            Debug.LogError($"Service {type.Name} not registered!");
            return null;
        }

        /// <summary>
        /// Try to get a service, returns false if not found
        /// </summary>
        public static bool TryGet<T>(out T service) where T : class
        {
            service = null;
            var type = typeof(T);

            if (_services.TryGetValue(type, out var svc))
            {
                service = svc as T;
                return true;
            }

            if (_factories.TryGetValue(type, out var factory))
            {
                service = factory() as T;
                _services[type] = service;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if a service is registered
        /// </summary>
        public static bool IsRegistered<T>() where T : class
        {
            var type = typeof(T);
            return _services.ContainsKey(type) || _factories.ContainsKey(type);
        }

        /// <summary>
        /// Unregister a service
        /// </summary>
        public static void Unregister<T>() where T : class
        {
            var type = typeof(T);
            _services.Remove(type);
            _factories.Remove(type);
        }

        /// <summary>
        /// Clear all services - use when changing scenes
        /// </summary>
        public static void Clear()
        {
            _services.Clear();
            _factories.Clear();
        }
    }
 
}
