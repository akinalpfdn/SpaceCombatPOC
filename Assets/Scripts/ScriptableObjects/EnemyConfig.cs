using UnityEngine;

namespace StarReapers.ScriptableObjects
{
    [CreateAssetMenu(fileName = "NewEnemyConfig", menuName = "StarReapers/Enemy Configuration")]
    public class EnemyConfig : ScriptableObject
    {
        [Header("Identity")]
        public string enemyName = "Drone";
        public Sprite enemySprite;

        [Header("Stats")]
        public float maxHealth = 30f;
        public float maxShield = 0f;

        [Header("Movement")]
        public float maxSpeed = 6f;
        public float acceleration = 3f;
        public float rotationSpeed = 120f;

        [Header("Combat")]
        public WeaponConfig weapon;
        public float attackRange = 8f;
        public float detectionRange = 12f;
        public float fireRateMultiplier = 2f; // Higher = slower shooting (1 = normal, 2 = half speed)

        [Header("AI Behavior")]
        public float patrolRadius = 5f;
        public float idleTime = 2f;
        public float fleeHealthThreshold = 0.2f;
        public bool canFlee = false;

        [Header("Rewards")]
        public int scoreValue = 100;

        [Header("UI")]
        [Tooltip("Scale of the target indicator ring (bigger enemies need bigger rings)")]
        public float targetIndicatorScale = 1f;

        [Tooltip("Health bar width and height")]
        public Vector2 healthBarSize = new Vector2(2f, 0.3f);

        [Tooltip("Health bar offset from entity center (3D: X=left/right, Y=up/down, Z=forward/back)")]
        public Vector3 healthBarOffset = new Vector3(0f, 0f, -1f);

        [Header("Audio")]
        public string deathSoundId = "explosion_small";
    }
}