// ============================================
// SHIP MOVEMENT - Strategy Pattern Ready
// Handles all movement logic independently
// Can be swapped for different movement behaviors
// ============================================

using UnityEngine;
using SpaceCombat.Interfaces;

namespace SpaceCombat.Movement
{
    /// <summary>
    /// Handles ship movement with smooth acceleration/deceleration
    /// DarkOrbit-style movement with momentum
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
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

        // Components
        private Rigidbody2D _rigidbody;

        // State
        private Vector2 _currentVelocity;
        private Vector2 _targetVelocity;
        private Vector2 _velocityRef;
        private float _currentSpeed;
        private bool _autoRotateEnabled = true;

        // Properties
        public Vector2 Velocity => _rigidbody?.linearVelocity ?? Vector2.zero;
        public float MaxSpeed => _maxSpeed;
        public float CurrentSpeedNormalized => _currentSpeed / _maxSpeed;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();

            // Configure rigidbody for top-down 2D
            _rigidbody.gravityScale = 0f;
            _rigidbody.angularDamping = 5f;
            _rigidbody.linearDamping = 0f;
            _rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;

            // Smooth movement - interpolate between physics frames
            _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
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
        /// </summary>
        public void ApplyMovement(Vector2 input)
        {
            // Calculate target velocity based on input
            if (input.magnitude > 0.1f)
            {
                input = input.normalized;
                _targetVelocity = input * _maxSpeed;
                
                // Accelerate towards target
                _currentVelocity = Vector2.SmoothDamp(
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
                _currentVelocity = Vector2.MoveTowards(
                    _currentVelocity, 
                    Vector2.zero, 
                    _deceleration * Time.fixedDeltaTime
                );
            }

            // Apply velocity
            _rigidbody.linearVelocity = _currentVelocity;
            _currentSpeed = _currentVelocity.magnitude;

            // Rotate to face movement direction (only if auto-rotate is enabled)
            if (_autoRotateEnabled && _rotateToMovement && _currentSpeed > _rotationThreshold)
            {
                RotateTowards(_currentVelocity.normalized);
            }
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
            _currentVelocity = Vector2.zero;
            _targetVelocity = Vector2.zero;
            _rigidbody.linearVelocity = Vector2.zero;
            _currentSpeed = 0;
        }

        /// <summary>
        /// Rotate ship to face a direction
        /// </summary>
        public void RotateTowards(Vector2 direction)
        {
            if (direction.magnitude < 0.1f) return;

            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            float currentAngle = transform.eulerAngles.z;
            
            float newAngle = Mathf.MoveTowardsAngle(
                currentAngle, 
                targetAngle, 
                _rotationSpeed * Time.fixedDeltaTime
            );

            transform.rotation = Quaternion.Euler(0, 0, newAngle);
        }

        /// <summary>
        /// Instantly rotate to face direction
        /// </summary>
        public void LookAt(Vector2 direction)
        {
            if (direction.magnitude < 0.1f) return;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        /// <summary>
        /// Add impulse force
        /// </summary>
        public void AddImpulse(Vector2 force)
        {
            _rigidbody.AddForce(force, ForceMode2D.Impulse);
            _currentVelocity = _rigidbody.linearVelocity;
        }

        /// <summary>
        /// Get forward direction
        /// </summary>
        public Vector2 GetForwardDirection()
        {
            return transform.up;
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
    /// </summary>
    public class ArcadeMovement : MonoBehaviour, IMovable
    {
        [SerializeField] private float _maxSpeed = 15f;
        [SerializeField] private float _rotationSpeed = 360f;

        private Rigidbody2D _rigidbody;

        public Vector2 Velocity => _rigidbody?.linearVelocity ?? Vector2.zero;
        public float MaxSpeed => _maxSpeed;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _rigidbody.gravityScale = 0f;
        }

        public void Move(Vector2 direction)
        {
            if (direction.magnitude > 0.1f)
            {
                // Instant velocity change
                _rigidbody.linearVelocity = direction.normalized * _maxSpeed;
                
                // Instant rotation
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
            else
            {
                _rigidbody.linearVelocity = Vector2.zero;
            }
        }

        public void SetSpeed(float speed)
        {
            _maxSpeed = speed;
        }

        public void Stop()
        {
            _rigidbody.linearVelocity = Vector2.zero;
        }
    }
}
