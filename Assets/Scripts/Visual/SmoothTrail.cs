// ============================================
// SmoothTrail.cs
// Buttery smooth engine trail using Line Renderer + Catmull-Rom spline
// Interpolates between recorded positions for smooth curves
// ============================================

using System.Collections.Generic;
using UnityEngine;

namespace StarReapers.Visual
{
    /// <summary>
    /// Creates smooth trails using Line Renderer with Catmull-Rom spline interpolation.
    /// Records positions over time and draws smooth curves between them.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class SmoothTrail : MonoBehaviour
    {
        // ============================================
        // CONFIGURATION
        // ============================================

        [Header("Trail Settings")]
        [SerializeField] private LineRenderer _lineRenderer;

        [Header("Recording")]
        [Tooltip("Trail duration when idle/slow")]
        [SerializeField] private float _minTrailDuration = 0.15f;

        [Tooltip("Trail duration at full speed")]
        [SerializeField] private float _maxTrailDuration = 0.4f;

        [Tooltip("How often to record new positions (seconds)")]
        [SerializeField] private float _recordInterval = 0.01f;

        [Tooltip("Minimum distance between points to avoid overlap")]
        [SerializeField] private float _minPointDistance = 0.05f;

        [Header("Width")]
        [Tooltip("Trail width at the ship")]
        [SerializeField] private float _startWidth = 0.4f;

        [Tooltip("Trail width at the tail")]
        [SerializeField] private float _endWidth = 0.02f;

        [Header("Speed Response")]
        [Tooltip("Ship's max speed for normalization")]
        [SerializeField] private float _maxShipSpeed = 10f;

        [Header("Color")]
        [SerializeField] private Color _startColor = new Color(0.3f, 0.7f, 1f, 1f);
        [SerializeField] private Color _endColor = new Color(0.1f, 0.3f, 0.8f, 0f);

        // ============================================
        // RUNTIME STATE
        // ============================================

        private struct TrailPoint
        {
            public Vector3 Position;
            public float TimeCreated;
        }

        private List<TrailPoint> _points = new List<TrailPoint>();
        private Transform _shipTransform;
        private float _lastRecordTime;
        private Vector3 _lastPosition;
        private float _currentSpeed;
        private float _smoothedSpeed;
        private bool _isInitialized;

        // ============================================
        // UNITY LIFECYCLE
        // ============================================

        private void Awake()
        {
            if (_lineRenderer == null)
            {
                _lineRenderer = GetComponent<LineRenderer>();
            }
        }

        private void Start()
        {
            Initialize();
        }

        private void LateUpdate()
        {
            if (!_isInitialized) return;

            CalculateSpeed();
            RecordPosition();
            RemoveOldPoints();
            UpdateLineRenderer();
        }

        // ============================================
        // INITIALIZATION
        // ============================================

        private void Initialize()
        {
            _shipTransform = transform.parent ?? transform;
            _lastPosition = _shipTransform.position;
            _lastRecordTime = Time.time;

            // Try to get max speed from ShipMovement
            var shipMovement = _shipTransform.GetComponent<StarReapers.Movement.ShipMovement>();
            if (shipMovement != null)
            {
                _maxShipSpeed = shipMovement.MaxSpeed;
            }

            // Configure line renderer for top-down view
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.alignment = LineAlignment.View;
            _lineRenderer.textureMode = LineTextureMode.Stretch;
            _lineRenderer.numCornerVertices = 5;
            _lineRenderer.numCapVertices = 3;

            // Set width curve (cone shape - thick at ship, thin at tail)
            // Line Renderer: position 0 = oldest point (tail), position N = newest (ship)
            // So we need: start thin (tail) -> end thick (ship)
            AnimationCurve widthCurve = new AnimationCurve();
            widthCurve.AddKey(0f, _endWidth / _startWidth);  // Tail = thin
            widthCurve.AddKey(1f, 1f);                        // Ship = thick
            _lineRenderer.widthCurve = widthCurve;
            _lineRenderer.widthMultiplier = _startWidth;

            // Set color gradient (tail = faded, ship = bright)
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(_endColor, 0f),    // Tail = faded
                    new GradientColorKey(_startColor, 1f)  // Ship = bright
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),           // Tail = transparent
                    new GradientAlphaKey(_startColor.a, 1f) // Ship = opaque
                }
            );
            _lineRenderer.colorGradient = gradient;

            _isInitialized = true;
        }

        // ============================================
        // POSITION RECORDING
        // ============================================

        private void CalculateSpeed()
        {
            Vector3 currentPos = _shipTransform.position;
            _currentSpeed = Vector3.Distance(currentPos, _lastPosition) / Time.deltaTime;
            _lastPosition = currentPos;

            // Smooth the speed to avoid jitter
            _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, _currentSpeed, Time.deltaTime * 10f);
        }

        private void RecordPosition()
        {
            // Time-based throttle
            if (Time.time - _lastRecordTime < _recordInterval) return;

            Vector3 currentPos = transform.position;

            // Distance-based check: don't add point if too close to last one
            // This prevents points stacking on top of each other when stopped
            if (_points.Count > 0)
            {
                float distanceToLast = Vector3.Distance(_points[_points.Count - 1].Position, currentPos);
                if (distanceToLast < _minPointDistance)
                {
                    return; // Skip adding point - ship hasn't moved enough
                }
            }

            _points.Add(new TrailPoint
            {
                Position = currentPos,
                TimeCreated = Time.time
            });

            _lastRecordTime = Time.time;
        }

        private void RemoveOldPoints()
        {
            // Trail duration based on smoothed speed - longer when moving fast
            float normalizedSpeed = Mathf.Clamp01(_smoothedSpeed / _maxShipSpeed);
            float currentDuration = Mathf.Lerp(_minTrailDuration, _maxTrailDuration, normalizedSpeed);

            float cutoffTime = Time.time - currentDuration;

            while (_points.Count > 0 && _points[0].TimeCreated < cutoffTime)
            {
                _points.RemoveAt(0);
            }
        }

        // ============================================
        // LINE RENDERER UPDATE
        // ============================================

        private void UpdateLineRenderer()
        {
            Vector3 currentPos = transform.position;

            // Need at least 2 points for a proper line
            if (_points.Count < 2)
            {
                _lineRenderer.positionCount = 0;
                return;
            }

            // Points count + 1 for current position (always track the engine)
            _lineRenderer.positionCount = _points.Count + 1;

            // Set historical points
            for (int i = 0; i < _points.Count; i++)
            {
                _lineRenderer.SetPosition(i, _points[i].Position);
            }

            // Last point is ALWAYS current engine position (no lag)
            _lineRenderer.SetPosition(_points.Count, currentPos);
        }

        // ============================================
        // PUBLIC METHODS
        // ============================================

        /// <summary>
        /// Clears all trail points.
        /// </summary>
        public void ClearTrail()
        {
            _points.Clear();
            _lineRenderer.positionCount = 0;
        }

        /// <summary>
        /// Temporarily disables trail recording.
        /// </summary>
        public void DisableTrail()
        {
            _isInitialized = false;
            ClearTrail();
        }

        /// <summary>
        /// Re-enables trail recording.
        /// </summary>
        public void EnableTrail()
        {
            _isInitialized = true;
            _lastPosition = _shipTransform.position;
        }
    }
}
