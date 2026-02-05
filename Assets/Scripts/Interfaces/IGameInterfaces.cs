// ============================================
// INTERFACES - Following Interface Segregation Principle
// Each interface has a single, focused responsibility
// ============================================

using UnityEngine;
using System;

namespace StarReapers.Interfaces
{
    /// <summary>
    /// For entities that can take damage.
    /// </summary>
    public interface IDamageable
    {
        float CurrentHealth { get; }
        float MaxHealth { get; }
        bool IsAlive { get; }

        /// <summary>
        /// Apply damage to the entity.
        /// </summary>
        /// <param name="amount">Damage amount</param>
        /// <param name="damageType">Type of damage for visual effects</param>
        /// <param name="source">Source of damage for popup aggregation (null = no aggregation)</param>
        void TakeDamage(float amount, DamageType damageType = DamageType.Normal, GameObject source = null);
        void Heal(float amount);

        event Action<float, float> OnHealthChanged; // current, max
        event Action OnDeath;
    }

    /// <summary>
    /// For entities that can move
    /// </summary>
    public interface IMovable
    {
        Vector2 Velocity { get; }
        float MaxSpeed { get; }
        
        void Move(Vector2 direction);
        void SetSpeed(float speed);
        void Stop();
    }

    /// <summary>
    /// For weapons that can fire
    /// </summary>
    public interface IWeapon
    {
        string WeaponName { get; }
        float Damage { get; }
        float FireRate { get; }
        bool CanFire { get; }
        
        void Fire(Vector2 direction, Vector2 origin);
        void Reload();
        
        event Action OnFire;
        event Action OnReload;
    }

    /// <summary>
    /// For objects managed by object pools
    /// </summary>
    public interface IPoolable
    {
        bool IsActive { get; }
        
        void OnSpawn();
        void OnDespawn();
        void ResetState();
    }

    /// <summary>
    /// For entities with AI behavior
    /// </summary>
    public interface IEnemy
    {
        EnemyState CurrentState { get; }
        Transform Target { get; }
        
        void SetTarget(Transform target);
        void UpdateBehavior();
    }

    /// <summary>
    /// For projectiles
    /// </summary>
    public interface IProjectile : IPoolable
    {
        float Damage { get; }
        float Speed { get; }
        DamageType DamageType { get; }
        
        void Initialize(Vector2 direction, float damage, float speed, LayerMask targetLayers);
    }

    /// <summary>
    /// For visual effects
    /// </summary>
    public interface IVisualEffect : IPoolable
    {
        void Play(Vector2 position, float scale = 1f);
        void Stop();
    }

    /// <summary>
    /// For audio playback
    /// </summary>
    public interface IAudioService
    {
        void PlaySFX(string sfxId, Vector2? position = null);
        void PlayMusic(string musicId, bool loop = true);
        void StopMusic();
        void SetMasterVolume(float volume);
        void SetSFXVolume(float volume);
        void SetMusicVolume(float volume);
    }

    /// <summary>
    /// For input handling - supports multiple input sources
    /// </summary>
    public interface IInputProvider
    {
        Vector2 MovementInput { get; }
        Vector2 AimDirection { get; }
        bool IsFiring { get; }
        bool IsSpecialAbility { get; }

        event Action OnFirePressed;
        event Action OnFireReleased;
        event Action OnSpecialAbilityPressed;

        /// <summary>
        /// Fired when player presses weapon slot keys (1-4).
        /// Parameter is slot index (0-3 for LA-1 to LA-4).
        /// </summary>
        event Action<int> OnWeaponSlotSelected;
    }

    // ============================================
    // ENUMS
    // ============================================
    
    public enum DamageType
    {
        Normal,
        Laser,
        Missile,
        Plasma,
        EMP
    }

    public enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Flee,
        Dead
    }

    public enum ShipType
    {
        Fighter,
        Bomber,
        Scout,
        Heavy
    }
}
