// ============================================
// SHIP MOVEMENT - Strategy Pattern Ready
// Handles all movement logic independently
// Can be swapped for different movement behaviors
// ============================================

using UnityEngine;
using StarReapers.Interfaces;
using StarReapers.Environment;

namespace StarReapers.Movement
{
    /// <summary>
    /// Handles ship movement with smooth acceleration/deceleration
    /// DarkOrbit-style movement with momentum
    /// 3D Version - Movement on XZ plane
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ShipMovement : MonoBehaviour, IMovable
    {
        // Movement values - Set by ShipConfig via Initialize()
        private float _maxSpeed;
        private float _acceleration;
        private float _deceleration;
        private float _rotationSpeed;

        [Header("Movement Smoothing")]
        [SerializeField] private float _velocitySmoothing = 0.1f;
        [SerializeField] private bool _rotateToMovement = true;
        [SerializeField] private float _rotationThreshold = 0.1f;

        [Header("Banking/Tilt (2.5D Effect)")]
        [SerializeField] private bool _enableBanking = true;
        [SerializeField] private float _maxTiltAngle = 30f;  // Maximum tilt in degrees
        [SerializeField] private float _tiltSmoothing = 5f;   // How fast to tilt
        [SerializeField] private float _tiltPower = 2.5f;     // Tilt intensity multiplier (higher = more aggressive)

        [Header("Map Bounds")]
        [SerializeField] private bool _respectMapBounds = true;
        [SerializeField] private MapBounds _mapBounds;

        // Components
        private Rigidbody _rigidbody;

        // State
        private Vector3 _currentVelocity;
        private Vector3 _targetVelocity;
        private Vector3 _velocityRef;
        private float _currentSpeed;
        private bool _autoRotateEnabled = true;

        // Banking state
        private float _currentTilt;
        private float _targetTilt;
        private float _smoothedTargetTilt;      // Extra smoothing layer to prevent scatter
        private float _targetTiltVelocity;      // For SmoothDamp
        private Quaternion _lastRotation;
        private float _angularVelocity;
        private float _pendingTurnAngle;  // Track how much we're turning this frame

        // Tilt smoothing settings
        private const float TILT_SMOOTH_TIME = 0.15f;        // How fast target tilt responds
        private const float TILT_DEAD_ZONE = 0.5f;           // Ignore angle changes smaller than this

        // Properties
        public Vector2 Velocity => new Vector2(_rigidbody?.linearVelocity.x ?? 0, _rigidbody?.linearVelocity.z ?? 0);
        public float MaxSpeed => _maxSpeed;
        public float CurrentSpeedNormalized => _currentSpeed / _maxSpeed;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();

            // Configure rigidbody for top-down 3D (XZ plane)
            _rigidbody.useGravity = false;
            _rigidbody.angularDamping = 5f;
            _rigidbody.linearDamping = 0f;
            // Lock Y position and X rotation for 2.5D movement
            // Z rotation is left free for banking/tilt effect
            _rigidbody.constraints = RigidbodyConstraints.FreezePositionY |
                                     RigidbodyConstraints.FreezeRotationX;

            // Smooth movement - interpolate between physics frames
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

            // Initialize banking state
            _lastRotation = transform.rotation;

            // Try to find map bounds if not assigned in inspector
            if (_respectMapBounds && _mapBounds == null)
            {
                _mapBounds = Object.FindFirstObjectByType<MapBounds>();
            }
        }

        /// <summary>
        /// Initialize with custom values
        /// </summary>
        public void Initialize(float maxSpeed, float acceleration, float deceleration, float rotationSpeed)
        {
            _maxSpeed = maxSpeed;
            _acceleration = acceleration;
            _deceleration = deceleration;
            _rotationSpeed = rotationSpeed;
        }

