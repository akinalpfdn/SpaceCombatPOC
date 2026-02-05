using UnityEngine;
using StarReapers.Interfaces;

namespace StarReapers.ScriptableObjects
{
    [CreateAssetMenu(fileName = "NewWeaponConfig", menuName = "StarReapers/Weapon Configuration")]
    public class WeaponConfig : ScriptableObject
    {
        [Header("Identity")]
        public string weaponName = "Laser";
        public DamageType damageType = DamageType.Laser;

        [Header("Stats")]
        public float damage = 10f;
        public float fireRate = 0.2f;
        public float projectileSpeed = 30f;
        public float range = 15f;
        public float accuracy = 0.95f;

        [Header("Burst Fire")]
        [Tooltip("Number of shots per burst. 1 = no burst, 3 = DarkOrbit-style triple shot")]
        [Range(1, 10)]
        public int burstCount = 1;

        [Tooltip("Delay between each shot in a burst (seconds). Lower = faster burst.")]
        [Range(0.01f, 0.2f)]
        public float burstDelay = 0.05f;

        /// <summary>
        /// Whether this weapon uses burst fire mode.
        /// </summary>
        public bool IsBurstFire => burstCount > 1;

        /// <summary>
        /// Damage per individual projectile in a burst.
        /// Total damage is split across burst shots.
        /// </summary>
        public float DamagePerBurstShot => burstCount > 1 ? damage / burstCount : damage;

        [Header("Projectile")]
        public GameObject projectilePrefab;
        public int projectilesPerShot = 1;
        public float spreadAngle = 0f;

        [Header("Visual")]
        public GameObject muzzleFlashPrefab;
        public Color projectileColor = Color.red;
        public float projectileScale = 1f;

        [Tooltip("Mesh-based visual config. When assigned, overrides sprite-based rendering with 3D mesh + trail.")]
        public ProjectileVisualConfig projectileVisualConfig;

        [Header("Audio")]
        public string fireSoundId = "laser_fire";
        public string hitSoundId = "laser_hit";
    }
}