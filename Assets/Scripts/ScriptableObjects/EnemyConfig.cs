using UnityEngine;

namespace SpaceCombat.ScriptableObjects
{
    [CreateAssetMenu(fileName = "NewEnemyConfig", menuName = "SpaceCombat/Enemy Configuration")]
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

        [Header("AI Behavior")]
        public float patrolRadius = 5f;
        public float idleTime = 2f;
        public float fleeHealthThreshold = 0.2f;
        public bool canFlee = false;

        [Header("Rewards")]
        public int scoreValue = 100;

        [Header("Audio")]
        public string deathSoundId = "explosion_small";
    }
}