        /// <summary>
        /// Apply movement input (call in FixedUpdate)
        /// 3D Version - Converts XY input to XZ plane movement
        /// </summary>
        public void ApplyMovement(Vector2 input)
        {
            // Convert 2D input to 3D XZ plane
            Vector3 input3D = new Vector3(input.x, 0, input.y);

            // Calculate target velocity based on input
            if (input.magnitude > 0.1f)
            {
                input = input.normalized;
                _targetVelocity = new Vector3(input.x, 0, input.y) * _maxSpeed;

                // Accelerate towards target
                _currentVelocity = Vector3.SmoothDamp(
                    _currentVelocity,
                    _targetVelocity,
                    ref _velocityRef,
                    _velocitySmoothing,
                    _acceleration * _maxSpeed
                );
            }
            else
            {
                // Decelerate when no input
                _currentVelocity = Vector3.MoveTowards(
                    _currentVelocity,
                    Vector3.zero,
                    _deceleration * Time.fixedDeltaTime
                );
            }

            // Apply velocity
            _rigidbody.linearVelocity = _currentVelocity;
            _currentSpeed = _currentVelocity.magnitude;

            // Clamp position to map bounds
            if (_respectMapBounds && _mapBounds != null)
            {
                Vector3 clampedPos = _mapBounds.ClampPosition3D(_rigidbody.position);
                _rigidbody.position = clampedPos;
            }

            // Calculate rotation and banking together (prevents jitter)
            // IMPORTANT: Only handle rotation when auto-rotate is enabled
            // When targeting (auto-rotate disabled), TargetSelector.RotateTowards handles rotation
            if (_autoRotateEnabled)
            {
                if (_rotateToMovement && _currentSpeed > _rotationThreshold)
                {
                    Vector3 moveDir = _currentVelocity.normalized;
                    Vector2 moveDir2D = new Vector2(moveDir.x, moveDir.z);
                    CalculateAndApplyRotation(moveDir2D);
                }
                else if (_enableBanking)
                {
                    // Still smooth out tilt when not rotating
                    _targetTilt = 0;
                    SmoothTilt();
                }
            }
            // When auto-rotate is disabled, don't touch rotation at all
            // RotateTowards() handles everything including banking
        }

            /// <summary>
        /// Calculate rotation angle and apply both rotation + banking together
        /// This prevents jitter by calculating turn amount before applying it
        /// Called from FixedUpdate (movement-based rotation)
        /// </summary>
        private void CalculateAndApplyRotation(Vector2 direction)
        {
            if (direction.magnitude < 0.1f) return;

            // Convert 2D direction to 3D XZ plane
            Vector3 targetDir = new Vector3(direction.x, 0, direction.y);
            Quaternion targetRotation = Quaternion.LookRotation(targetDir);

            // Calculate how much we need to turn this frame
            float currentY = _rigidbody.rotation.eulerAngles.y;  // Read from Rigidbody
            float targetY = targetRotation.eulerAngles.y;
            float angleDiff = Mathf.DeltaAngle(currentY, targetY);

            // Limit rotation to what's possible this frame
            float maxTurnThisFrame = _rotationSpeed * Time.fixedDeltaTime;
            if (maxTurnThisFrame < 0.001f) return;  // Safety check

            float actualTurn = Mathf.Clamp(angleDiff, -maxTurnThisFrame, maxTurnThisFrame);

            // Calculate target tilt based on the turn we're about to make
            if (_enableBanking)
            {
                // Dead zone: Ignore very small angle changes
                if (Mathf.Abs(angleDiff) < TILT_DEAD_ZONE)
                {
                    _targetTilt = 0f;
                }
                else
                {
                    _targetTilt = -actualTurn * _maxTiltAngle * _tiltPower / maxTurnThisFrame;
                    _targetTilt = Mathf.Clamp(_targetTilt, -_maxTiltAngle, _maxTiltAngle);
                }

                // Extra smoothing layer for target tilt
                _smoothedTargetTilt = Mathf.SmoothDamp(
                    _smoothedTargetTilt,
                    _targetTilt,
                    ref _targetTiltVelocity,
                    TILT_SMOOTH_TIME
                );

                // Smooth interpolation from current to smoothed target
                _currentTilt = Mathf.Lerp(
                    _currentTilt,
                    _smoothedTargetTilt,
                    _tiltSmoothing * Time.fixedDeltaTime
                );
            }

            // Apply rotation using Rigidbody.MoveRotation (works properly with physics)
            float newY = currentY + actualTurn;
            Quaternion newRotation = Quaternion.Euler(0, newY, _currentTilt);
            _rigidbody.MoveRotation(newRotation);
            _lastRotation = newRotation;
        }

