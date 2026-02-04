// ============================================
// VIRTUAL JOYSTICK - Mobile touch joystick
// UI EventSystem-based virtual joystick for mobile input
// ============================================

using UnityEngine;
using UnityEngine.EventSystems;

namespace SpaceCombat.UI.Mobile
{
    /// <summary>
    /// Virtual joystick for mobile touch input.
    /// Uses Unity UI EventSystem for drag detection.
    ///
    /// Usage:
    /// - Attach to a UI Image (the joystick base)
    /// - Assign a child Image as the handle
    /// - Read Direction property for normalized input
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        // ============================================
        // CONFIGURATION
        // ============================================

        [Header("References")]
        [SerializeField] private RectTransform _handle;

        [Header("Settings")]
        [SerializeField] private float _handleRange = 50f;
        [Tooltip("Dead zone - inputs below this magnitude are ignored")]
        [SerializeField] [Range(0f, 0.5f)] private float _deadZone = 0.1f;

        // ============================================
        // RUNTIME STATE
        // ============================================

        private RectTransform _baseRect;
        private Canvas _canvas;
        private Camera _canvasCamera;
        private Vector2 _inputDirection;
        private bool _isDragging;

        // ============================================
        // PUBLIC PROPERTIES
        // ============================================

        /// <summary>
        /// Normalized joystick direction (-1 to 1 on each axis).
        /// Returns Vector2.zero when not being used or within dead zone.
        /// </summary>
        public Vector2 Direction => _inputDirection;

        /// <summary>
        /// Raw input magnitude (0 to 1) before dead zone is applied.
        /// Useful for analog speed control.
        /// </summary>
        public float Magnitude { get; private set; }

        /// <summary>
        /// Whether the joystick is currently being touched/dragged.
        /// </summary>
        public bool IsActive => _isDragging;

        // ============================================
        // UNITY LIFECYCLE
        // ============================================

        private void Awake()
        {
            _baseRect = GetComponent<RectTransform>();

            // Find the canvas for coordinate conversion
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null && _canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                _canvasCamera = _canvas.worldCamera;
            }

            // Ensure handle is at center initially
            if (_handle != null)
            {
                _handle.anchoredPosition = Vector2.zero;
            }
        }

        // ============================================
        // EVENT SYSTEM HANDLERS
        // ============================================

        /// <summary>
        /// Called when touch/click begins on the joystick.
        /// IPointerDownHandler implementation.
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            _isDragging = true;
            OnDrag(eventData);
        }

        /// <summary>
        /// Called when touch/click is released.
        /// IPointerUpHandler implementation.
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            _isDragging = false;
            _inputDirection = Vector2.zero;
            Magnitude = 0f;

            // Return handle to center
            if (_handle != null)
            {
                _handle.anchoredPosition = Vector2.zero;
            }
        }

        /// <summary>
        /// Called continuously while dragging.
        /// IDragHandler implementation.
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            // Convert screen position to local position in the joystick base
            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _baseRect,
                eventData.position,
                _canvasCamera,
                out localPoint))
            {
                return;
            }

            // Calculate direction and magnitude
            Vector2 direction = localPoint;
            float magnitude = direction.magnitude;

            // Clamp to handle range
            if (magnitude > _handleRange)
            {
                direction = direction.normalized * _handleRange;
                magnitude = _handleRange;
            }

            // Update handle visual position
            if (_handle != null)
            {
                _handle.anchoredPosition = direction;
            }

            // Normalize magnitude (0 to 1)
            Magnitude = magnitude / _handleRange;

            // Apply dead zone
            if (Magnitude < _deadZone)
            {
                _inputDirection = Vector2.zero;
                Magnitude = 0f;
            }
            else
            {
                // Remap magnitude to account for dead zone
                float remappedMagnitude = (Magnitude - _deadZone) / (1f - _deadZone);
                _inputDirection = direction.normalized * remappedMagnitude;
            }
        }

        // ============================================
        // PUBLIC METHODS
        // ============================================

        /// <summary>
        /// Manually reset the joystick to center position.
        /// </summary>
        public void ResetJoystick()
        {
            _isDragging = false;
            _inputDirection = Vector2.zero;
            Magnitude = 0f;

            if (_handle != null)
            {
                _handle.anchoredPosition = Vector2.zero;
            }
        }

        /// <summary>
        /// Set the handle movement range.
        /// </summary>
        public void SetHandleRange(float range)
        {
            _handleRange = Mathf.Max(1f, range);
        }
    }
}
