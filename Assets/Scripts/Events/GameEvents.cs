// ============================================
// EVENT SYSTEM - Observer Pattern Implementation
// Decouples systems completely, enables scalability
// ============================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceCombat.Events
{
    /// <summary>
    /// Central event bus for game-wide communication
    /// Implements Observer pattern for loose coupling
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _eventHandlers = new();

        public static void Subscribe<T>(Action<T> handler) where T : IGameEvent
        {
            var type = typeof(T);
            if (!_eventHandlers.ContainsKey(type))
            {
                _eventHandlers[type] = new List<Delegate>();
            }
            _eventHandlers[type].Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
        {
            var type = typeof(T);
            if (_eventHandlers.ContainsKey(type))
            {
                _eventHandlers[type].Remove(handler);
            }
        }

        public static void Publish<T>(T gameEvent) where T : IGameEvent
        {
            var type = typeof(T);
            if (_eventHandlers.ContainsKey(type))
            {
                foreach (var handler in _eventHandlers[type].ToArray())
                {
                    try
                    {
                        (handler as Action<T>)?.Invoke(gameEvent);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error handling event {type.Name}: {e.Message}");
                    }
                }
            }
        }

        public static void Clear()
        {
            _eventHandlers.Clear();
        }

        public static void ClearEvent<T>() where T : IGameEvent
        {
            var type = typeof(T);
            if (_eventHandlers.ContainsKey(type))
            {
                _eventHandlers[type].Clear();
            }
        }
    }

    /// <summary>
    /// Marker interface for all game events
    /// </summary>
    public interface IGameEvent { }

    // ============================================
    // COMBAT EVENTS
    // ============================================

    public struct DamageEvent : IGameEvent
    {
        public GameObject Target;
        public GameObject Source;
        public float Damage;
        public Interfaces.DamageType DamageType;
        public Vector2 HitPosition;

        public DamageEvent(GameObject target, GameObject source, float damage, 
            Interfaces.DamageType damageType, Vector2 hitPosition)
        {
            Target = target;
            Source = source;
            Damage = damage;
            DamageType = damageType;
            HitPosition = hitPosition;
        }
    }

    public struct EntityDeathEvent : IGameEvent
    {
        public GameObject Entity;
        public Vector2 Position;
        public bool IsPlayer;
        public int ScoreValue;

        public EntityDeathEvent(GameObject entity, Vector2 position, bool isPlayer, int scoreValue = 0)
        {
            Entity = entity;
            Position = position;
            IsPlayer = isPlayer;
            ScoreValue = scoreValue;
        }
    }

    public struct ProjectileFiredEvent : IGameEvent
    {
        public Vector2 Origin;
        public Vector2 Direction;
        public string WeaponType;
        public bool IsPlayer;

        public ProjectileFiredEvent(Vector2 origin, Vector2 direction, string weaponType, bool isPlayer)
        {
            Origin = origin;
            Direction = direction;
            WeaponType = weaponType;
            IsPlayer = isPlayer;
        }
    }

    public struct ExplosionEvent : IGameEvent
    {
        public Vector2 Position;
        public float Radius;
        public ExplosionSize Size;

        public ExplosionEvent(Vector2 position, float radius, ExplosionSize size)
        {
            Position = position;
            Radius = radius;
            Size = size;
        }
    }

    public enum ExplosionSize { Small, Medium, Large }

    /// <summary>
    /// Published when a projectile hits an entity's shield.
    /// Used by ShieldVisualController to spawn ripple effects at exact hit point.
    /// </summary>
    public struct ShieldHitEvent : IGameEvent
    {
        public GameObject Target;           // Entity that was hit
        public Vector3 HitWorldPosition;    // Exact world-space hit point (from raycast)
        public float DamageAmount;          // For intensity scaling
        public Interfaces.DamageType DamageType;  // For potential color variation

        public ShieldHitEvent(GameObject target, Vector3 hitPosition,
            float damage, Interfaces.DamageType damageType)
        {
            Target = target;
            HitWorldPosition = hitPosition;
            DamageAmount = damage;
            DamageType = damageType;
        }
    }

    // ============================================
    // GAME STATE EVENTS
    // ============================================

    public struct GameStateChangedEvent : IGameEvent
    {
        public GameState PreviousState;
        public GameState NewState;

        public GameStateChangedEvent(GameState previous, GameState newState)
        {
            PreviousState = previous;
            NewState = newState;
        }
    }

    public enum GameState { Loading, Playing, Paused, GameOver, Victory }

    public struct ScoreChangedEvent : IGameEvent
    {
        public int NewScore;
        public int Delta;

        public ScoreChangedEvent(int newScore, int delta)
        {
            NewScore = newScore;
            Delta = delta;
        }
    }

    public struct WaveStartedEvent : IGameEvent
    {
        public int WaveNumber;
        public int EnemyCount;

        public WaveStartedEvent(int waveNumber, int enemyCount)
        {
            WaveNumber = waveNumber;
            EnemyCount = enemyCount;
        }
    }

    public struct WaveCompletedEvent : IGameEvent
    {
        public int WaveNumber;
        public float CompletionTime;

        public WaveCompletedEvent(int waveNumber, float completionTime)
        {
            WaveNumber = waveNumber;
            CompletionTime = completionTime;
        }
    }

    // ============================================
    // PLAYER EVENTS
    // ============================================

    public struct PlayerHealthChangedEvent : IGameEvent
    {
        public float CurrentHealth;
        public float MaxHealth;
        public float Percentage;

        public PlayerHealthChangedEvent(float current, float max)
        {
            CurrentHealth = current;
            MaxHealth = max;
            Percentage = current / max;
        }
    }

    public struct PlayerShieldChangedEvent : IGameEvent
    {
        public float CurrentShield;
        public float MaxShield;

        public PlayerShieldChangedEvent(float current, float max)
        {
            CurrentShield = current;
            MaxShield = max;
        }
    }

    // ============================================
    // AUDIO EVENTS
    // ============================================

    public struct PlaySFXEvent : IGameEvent
    {
        public string SFXId;
        public Vector2? Position;
        public float VolumeScale;

        public PlaySFXEvent(string sfxId, Vector2? position = null, float volumeScale = 1f)
        {
            SFXId = sfxId;
            Position = position;
            VolumeScale = volumeScale;
        }
    }

    // ============================================
    // SPAWN EVENTS
    // ============================================

    public struct EnemySpawnedEvent : IGameEvent
    {
        public GameObject Enemy;
        public Vector2 Position;
        public string EnemyType;

        public EnemySpawnedEvent(GameObject enemy, Vector2 position, string enemyType)
        {
            Enemy = enemy;
            Position = position;
            EnemyType = enemyType;
        }
    }
}