        /// <summary>
        /// Smoothly interpolate current tilt to target tilt
        /// Uses the extra smoothing layer to prevent scatter
        /// </summary>
        private void SmoothTilt()
        {
            // Extra smoothing layer for target tilt
            _smoothedTargetTilt = Mathf.SmoothDamp(
                _smoothedTargetTilt,
                _targetTilt,
                ref _targetTiltVelocity,
                TILT_SMOOTH_TIME
            );

            _currentTilt = Mathf.Lerp(
                _currentTilt,
                _smoothedTargetTilt,
                _tiltSmoothing * Time.fixedDeltaTime
            );

            // Apply current tilt without changing Y rotation (using Rigidbody)
            float currentY = _rigidbody.rotation.eulerAngles.y;
            Quaternion newRotation = Quaternion.Euler(0, currentY, _currentTilt);
            _rigidbody.MoveRotation(newRotation);
            _lastRotation = newRotation;
        }

        /// <summary>
        /// Apply banking/tilt effect based on rotation
        /// 2.5D Effect - Ship tilts into turns (like aircraft)
        /// </summary>
        private void ApplyBanking()
        {
            // Calculate angular velocity (how fast we're turning)
            float angleDelta = Mathf.DeltaAngle(_lastRotation.eulerAngles.y, transform.eulerAngles.y);
            _angularVelocity = angleDelta / Time.fixedDeltaTime;
            _lastRotation = transform.rotation;

            // Calculate target tilt based on angular velocity
            // Turning left (negative angular velocity) = tilt left (negative Z rotation)
            // Turning right (positive angular velocity) = tilt right (positive Z rotation)
            // TiltPower multiplies the effect for more aggressive banking
            _targetTilt = -_angularVelocity * _maxTiltAngle * _tiltPower / _rotationSpeed;

            // Clamp tilt to maximum angle
            _targetTilt = Mathf.Clamp(_targetTilt, -_maxTiltAngle, _maxTiltAngle);

            // Smoothly interpolate current tilt to target tilt
            _currentTilt = Mathf.Lerp(
                _currentTilt,
                _targetTilt,
                _tiltSmoothing * Time.fixedDeltaTime
            );

            // Apply tilt as local Z rotation
            // Get current rotation (only Y axis for facing direction)
            Vector3 currentEuler = transform.rotation.eulerAngles;
            // Set Y rotation, apply Z tilt, keep X at 0
            transform.rotation = Quaternion.Euler(0, currentEuler.y, _currentTilt);
        }

        /// <summary>
        /// Move in a direction (IMovable implementation)
        /// </summary>
        public void Move(Vector2 direction)
        {
            ApplyMovement(direction);
        }

        /// <summary>
        /// Set maximum speed
        /// </summary>
        public void SetSpeed(float speed)
        {
            _maxSpeed = Mathf.Max(0, speed);
        }

        /// <summary>
        /// Set maximum speed
        /// </summary>
        public void SetMaxSpeed(float speed)
        {
            _maxSpeed = Mathf.Max(0, speed);
        }

        /// <summary>
        /// Stop all movement
        /// </summary>
        public void Stop()
        {
            _currentVelocity = Vector3.zero;
            _targetVelocity = Vector3.zero;
            _rigidbody.linearVelocity = Vector3.zero;
            _currentSpeed = 0;
        }

        /// <summary>
        /// Rotate ship to face a direction
        /// 3D Version - Rotates around Y axis to face direction on XZ plane
        /// Applies banking/tilt effect when enabled
        /// </summary>
        public void RotateTowards(Vector2 direction)
        {
            if (direction.magnitude < 0.1f) return;
            if (_rigidbody == null) return;

            // Convert 2D direction to 3D XZ plane
            Vector3 targetDir = new Vector3(direction.x, 0, direction.y);
            if (targetDir == Vector3.zero) return;

            Quaternion targetRotation = Quaternion.LookRotation(targetDir);

            // Use Time.deltaTime since this is called from Update (TargetSelector)
            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f) return;

            // Ensure rotation speed has a sensible minimum (fallback if not initialized)
            float rotSpeed = _rotationSpeed > 0f ? _rotationSpeed : 180f;

            // Calculate turn amount
            float currentY = _rigidbody.rotation.eulerAngles.y;  // Read from Rigidbody, not transform
            float targetY = targetRotation.eulerAngles.y;
            float angleDiff = Mathf.DeltaAngle(currentY, targetY);

            // Limit rotation to what's possible this frame
            float maxTurnThisFrame = rotSpeed * deltaTime;
            float actualTurn = Mathf.Clamp(angleDiff, -maxTurnThisFrame, maxTurnThisFrame);

            // Calculate new Y rotation
            float newY = currentY + actualTurn;

