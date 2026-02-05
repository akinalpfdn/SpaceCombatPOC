// ============================================
// SpeedBasedTrail.cs
// Dynamically adjusts trail length based on ship speed
// Uses Strategy pattern for different trail behaviors
// ============================================

using UnityEngine;
using VContainer;

namespace StarReapers.Visual
{
    /// <summary>
    /// Controls trail renderer properties based on ship movement speed.
    /// Trail becomes longer and more visible at higher speeds.
    /// </summary>
    [RequireComponent(typeof(TrailRenderer))]
    public class SpeedBasedTrail : MonoBehaviour
    {
        // ============================================
        // CONFIGURATION
        // ============================================

        [Header("Trail Settings")]
        [SerializeField] private TrailRenderer _trailRenderer;

        [Header("Speed Response")]
        [Tooltip("Minimum trail time when moving slowly")]
        [SerializeField] private float _minTrailTime = 0.3f;

        [Tooltip("Maximum trail time at full speed")]
        [SerializeField] private float _maxTrailTime = 1.5f;

        [Tooltip("Speed threshold below which trail is hidden")]
        [SerializeField] private float _speedThreshold = 0.1f;

        [Header("Width Settings")]
        [Tooltip("Trail start width at minimum speed")]
        [SerializeField] private float _minStartWidth = 0.3f;

        [Tooltip("Trail start width at maximum speed")]
        [SerializeField] private float _maxStartWidth = 0.6f;

        [Tooltip("Trail end width (tip of the trail)")]
        [SerializeField] private float _endWidth = 0.02f;

        [Header("Smoothing")]
        [Tooltip("How quickly trail responds to speed changes")]
        [SerializeField] private float _smoothSpeed = 5f;

        // ============================================
        // RUNTIME STATE
        // ============================================

        private Transform _shipTransform;
        private Vector3 _lastPosition;
        private float _currentSpeed;
        private float _targetTrailTime;
        private float _targetStartWidth;
        private float _maxShipSpeed = 10f;
        private bool _isInitialized;

        // ============================================
        // UNITY LIFECYCLE
        // ============================================

        private void Awake()
        {
            if (_trailRenderer == null)
            {
                _trailRenderer = GetComponent<TrailRenderer>();
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
            var shipMovement = _shipTransform.GetComponent<StarReapers.Movement.ShipMovement>();
            if (shipMovement != null)
            {
                _maxShipSpeed = shipMovement.MaxSpeed;
            }

            // Configure trail for smooth curves
            _trailRenderer.minVertexDistance = 0.01f;  // Very smooth curves
            _trailRenderer.numCornerVertices = 10;     // Very round corners
            _trailRenderer.numCapVertices = 5;         // Smooth ends
            _trailRenderer.endWidth = _endWidth;       // Cone shape - thin at end
            _trailRenderer.textureMode = LineTextureMode.Stretch;

            _trailRenderer.emitting = true;
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

            // Always emit - trail naturally fades based on time
            if (!_trailRenderer.emitting)
            {
                _trailRenderer.emitting = true;
            }

            // Calculate target values based on speed
            _targetTrailTime = Mathf.Lerp(_minTrailTime, _maxTrailTime, normalizedSpeed);
            _targetStartWidth = Mathf.Lerp(_minStartWidth, _maxStartWidth, normalizedSpeed);

            // Smoothly interpolate to target values
            _trailRenderer.time = Mathf.Lerp(_trailRenderer.time, _targetTrailTime, Time.deltaTime * _smoothSpeed);
            _trailRenderer.startWidth = Mathf.Lerp(_trailRenderer.startWidth, _targetStartWidth, Time.deltaTime * _smoothSpeed);
            _trailRenderer.endWidth = _endWidth;
        }

        // ============================================
        // PUBLIC METHODS
        // ============================================

        /// <summary>
        /// Sets the maximum speed reference for normalization.
        /// Called externally if ship config changes.
        /// </summary>
        public void SetMaxSpeed(float maxSpeed)
        {
            _maxShipSpeed = Mathf.Max(1f, maxSpeed);
        }

        /// <summary>
        /// Temporarily disables the trail (e.g., during teleport).
        /// </summary>
        public void DisableTrail()
        {
            _trailRenderer.emitting = false;
            _trailRenderer.Clear();
        }

        /// <summary>
        /// Re-enables the trail after being disabled.
        /// </summary>
        public void EnableTrail()
        {
            // Trail will auto-enable on next movement
        }

        /// <summary>
        /// Clears any existing trail positions.
        /// Useful when repositioning the ship.
        /// </summary>
        public void ClearTrail()
        {
            _trailRenderer.Clear();
        }
    }
}
