// ============================================
// SHIP MOVEMENT - Strategy Pattern Ready
// Handles all movement logic independently
// Can be swapped for different movement behaviors
// ============================================

using UnityEngine;
using SpaceCombat.Interfaces;
using SpaceCombat.Environment;

namespace SpaceCombat.Movement
{
    /// <summary>
    /// Handles ship movement with smooth acceleration/deceleration
    /// DarkOrbit-style movement with momentum
    /// 3D Version - Movement on XZ plane
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ShipMovement : MonoBehaviour, IMovable
    {
        [Header("Movement Settings")]
        [SerializeField] private float _maxSpeed = 10f;
        [SerializeField] private float _acceleration = 5f;
        [SerializeField] private float _deceleration = 3f;
        [SerializeField] private float _rotationSpeed = 180f;

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

        // Components
        private Rigidbody _rigidbody;
        private MapBounds _mapBounds;

        // State
        private Vector3 _currentVelocity;
        private Vector3 _targetVelocity;
        private Vector3 _velocityRef;
        private float _currentSpeed;
        private bool _autoRotateEnabled = true;

        // Banking state
        private float _currentTilt;
        private float _targetTilt;
        private Quaternion _lastRotation;
        private float _angularVelocity;

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

            // Find map bounds in scene
            if (_respectMapBounds)
            {
                _mapBounds = FindObjectOfType<MapBounds>();
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

            // Rotate to face movement direction (only if auto-rotate is enabled)
            if (_autoRotateEnabled && _rotateToMovement && _currentSpeed > _rotationThreshold)
            {
                Vector3 moveDir = _currentVelocity.normalized;
                RotateTowards(new Vector2(moveDir.x, moveDir.z));
            }

            // Apply banking/tilt effect
            if (_enableBanking)
            {
                ApplyBanking();
            }
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
        /// Preserves Z-axis banking tilt
        /// </summary>
        public void RotateTowards(Vector2 direction)
        {
            if (direction.magnitude < 0.1f) return;

            // Convert 2D direction to 3D XZ plane
            Vector3 targetDir = new Vector3(direction.x, 0, direction.y);
            if (targetDir != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDir);

                if (_enableBanking)
                {
                    // Preserve current Z tilt when rotating Y
                    Vector3 currentEuler = transform.rotation.eulerAngles;
                    Quaternion rotated = Quaternion.Slerp(
                        Quaternion.Euler(0, currentEuler.y, 0),  // Only Y rotation
                        targetRotation,
                        _rotationSpeed * Time.fixedDeltaTime
                    );
                    // Apply Y rotation + current Z tilt
                    transform.rotation = Quaternion.Euler(0, rotated.eulerAngles.y, _currentTilt);
                }
                else
                {
                    // No banking - normal rotation
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        _rotationSpeed * Time.fixedDeltaTime
                    );
                }
            }
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
