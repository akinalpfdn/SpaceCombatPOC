using UnityEngine;
using StarReapers.Combat;
using StarReapers.ScriptableObjects;

namespace StarReapers.AI
{
    /// <summary>
    /// Shared data container for enemy AI states.
    /// Holds all references and runtime data that states need to operate.
    /// Avoids states needing direct access to Enemy internals.
    /// </summary>
    public class EnemyContext
    {
        // Components
        public Transform Transform { get; set; }
        public Rigidbody Rigidbody { get; set; }
        public WeaponController WeaponController { get; set; }

        // Configuration
        public EnemyConfig Config { get; set; }

        // Target
        public Transform Target { get; set; }

        // Spawn position (for patrol)
        public Vector3 SpawnPosition { get; set; }

        // State timers
        public float StateTimer { get; set; }
        public float LastFireTime { get; set; }
        public int StrafeDirection { get; set; } = 1;

        // Patrol
        public Vector3 PatrolPoint { get; set; }

        // Health info (read from Enemy)
        public float HealthPercent { get; set; }
        public bool IsAlive { get; set; }

        // Config shortcuts
        public float DetectionRange => Config?.detectionRange ?? 12f;
        public float AttackRange => Config?.attackRange ?? 8f;
        public float PatrolRadius => Config?.patrolRadius ?? 5f;
        public float MoveSpeed => Config?.maxSpeed ?? 5f;
        public float RotationSpeed => Config?.rotationSpeed ?? 120f;
        public float IdleTime => Config?.idleTime ?? 2f;
        public bool CanFlee => Config?.canFlee ?? false;
        public float FleeHealthThreshold => Config?.fleeHealthThreshold ?? 0.2f;
    }
}
