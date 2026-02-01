// ============================================
// SPAWN STRATEGIES - Concrete Strategy Implementations
// Different algorithms for enemy distribution
// ============================================

using System.Collections.Generic;
using UnityEngine;

namespace SpaceCombat.Spawning
{
    // ============================================
    // UNIFORM RANDOM STRATEGY
    // Pure random distribution across entire bounds
    // Good for: General purpose, organic feel
    // ============================================
    
    /// <summary>
    /// Distributes spawns uniformly random across the entire map.
    /// Simple but effective for basic scenarios.
    /// </summary>
    public class UniformRandomSpawnStrategy : SpawnStrategyBase
    {
        public override string StrategyName => "Uniform Random";
        
        protected override Vector3 CalculatePosition(Bounds bounds)
        {
            float x = Random.Range(bounds.min.x, bounds.max.x);
            float z = Random.Range(bounds.min.z, bounds.max.z);
            
            return new Vector3(x, DEFAULT_Y_POSITION, z);
        }
    }
    
    // ============================================
    // GRID DISTRIBUTION STRATEGY
    // Places spawns on a grid with jitter
    // Good for: Even distribution, avoiding clustering
    // ============================================
    
    /// <summary>
    /// Distributes spawns on a grid pattern with random offset (jitter).
    /// Ensures even coverage of the map area.
    /// </summary>
    public class GridDistributionStrategy : SpawnStrategyBase
    {
        private readonly float _jitterAmount;
        private int _currentIndex;
        private Vector3[] _gridPositions;
        
        public override string StrategyName => "Grid Distribution";
        
        public GridDistributionStrategy(float jitterAmount = 0.3f)
        {
            _jitterAmount = Mathf.Clamp01(jitterAmount);
        }
        
        public override Vector3[] GetSpawnPositions(Bounds bounds, int count, Vector3 excludePosition, 
            float minDistance, float minSpacing)
        {
            // Pre-calculate grid
            _gridPositions = CalculateGridPositions(bounds, count);
            _currentIndex = 0;
            
            // Shuffle grid for variety
            ShuffleArray(_gridPositions);
            
            // Filter valid positions
            var validPositions = new List<Vector3>();
            
            foreach (var pos in _gridPositions)
            {
                if (IsValidSpawnPosition(pos, excludePosition, minDistance))
                {
                    validPositions.Add(pos);
                    if (validPositions.Count >= count) break;
                }
            }
            
            // Fill remaining with random if needed
            while (validPositions.Count < count)
            {
                var randomPos = GetSpawnPosition(bounds, excludePosition, minDistance);
                validPositions.Add(randomPos);
            }
            
            return validPositions.ToArray();
        }
        
        protected override Vector3 CalculatePosition(Bounds bounds)
        {
            if (_gridPositions != null && _currentIndex < _gridPositions.Length)
            {
                return _gridPositions[_currentIndex++];
            }
            
            // Fallback to random
            float x = Random.Range(bounds.min.x, bounds.max.x);
            float z = Random.Range(bounds.min.z, bounds.max.z);
            return new Vector3(x, DEFAULT_Y_POSITION, z);
        }
        
        private Vector3[] CalculateGridPositions(Bounds bounds, int count)
        {
            // Calculate grid dimensions to fit count items
            float aspect = bounds.size.x / bounds.size.z;
            int cols = Mathf.CeilToInt(Mathf.Sqrt(count * aspect));
            int rows = Mathf.CeilToInt((float)count / cols);
            
            float cellWidth = bounds.size.x / cols;
            float cellHeight = bounds.size.z / rows;
            float jitterX = cellWidth * _jitterAmount * 0.5f;
            float jitterZ = cellHeight * _jitterAmount * 0.5f;
            
            var positions = new Vector3[cols * rows];
            int index = 0;
            
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    float x = bounds.min.x + (col + 0.5f) * cellWidth + Random.Range(-jitterX, jitterX);
                    float z = bounds.min.z + (row + 0.5f) * cellHeight + Random.Range(-jitterZ, jitterZ);
                    
                    positions[index++] = new Vector3(x, DEFAULT_Y_POSITION, z);
                }
            }
            
