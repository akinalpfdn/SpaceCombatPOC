using UnityEngine;
using SpaceCombat.Interfaces;

namespace SpaceCombat.ScriptableObjects
{
    [CreateAssetMenu(fileName = "NewWeaponConfig", menuName = "SpaceCombat/Weapon Configuration")]
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