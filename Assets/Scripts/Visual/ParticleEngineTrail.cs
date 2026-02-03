// ============================================
// ParticleEngineTrail.cs
// Smooth particle-based engine trail that responds to ship speed
// Uses Particle System Trails for buttery smooth curves
// ============================================

using UnityEngine;

namespace SpaceCombat.Visual
{
    /// <summary>
    /// Creates a smooth particle-based engine trail.
    /// Unlike TrailRenderer, particle trails interpolate smoothly between positions.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class ParticleEngineTrail : MonoBehaviour
    {
        // ============================================
        // CONFIGURATION
        // ============================================

        [Header("Trail Settings")]
        [SerializeField] private ParticleSystem _particleSystem;

        [Header("Speed Response")]
        [Tooltip("Particles emitted per second at minimum speed")]
        [SerializeField] private float _minEmissionRate = 20f;

        [Tooltip("Particles emitted per second at maximum speed")]
        [SerializeField] private float _maxEmissionRate = 100f;

        [Tooltip("Speed threshold below which trail is hidden")]
        [SerializeField] private float _speedThreshold = 0.5f;

        [Header("Size Settings")]
        [Tooltip("Particle start size at minimum speed")]
        [SerializeField] private float _minStartSize = 0.2f;

        [Tooltip("Particle start size at maximum speed")]
        [SerializeField] private float _maxStartSize = 0.5f;

        [Header("Trail Length")]
        [Tooltip("Particle lifetime at minimum speed")]
        [SerializeField] private float _minLifetime = 0.2f;

        [Tooltip("Particle lifetime at maximum speed (longer = longer trail)")]
        [SerializeField] private float _maxLifetime = 0.8f;

        [Header("Smoothing")]
        [Tooltip("How quickly trail responds to speed changes")]
        [SerializeField] private float _smoothSpeed = 8f;

        // ============================================
        // RUNTIME STATE
        // ============================================

        private Transform _shipTransform;
        private Vector3 _lastPosition;
        private float _currentSpeed;
        private float _maxShipSpeed = 10f;
        private bool _isInitialized;

        private ParticleSystem.EmissionModule _emission;
        private ParticleSystem.MainModule _main;
        private ParticleSystem.TrailModule _trails;

        private float _currentEmissionRate;
        private float _currentStartSize;
        private float _currentLifetime;

        // ============================================
        // UNITY LIFECYCLE
        // ============================================

        private void Awake()
        {
            if (_particleSystem == null)
            {
                _particleSystem = GetComponent<ParticleSystem>();
            }
        }

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            if (!_isInitialized) return;

            CalculateSpeed();
            UpdateTrailProperties();
        }

        // ============================================
        // INITIALIZATION
        // ============================================

        private void Initialize()
        {
            // Get parent ship transform
            _shipTransform = transform.parent;
            if (_shipTransform == null)
            {
                _shipTransform = transform;
            }

            _lastPosition = _shipTransform.position;

            // Try to get max speed from ShipMovement component
            var shipMovement = _shipTransform.GetComponent<SpaceCombat.Movement.ShipMovement>();
            if (shipMovement != null)
            {
                _maxShipSpeed = shipMovement.MaxSpeed;
            }

            // Cache particle system modules
            _emission = _particleSystem.emission;
            _main = _particleSystem.main;
            _trails = _particleSystem.trails;

            // Initialize current values
            _currentEmissionRate = _minEmissionRate;
            _currentStartSize = _minStartSize;
            _currentLifetime = _minLifetime;

            _isInitialized = true;
        }

        // ============================================
        // SPEED CALCULATION
        // ============================================

        private void CalculateSpeed()
        {
            Vector3 currentPosition = _shipTransform.position;
            float distance = Vector3.Distance(currentPosition, _lastPosition);
            _currentSpeed = distance / Time.deltaTime;
            _lastPosition = currentPosition;
        }

        // ============================================
        // TRAIL PROPERTY UPDATES
        // ============================================

        private void UpdateTrailProperties()
        {
            // Normalize speed (0 to 1)
            float normalizedSpeed = Mathf.Clamp01(_currentSpeed / _maxShipSpeed);

            // Check if moving fast enough to show trail
            bool shouldEmit = _currentSpeed > _speedThreshold;

            if (shouldEmit)
            {
                // Calculate target values based on speed
                float targetEmissionRate = Mathf.Lerp(_minEmissionRate, _maxEmissionRate, normalizedSpeed);
                float targetStartSize = Mathf.Lerp(_minStartSize, _maxStartSize, normalizedSpeed);
                float targetLifetime = Mathf.Lerp(_minLifetime, _maxLifetime, normalizedSpeed);

                // Smoothly interpolate
                _currentEmissionRate = Mathf.Lerp(_currentEmissionRate, targetEmissionRate, Time.deltaTime * _smoothSpeed);
                _currentStartSize = Mathf.Lerp(_currentStartSize, targetStartSize, Time.deltaTime * _smoothSpeed);
                _currentLifetime = Mathf.Lerp(_currentLifetime, targetLifetime, Time.deltaTime * _smoothSpeed);

                // Apply to particle system
                _emission.rateOverTime = _currentEmissionRate;
                _main.startSize = _currentStartSize;
                _main.startLifetime = _currentLifetime;

                if (!_emission.enabled)
                {
                    _emission.enabled = true;
                }
            }
            else
            {
                // Fade out emission when stopped
                _currentEmissionRate = Mathf.Lerp(_currentEmissionRate, 0f, Time.deltaTime * _smoothSpeed * 2f);
                _emission.rateOverTime = _currentEmissionRate;

                if (_currentEmissionRate < 1f)
                {
                    _emission.enabled = false;
                }
            }
        }

        // ============================================
        // PUBLIC METHODS
        // ============================================

        /// <summary>
        /// Sets the maximum speed reference for normalization.
        /// </summary>
        public void SetMaxSpeed(float maxSpeed)
        {
            _maxShipSpeed = Mathf.Max(1f, maxSpeed);
        }

        /// <summary>
        /// Temporarily disables the trail.
        /// </summary>
        public void DisableTrail()
        {
            _emission.enabled = false;
            _particleSystem.Clear();
        }

        /// <summary>
        /// Re-enables the trail.
        /// </summary>
        public void EnableTrail()
        {
            _emission.enabled = true;
        }

        /// <summary>
        /// Clears all particles.
        /// </summary>
        public void ClearTrail()
        {
            _particleSystem.Clear();
        }
    }
}