            // Calculate tilt if banking is enabled
            float tiltToApply = 0f;
            if (_enableBanking && maxTurnThisFrame > 0.001f)
            {
                if (Mathf.Abs(angleDiff) < TILT_DEAD_ZONE)
                {
                    _targetTilt = 0f;
                }
                else
                {
                    _targetTilt = -actualTurn * _maxTiltAngle * _tiltPower / maxTurnThisFrame;
                    _targetTilt = Mathf.Clamp(_targetTilt, -_maxTiltAngle, _maxTiltAngle);
                }

                // Extra smoothing layer
                _smoothedTargetTilt = Mathf.SmoothDamp(
                    _smoothedTargetTilt,
                    _targetTilt,
                    ref _targetTiltVelocity,
                    TILT_SMOOTH_TIME
                );

                _currentTilt = Mathf.Lerp(
                    _currentTilt,
                    _smoothedTargetTilt,
                    _tiltSmoothing * deltaTime
                );

                tiltToApply = _currentTilt;
            }

            // Apply rotation using Rigidbody.MoveRotation (works properly with physics)
            Quaternion newRotation = Quaternion.Euler(0, newY, tiltToApply);
            _rigidbody.MoveRotation(newRotation);
            _lastRotation = newRotation;
        }

        /// <summary>
        /// Instantly rotate to face direction
        /// </summary>
        public void LookAt(Vector2 direction)
        {
            if (direction.magnitude < 0.1f) return;

            Vector3 targetDir = new Vector3(direction.x, 0, direction.y);
            transform.rotation = Quaternion.LookRotation(targetDir);
        }

        /// <summary>
        /// Add impulse force
        /// </summary>
        public void AddImpulse(Vector2 force)
        {
            Vector3 force3D = new Vector3(force.x, 0, force.y);
            _rigidbody.AddForce(force3D, ForceMode.Impulse);
            _currentVelocity = _rigidbody.linearVelocity;
        }

        /// <summary>
        /// Get forward direction
        /// </summary>
        public Vector2 GetForwardDirection()
        {
            // In 3D with Y-up, forward is -Z for top-down view
            return new Vector2(transform.forward.x, transform.forward.z);
        }

        /// <summary>
        /// Check if currently moving
        /// </summary>
        public bool IsMoving()
        {
            return _currentSpeed > _rotationThreshold;
        }

        /// <summary>
        /// Enable or disable automatic rotation to movement direction
        /// When disabled, you can manually control rotation (e.g., for targeting)
        /// </summary>
        public void SetAutoRotate(bool enabled)
        {
            _autoRotateEnabled = enabled;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw velocity vector
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, _currentVelocity * 0.5f);

            // Draw target velocity
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, _targetVelocity * 0.5f);
        }
#endif
    }

    /// <summary>
    /// Alternative movement strategy - more arcade-like instant response
    /// Demonstrates Strategy pattern - can swap movement behaviors
    /// 3D Version - Movement on XZ plane
    /// </summary>
    public class ArcadeMovement : MonoBehaviour, IMovable
    {
        [SerializeField] private float _maxSpeed = 15f;
        [SerializeField] private float _rotationSpeed = 360f;

        private Rigidbody _rigidbody;

        public Vector2 Velocity => new Vector2(_rigidbody?.linearVelocity.x ?? 0, _rigidbody?.linearVelocity.z ?? 0);
        public float MaxSpeed => _maxSpeed;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.useGravity = false;
            _rigidbody.constraints = RigidbodyConstraints.FreezePositionY |
                                     RigidbodyConstraints.FreezeRotationX |
                                     RigidbodyConstraints.FreezeRotationZ;
        }

        public void Move(Vector2 direction)
        {
            if (direction.magnitude > 0.1f)
            {
                // Instant velocity change on XZ plane
                Vector3 velocity3D = new Vector3(direction.x, 0, direction.y).normalized * _maxSpeed;
                _rigidbody.linearVelocity = velocity3D;

                // Instant rotation
                Vector3 targetDir = new Vector3(direction.x, 0, direction.y);
                transform.rotation = Quaternion.LookRotation(targetDir);
            }
            else
            {
                _rigidbody.linearVelocity = Vector3.zero;
            }
        }

        public void SetSpeed(float speed)
        {
            _maxSpeed = speed;
        }

        public void Stop()
        {
            _rigidbody.linearVelocity = Vector3.zero;
        }
    }
}