            return positions;
        }
        
        private void ShuffleArray<T>(T[] array)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
        }
    }
    
    // ============================================
    // RING DISTRIBUTION STRATEGY
    // Places spawns in concentric rings around center
    // Good for: Arena-style games, waves
    // ============================================
    
    /// <summary>
    /// Distributes spawns in rings at varying distances from center.
    /// Creates natural difficulty progression (closer = more dangerous).
    /// </summary>
    public class RingDistributionStrategy : SpawnStrategyBase
    {
        private readonly int _ringCount;
        private readonly float _innerRadiusRatio;
        
        public override string StrategyName => "Ring Distribution";
        
        public RingDistributionStrategy(int ringCount = 3, float innerRadiusRatio = 0.3f)
        {
            _ringCount = Mathf.Max(1, ringCount);
            _innerRadiusRatio = Mathf.Clamp01(innerRadiusRatio);
        }
        
        public override Vector3[] GetSpawnPositions(Bounds bounds, int count, Vector3 excludePosition, 
            float minDistance, float minSpacing)
        {
            var positions = new List<Vector3>();
            Vector3 center = bounds.center;
            float maxRadius = Mathf.Min(bounds.extents.x, bounds.extents.z);
            float innerRadius = maxRadius * _innerRadiusRatio;
            
            // Distribute count across rings (more in outer rings)
            int[] countsPerRing = DistributeCountsToRings(count);
            
            for (int ring = 0; ring < _ringCount; ring++)
            {
                float t = (float)(ring + 1) / _ringCount;
                float radius = Mathf.Lerp(innerRadius, maxRadius, t);
                int ringCount = countsPerRing[ring];
                
                float angleStep = 360f / ringCount;
                float startAngle = Random.Range(0f, angleStep); // Random offset per ring
                
                for (int i = 0; i < ringCount; i++)
                {
                    float angle = (startAngle + i * angleStep) * Mathf.Deg2Rad;
                    
                    // Add slight radius variation
                    float actualRadius = radius + Random.Range(-radius * 0.1f, radius * 0.1f);
                    
                    float x = center.x + Mathf.Cos(angle) * actualRadius;
                    float z = center.z + Mathf.Sin(angle) * actualRadius;
                    
                    Vector3 pos = new Vector3(x, DEFAULT_Y_POSITION, z);
                    pos = ClampToBounds(pos, bounds);
                    
                    if (IsValidSpawnPosition(pos, excludePosition, minDistance))
                    {
                        positions.Add(pos);
                    }
                }
            }
            
            // Fill remaining with random if needed
            while (positions.Count < count)
            {
                positions.Add(GetSpawnPosition(bounds, excludePosition, minDistance));
            }
            
            return positions.ToArray();
        }
        
        protected override Vector3 CalculatePosition(Bounds bounds)
        {
            Vector3 center = bounds.center;
            float maxRadius = Mathf.Min(bounds.extents.x, bounds.extents.z);
            float innerRadius = maxRadius * _innerRadiusRatio;
            
            // Random ring selection (biased toward outer)
            float ringT = Mathf.Sqrt(Random.value); // Square root for uniform area distribution
            float radius = Mathf.Lerp(innerRadius, maxRadius, ringT);
            
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float x = center.x + Mathf.Cos(angle) * radius;
            float z = center.z + Mathf.Sin(angle) * radius;
            
            return ClampToBounds(new Vector3(x, DEFAULT_Y_POSITION, z), bounds);
        }
        
        private int[] DistributeCountsToRings(int totalCount)
        {
            int[] counts = new int[_ringCount];
            
            // Weight outer rings more heavily (proportional to circumference)
            float totalWeight = 0f;
            float[] weights = new float[_ringCount];
            
            for (int i = 0; i < _ringCount; i++)
            {
                weights[i] = (i + 1); // Linear increase
                totalWeight += weights[i];
            }
            
            int assigned = 0;
            for (int i = 0; i < _ringCount; i++)
            {
                counts[i] = Mathf.RoundToInt((weights[i] / totalWeight) * totalCount);
                assigned += counts[i];
            }
            
            // Adjust for rounding errors
            counts[_ringCount - 1] += totalCount - assigned;
            
            return counts;
        }
    }
    
    // ============================================
    // CLUSTERED STRATEGY
    // Creates groups of enemies in different areas
    // Good for: Exploration, POI-based spawning
    // ============================================
    
    /// <summary>
    /// Distributes spawns in clusters across the map.
    /// Creates pockets of enemies for exploration gameplay.
    /// </summary>
    public class ClusteredSpawnStrategy : SpawnStrategyBase
    {
        private readonly int _clusterCount;
        private readonly float _clusterRadius;
        
        private Vector3[] _clusterCenters;
        private int _currentCluster;
        
        public override string StrategyName => "Clustered";
        
        public ClusteredSpawnStrategy(int clusterCount = 4, float clusterRadius = 15f)
        {
            _clusterCount = Mathf.Max(1, clusterCount);
            _clusterRadius = Mathf.Max(1f, clusterRadius);
        }
        
        public override Vector3[] GetSpawnPositions(Bounds bounds, int count, Vector3 excludePosition, 
            float minDistance, float minSpacing)
        {
            // Generate cluster centers away from player
            _clusterCenters = GenerateClusterCenters(bounds, excludePosition, minDistance);
            
            var positions = new List<Vector3>();
            int perCluster = Mathf.CeilToInt((float)count / _clusterCount);
            
            for (int c = 0; c < _clusterCount && positions.Count < count; c++)
            {
                Vector3 center = _clusterCenters[c];
                int clusterSize = Mathf.Min(perCluster, count - positions.Count);
                
                for (int i = 0; i < clusterSize; i++)
                {
                    // Random position within cluster
                    float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    float distance = Random.Range(0f, _clusterRadius);
                    
                    float x = center.x + Mathf.Cos(angle) * distance;
                    float z = center.z + Mathf.Sin(angle) * distance;
                    
                    Vector3 pos = ClampToBounds(new Vector3(x, DEFAULT_Y_POSITION, z), bounds);
                    
                    if (IsValidSpawnPosition(pos, excludePosition, minDistance))
                    {
                        positions.Add(pos);
                    }
                }
            }
            
            // Fill remaining with random if needed
            while (positions.Count < count)
            {
                _currentCluster = Random.Range(0, _clusterCount);
                positions.Add(GetSpawnPosition(bounds, excludePosition, minDistance));
            }
            
            return positions.ToArray();
        }
        
        protected override Vector3 CalculatePosition(Bounds bounds)
        {
            if (_clusterCenters == null || _clusterCenters.Length == 0)
            {
                // Fallback to uniform random
                return new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    DEFAULT_Y_POSITION,
                    Random.Range(bounds.min.z, bounds.max.z)
                );
            }
            
            Vector3 center = _clusterCenters[_currentCluster % _clusterCenters.Length];
            _currentCluster++;
            
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(0f, _clusterRadius);
            
            float x = center.x + Mathf.Cos(angle) * distance;
            float z = center.z + Mathf.Sin(angle) * distance;
            
            return ClampToBounds(new Vector3(x, DEFAULT_Y_POSITION, z), bounds);
        }
        
        private Vector3[] GenerateClusterCenters(Bounds bounds, Vector3 excludePosition, float minDistance)
        {
            var centers = new List<Vector3>();
            float minClusterSpacing = _clusterRadius * 2.5f;
            
            for (int attempt = 0; attempt < MAX_ATTEMPTS * _clusterCount && centers.Count < _clusterCount; attempt++)
            {
                float x = Random.Range(bounds.min.x + _clusterRadius, bounds.max.x - _clusterRadius);
                float z = Random.Range(bounds.min.z + _clusterRadius, bounds.max.z - _clusterRadius);
                Vector3 candidate = new Vector3(x, DEFAULT_Y_POSITION, z);
                
                // Check distance from player
                if (!IsValidSpawnPosition(candidate, excludePosition, minDistance + _clusterRadius))
                    continue;
                
                // Check distance from other clusters
                bool tooClose = false;
                foreach (var existing in centers)
                {
                    float dx = candidate.x - existing.x;
                    float dz = candidate.z - existing.z;
                    if (dx * dx + dz * dz < minClusterSpacing * minClusterSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }
                
                if (!tooClose)
                {
                    centers.Add(candidate);
                }
            }
            
            // Fallback: distribute evenly if not enough clusters found
            while (centers.Count < _clusterCount)
            {
                float angle = (centers.Count / (float)_clusterCount) * 360f * Mathf.Deg2Rad;
                float radius = Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.6f;
                
                Vector3 pos = new Vector3(
                    bounds.center.x + Mathf.Cos(angle) * radius,
                    DEFAULT_Y_POSITION,
                    bounds.center.z + Mathf.Sin(angle) * radius
                );
                
                centers.Add(pos);
            }
            
            return centers.ToArray();
        }
    }
    
    // ============================================
    // EDGE SPAWN STRATEGY
    // Spawns enemies at map edges
    // Good for: Wave-based survival, incoming threats
    // ============================================
    
    /// <summary>
    /// Distributes spawns along the edges of the map.
    /// Creates feeling of incoming threats from outside.
    /// </summary>
    public class EdgeSpawnStrategy : SpawnStrategyBase
    {
        public enum EdgePreference
        {
            All,
            Horizontal, // Top and bottom
            Vertical,   // Left and right
            FarFromPlayer
        }
        
        private readonly EdgePreference _preference;
        private readonly float _edgeInset;
        
        public override string StrategyName => $"Edge ({_preference})";
        
        public EdgeSpawnStrategy(EdgePreference preference = EdgePreference.All, float edgeInset = 2f)
        {
            _preference = preference;
            _edgeInset = Mathf.Max(0f, edgeInset);
        }
        
        protected override Vector3 CalculatePosition(Bounds bounds)
        {
            // Pick random edge based on preference
            int edge = GetEdgeIndex();
            
            return GetPositionOnEdge(bounds, edge);
        }
        
        private int GetEdgeIndex()
        {
            switch (_preference)
            {
                case EdgePreference.Horizontal:
                    return Random.value > 0.5f ? 0 : 2; // Top or bottom
                case EdgePreference.Vertical:
                    return Random.value > 0.5f ? 1 : 3; // Left or right
                default:
                    return Random.Range(0, 4);
            }
        }
        
        private Vector3 GetPositionOnEdge(Bounds bounds, int edge)
        {
            float x, z;
            
            switch (edge)
            {
                case 0: // Top (max Z)
                    x = Random.Range(bounds.min.x + _edgeInset, bounds.max.x - _edgeInset);
                    z = bounds.max.z - _edgeInset;
                    break;
                case 1: // Right (max X)
                    x = bounds.max.x - _edgeInset;
                    z = Random.Range(bounds.min.z + _edgeInset, bounds.max.z - _edgeInset);
                    break;
                case 2: // Bottom (min Z)
                    x = Random.Range(bounds.min.x + _edgeInset, bounds.max.x - _edgeInset);
                    z = bounds.min.z + _edgeInset;
                    break;
                case 3: // Left (min X)
                default:
                    x = bounds.min.x + _edgeInset;
                    z = Random.Range(bounds.min.z + _edgeInset, bounds.max.z - _edgeInset);
                    break;
            }
            
            return new Vector3(x, DEFAULT_Y_POSITION, z);
        }
        
        public override Vector3 GetSpawnPosition(Bounds bounds, Vector3 excludePosition, float minDistance)
        {
            if (_preference == EdgePreference.FarFromPlayer)
            {
                return GetPositionFarFromPlayer(bounds, excludePosition);
            }
            
            return base.GetSpawnPosition(bounds, excludePosition, minDistance);
        }
        
        private Vector3 GetPositionFarFromPlayer(Bounds bounds, Vector3 playerPos)
        {
            // Determine which edge is furthest from player
            float[] distances = new float[4];
            distances[0] = bounds.max.z - playerPos.z; // Top
            distances[1] = bounds.max.x - playerPos.x; // Right
            distances[2] = playerPos.z - bounds.min.z; // Bottom
            distances[3] = playerPos.x - bounds.min.x; // Left
            
            // Find furthest edge
            int furthestEdge = 0;
            float maxDist = distances[0];
            for (int i = 1; i < 4; i++)
            {
                if (distances[i] > maxDist)
                {
                    maxDist = distances[i];
                    furthestEdge = i;
                }
            }
            
            return GetPositionOnEdge(bounds, furthestEdge);
        }
    }
}
