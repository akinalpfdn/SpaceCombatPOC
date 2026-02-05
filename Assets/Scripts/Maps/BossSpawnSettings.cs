// ============================================
// BOSS SPAWN SETTINGS - Configuration for boss enemy spawning
// Defines when and where the boss spawns on each map
// ============================================

using System;
using UnityEngine;
using StarReapers.Entities;

namespace StarReapers.Maps
{
    /// <summary>
    /// Defines which corner of the map the boss will spawn in.
    /// </summary>
    public enum SpawnCorner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Random
    }

    /// <summary>
    /// Defines what triggers the boss to spawn.
    /// </summary>
    public enum BossSpawnTrigger
    {
        /// <summary>Boss spawns immediately when the map starts.</summary>
        None,

        /// <summary>Boss spawns after X seconds into the map.</summary>
        Timer,

        /// <summary>Boss spawns after killing X enemies.</summary>
        KillCount,

        /// <summary>Boss spawns when a specific wave is completed (future use).</summary>
        WaveComplete,

        /// <summary>Boss spawns via external trigger (events, scripted moments).</summary>
        Manual
    }

    /// <summary>
    /// Configuration for boss enemy spawning in a map.
    ///
    /// The boss is a special enemy that:
    /// - Spawns at a specific corner of the map
    /// - Has a trigger condition for when to spawn
    /// - Only one boss is active at a time
    /// </summary>
    [Serializable]
    public class BossSpawnSettings
    {
        [Tooltip("Enable boss spawning for this map")]
        public bool enabled = true;

        [Tooltip("The boss enemy prefab to spawn")]
        public Enemy bossPrefab;

        [Tooltip("Which corner of the map the boss spawns in")]
        public SpawnCorner spawnCorner = SpawnCorner.Random;

        [Tooltip("What triggers the boss to spawn")]
        public BossSpawnTrigger spawnTrigger = BossSpawnTrigger.Timer;

        [Tooltip("Trigger value: seconds for Timer, kill count for KillCount, wave number for WaveComplete")]
        [Range(1f, 600f)]
        public float triggerValue = 60f;

        [Tooltip("Distance from map corner for spawn position")]
        [Range(5f, 30f)]
        public float cornerOffset = 15f;

        /// <summary>
        /// Validates the settings have required references when enabled.
        /// </summary>
        public bool IsValid => !enabled || bossPrefab != null;
    }
}
