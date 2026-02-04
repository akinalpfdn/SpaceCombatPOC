// ============================================
// SPAWN DEBUGGER - Editor tool for testing spawn distribution
// Visualizes spawn positions and strategy behavior
// Uses new Input System (UnityEngine.InputSystem)
// ============================================

using SpaceCombat.Interfaces;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceCombat.Spawning
{
    /// <summary>
    /// Debug tool for visualizing and testing spawn strategies.
    /// Only active in Editor or Development builds.
    /// 
    /// Usage:
    /// 1. Add to a GameObject in scene
    /// 2. Assign SpawnConfig
    /// 3. Set test parameters
    /// 4. Click "Test Spawn" in Inspector or press T in Play mode
    /// </summary>
    public class SpawnDebugger : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private SpawnConfig _config;
        [SerializeField] private Environment.MapBounds _mapBounds;

        [Header("Test Parameters")]
        [SerializeField] private int _testSpawnCount = 10;
        [SerializeField] private Vector3 _testPlayerPosition = Vector3.zero;
        [SerializeField] private float _testMinDistance = 20f;
        [SerializeField] private float _testMinSpacing = 5f;

        [Header("Visualization")]
        [SerializeField] private bool _showDebugGizmos = true;
        [SerializeField] private float _positionMarkerSize = 1f;
        [SerializeField] private Color _spawnPositionColor = Color.red;
        [SerializeField] private Color _playerExclusionColor = new Color(1f, 0f, 0f, 0.2f);
        [SerializeField] private Color _boundsColor = Color.green;

        [Header("Runtime State")]
        [SerializeField] private List<Vector3> _debugSpawnPositions = new List<Vector3>();

        private ISpawnStrategy _testStrategy;
        private Bounds _testBounds;

#if UNITY_EDITOR || DEVELOPMENT_BUILD

        private void Update()
        {
            if (Keyboard.current == null) return;

            // Press T to test spawn positions
            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                TestSpawnPositions();
            }

            // Press C to clear debug positions
            if (Keyboard.current.cKey.wasPressedThisFrame)
            {
                ClearDebugPositions();
            }
        }
        
        /// <summary>
        /// Test spawn position generation with current settings.
        /// </summary>
        [ContextMenu("Test Spawn Positions")]
        public void TestSpawnPositions()
        {
            // Get bounds
            InitializeTestBounds();
            
            // Create strategy from config
            _testStrategy = _config != null 
                ? SpawnStrategyFactory.CreateFromConfig(_config)
                : new UniformRandomSpawnStrategy();
            
            // Use config values if available
            float minDistance = _config?.MinDistanceFromPlayer ?? _testMinDistance;
            float minSpacing = _config?.MinSpacingBetweenEnemies ?? _testMinSpacing;
            int count = _config?.InitialEnemyCount ?? _testSpawnCount;
            
            // Generate positions
            _debugSpawnPositions.Clear();
            
            Vector3[] positions = _testStrategy.GetSpawnPositions(
                _testBounds,
                count,
                _testPlayerPosition,
                minDistance,
                minSpacing
            );
            
            _debugSpawnPositions.AddRange(positions);
            
            // Log statistics
            LogSpawnStatistics(positions, minDistance, minSpacing);
        }
        
        /// <summary>
        /// Clear debug visualization.
        /// </summary>
        [ContextMenu("Clear Debug Positions")]
        public void ClearDebugPositions()
        {
            _debugSpawnPositions.Clear();
        }
        
        /// <summary>
        /// Test all strategy types for comparison.
        /// </summary>
        [ContextMenu("Compare All Strategies")]
        public void CompareAllStrategies()
        {
            InitializeTestBounds();
            
            var strategies = new (SpawnDistributionType type, ISpawnStrategy strategy)[]
            {
                (SpawnDistributionType.UniformRandom, new UniformRandomSpawnStrategy()),
                (SpawnDistributionType.Grid, new GridDistributionStrategy()),
                (SpawnDistributionType.Ring, new RingDistributionStrategy()),
                (SpawnDistributionType.Clustered, new ClusteredSpawnStrategy()),
                (SpawnDistributionType.Edge, new EdgeSpawnStrategy())
            };
            
            float minDistance = _config?.MinDistanceFromPlayer ?? _testMinDistance;
            float minSpacing = _config?.MinSpacingBetweenEnemies ?? _testMinSpacing;
            int count = _config?.InitialEnemyCount ?? _testSpawnCount;
            
            Debug.Log("=== SPAWN STRATEGY COMPARISON ===");
            Debug.Log($"Test Parameters: Count={count}, MinDistance={minDistance}, MinSpacing={minSpacing}");
            Debug.Log($"Bounds: Center={_testBounds.center}, Size={_testBounds.size}");
            Debug.Log("");
            
            foreach (var (type, strategy) in strategies)
            {
                var positions = strategy.GetSpawnPositions(
                    _testBounds,
                    count,
                    _testPlayerPosition,
                    minDistance,
                    minSpacing
                );
                
                AnalyzeDistribution(type.ToString(), positions, minDistance, minSpacing);
            }
        }
        
        private void InitializeTestBounds()
        {
            if (_mapBounds != null)
            {
                _testBounds = _mapBounds.Bounds3D;
            }
            else if (_config != null && _config.UseCustomBounds)
            {
                _testBounds = new Bounds(_config.CustomBoundsCenter, _config.CustomBoundsSize);
            }
            else
            {
                // Default large bounds
                _testBounds = new Bounds(Vector3.zero, new Vector3(200f, 0f, 200f));
            }
        }
        
        private void LogSpawnStatistics(Vector3[] positions, float minDistance, float minSpacing)
        {
            if (positions.Length == 0)
            {
                Debug.Log("[SpawnDebugger] No positions generated!");
                return;
            }
            
            string strategyName = _testStrategy?.StrategyName ?? "Unknown";
            
            Debug.Log($"=== Spawn Test: {strategyName} ===");
            Debug.Log($"Generated {positions.Length} positions");
            
            // Check distances from player
            int tooCloseToPlayer = 0;
            float minPlayerDist = float.MaxValue;
            float maxPlayerDist = 0f;
            float avgPlayerDist = 0f;
            
            foreach (var pos in positions)
            {
                float dist = Vector3.Distance(new Vector3(pos.x, 0, pos.z), 
                    new Vector3(_testPlayerPosition.x, 0, _testPlayerPosition.z));
                
                if (dist < minDistance) tooCloseToPlayer++;
                minPlayerDist = Mathf.Min(minPlayerDist, dist);
                maxPlayerDist = Mathf.Max(maxPlayerDist, dist);
                avgPlayerDist += dist;
            }
            avgPlayerDist /= positions.Length;
            
            Debug.Log($"Player Distance - Min: {minPlayerDist:F1}, Max: {maxPlayerDist:F1}, Avg: {avgPlayerDist:F1}");
            if (tooCloseToPlayer > 0)
            {
                Debug.LogWarning($"WARNING: {tooCloseToPlayer} positions too close to player (< {minDistance})");
            }
            
            // Check spacing between spawns
            int tooCloseToEachOther = 0;
            float minEnemyDist = float.MaxValue;
            
            for (int i = 0; i < positions.Length; i++)
            {
                for (int j = i + 1; j < positions.Length; j++)
                {
                    float dist = Vector3.Distance(
                        new Vector3(positions[i].x, 0, positions[i].z),
                        new Vector3(positions[j].x, 0, positions[j].z)
                    );
                    
                    if (dist < minSpacing) tooCloseToEachOther++;
                    minEnemyDist = Mathf.Min(minEnemyDist, dist);
                }
            }
            
            Debug.Log($"Enemy Spacing - Min: {minEnemyDist:F1} (required: {minSpacing})");
            if (tooCloseToEachOther > 0)
            {
                Debug.LogWarning($"WARNING: {tooCloseToEachOther} position pairs too close (< {minSpacing})");
            }
            
            // Coverage analysis
            Vector3 centroid = Vector3.zero;
            foreach (var pos in positions)
            {
                centroid += pos;
            }
            centroid /= positions.Length;
            
            float spread = 0f;
            foreach (var pos in positions)
            {
                spread += Vector3.Distance(pos, centroid);
            }
            spread /= positions.Length;
            
            Debug.Log($"Distribution - Centroid: {centroid}, Avg Spread: {spread:F1}");
            Debug.Log("================================");
        }
        
        private void AnalyzeDistribution(string strategyName, Vector3[] positions, float minDistance, float minSpacing)
        {
            float minPlayerDist = float.MaxValue;
            float avgPlayerDist = 0f;
            float minEnemyDist = float.MaxValue;
            int violations = 0;
            
            foreach (var pos in positions)
            {
                float dist = Vector3.Distance(new Vector3(pos.x, 0, pos.z), 
                    new Vector3(_testPlayerPosition.x, 0, _testPlayerPosition.z));
                
                minPlayerDist = Mathf.Min(minPlayerDist, dist);
                avgPlayerDist += dist;
                if (dist < minDistance) violations++;
            }
            avgPlayerDist /= positions.Length;
            
            for (int i = 0; i < positions.Length; i++)
            {
                for (int j = i + 1; j < positions.Length; j++)
                {
                    float dist = Vector3.Distance(
                        new Vector3(positions[i].x, 0, positions[i].z),
                        new Vector3(positions[j].x, 0, positions[j].z)
                    );
                    minEnemyDist = Mathf.Min(minEnemyDist, dist);
                }
            }
            
            Debug.Log($"{strategyName}: MinPlayer={minPlayerDist:F1}, AvgPlayer={avgPlayerDist:F1}, MinEnemy={minEnemyDist:F1}, Violations={violations}");
        }
        
        private void OnDrawGizmos()
        {
            if (!_showDebugGizmos) return;
            
            // Draw bounds
            InitializeTestBounds();
            Gizmos.color = _boundsColor;
            Gizmos.DrawWireCube(_testBounds.center, _testBounds.size + Vector3.up);
            
            // Draw player exclusion zone
            Gizmos.color = _playerExclusionColor;
            DrawCircleXZ(_testPlayerPosition, _testMinDistance, 32);
            
            // Draw player position
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(_testPlayerPosition, _positionMarkerSize * 2f);
            
            // Draw spawn positions
            Gizmos.color = _spawnPositionColor;
            foreach (var pos in _debugSpawnPositions)
            {
                Gizmos.DrawSphere(pos, _positionMarkerSize);
                
                // Draw line to player to visualize distance
                Gizmos.color = new Color(_spawnPositionColor.r, _spawnPositionColor.g, _spawnPositionColor.b, 0.3f);
                Gizmos.DrawLine(pos, _testPlayerPosition);
                Gizmos.color = _spawnPositionColor;
            }
            
            // Draw connections between nearby spawns (for spacing visualization)
            if (_debugSpawnPositions.Count > 1)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
                for (int i = 0; i < _debugSpawnPositions.Count; i++)
                {
                    for (int j = i + 1; j < _debugSpawnPositions.Count; j++)
                    {
                        float dist = Vector3.Distance(_debugSpawnPositions[i], _debugSpawnPositions[j]);
                        if (dist < _testMinSpacing * 2f)
                        {
                            Gizmos.DrawLine(_debugSpawnPositions[i], _debugSpawnPositions[j]);
                        }
                    }
                }
            }
        }
        
        private void DrawCircleXZ(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(radius, 0f, 0f);
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }
        }
        
#endif
    }
}