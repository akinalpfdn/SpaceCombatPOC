using UnityEngine;

namespace StarReapers.ScriptableObjects
{
    [CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "StarReapers/Game Balance")]
    public class GameBalanceConfig : ScriptableObject
    {
        [Header("Wave Settings")]
        public int baseEnemiesPerWave = 5;
        public int additionalEnemiesPerWave = 2;
        public float timeBetweenWaves = 5f;
        public float timeBetweenSpawns = 1f;

        [Header("Player Settings")]
        public float playerRespawnTime = 3f;
        public int startingLives = 3;
        public float invincibilityTime = 2f;
    }
}