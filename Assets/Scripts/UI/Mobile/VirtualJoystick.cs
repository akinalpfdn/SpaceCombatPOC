// ============================================
// VIRTUAL JOYSTICK - Mobile touch joystick
// UI EventSystem-based virtual joystick for mobile input
// Supports floating mode (joystick moves to touch position)
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
    /// - Attach to a UI Image (the touch zone)
    /// - Assign JoystickContainer and Handle references
    /// - Read Direction property for normalized input
    ///
    /// Floating Mode:
    /// - Joystick appears where user touches
    /// - Parent rect (touch zone) should have Pos X/Y = 0
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        // ============================================
        // CONFIGURATION
        // ============================================

        [Header("References")]
        [SerializeField] private RectTransform _handle;
        [Tooltip("Container that holds the joystick visuals (for floating mode)")]
        [SerializeField] private RectTransform _joystickContainer;

        [Header("Settings")]
        [SerializeField] private float _handleRange = 50f;
        [Tooltip("Dead zone - inputs below this magnitude are ignored")]
        [SerializeField] [Range(0f, 0.5f)] private float _deadZone = 0.1f;

        [Header("Floating Mode")]
        [Tooltip("When enabled, joystick moves to where user touches")]
        [SerializeField] private bool _floatingMode = true;
        [Tooltip("Hide joystick visuals when not in use")]
        [SerializeField] private bool _hideWhenInactive = true;
        [Tooltip("Fade duration for show/hide animation")]
        [SerializeField] private float _fadeDuration = 0.15f;

        // ============================================
        // RUNTIME STATE
        // ============================================

        private RectTransform _baseRect;
        private Camera _canvasCamera;
        private Vector2 _inputDirection;
        private bool _isDragging;
        private CanvasGroup _canvasGroup;
        private float _targetAlpha;

        // ============================================
        // PUBLIC PROPERTIES
        // ============================================

        /// <summary>
        /// Normalized joystick direction (-1 to 1 on each axis).
        /// </summary>
        public Vector2 Direction => _inputDirection;

        /// <summary>
        /// Raw input magnitude (0 to 1).
        /// </summary>
        public float Magnitude { get; private set; }

        /// <summary>
        /// Whether the joystick is currently being touched.
        /// </summary>
        public bool IsActive => _isDragging;

        /// <summary>
        /// Whether floating mode is enabled.
        /// </summary>
        public bool IsFloatingMode => _floatingMode;

        // ============================================
        // UNITY LIFECYCLE
        // ============================================

        private void Awake()
        {
            _baseRect = GetComponent<RectTransform>();

            // Get canvas camera for Screen Space - Camera mode
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                _canvasCamera = canvas.worldCamera;
            }

            // Setup floating mode
            if (_floatingMode && _joystickContainer != null)
            {
                _canvasGroup = _joystickContainer.GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = _joystickContainer.gameObject.AddComponent<CanvasGroup>();
                }

                if (_hideWhenInactive)
                {
                    _canvasGroup.alpha = 0f;
                    _targetAlpha = 0f;
                }
            }

            // Ensure handle starts at center
            if (_handle != null)
            {
                _handle.anchoredPosition = Vector2.zero;
            }
        }

        private void Update()
        {
            // Smooth alpha transition for floating mode
            if (_floatingMode && _hideWhenInactive && _canvasGroup != null)
            {
                if (!Mathf.Approximately(_canvasGroup.alpha, _targetAlpha))
                {
                    _canvasGroup.alpha = Mathf.MoveTowards(
                        _canvasGroup.alpha,
                        _targetAlpha,
                        Time.unscaledDeltaTime / _fadeDuration
                    );
                }
            }
        }

        // ============================================
        // EVENT SYSTEM HANDLERS
        // ============================================

        public void OnPointerDown(PointerEventData eventData)
        {
            _isDragging = true;

            // Floating mode: Move joystick to touch position
            if (_floatingMode && _joystickContainer != null)
            {
                RectTransform parentRect = _joystickContainer.parent as RectTransform;
                if (parentRect != null)
                {
                    Vector2 localPoint;
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        parentRect,
                        eventData.position,
                        _canvasCamera,
                        out localPoint))
                    {
                        _joystickContainer.anchoredPosition = localPoint;
                    }
                }

                if (_hideWhenInactive && _canvasGroup != null)
                {
                    _targetAlpha = 1f;
                }
            }

            // Reset handle to center
            if (_handle != null)
            {
                _handle.anchoredPosition = Vector2.zero;
            }

            _inputDirection = Vector2.zero;
            Magnitude = 0f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isDragging = false;
            _inputDirection = Vector2.zero;
            Magnitude = 0f;

            if (_handle != null)
            {
                _handle.anchoredPosition = Vector2.zero;
            }

            if (_floatingMode && _hideWhenInactive && _canvasGroup != null)
            {
                _targetAlpha = 0f;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 direction;

            // Calculate direction relative to joystick center
            RectTransform targetRect = (_floatingMode && _joystickContainer != null)
                ? _joystickContainer
                : _baseRect;

            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetRect,
                eventData.position,
                _canvasCamera,
                out localPoint))
            {
                return;
            }
            direction = localPoint;

            float magnitude = direction.magnitude;

            // Clamp to handle range
            if (magnitude > _handleRange)
            {
                direction = direction.normalized * _handleRange;
                magnitude = _handleRange;
            }

            // Update handle visual
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
                float remappedMagnitude = (Magnitude - _deadZone) / (1f - _deadZone);
                _inputDirection = direction.normalized * remappedMagnitude;
            }
        }

        // ============================================
        // PUBLIC METHODS
        // ============================================

        public void ResetJoystick()
        {
            _isDragging = false;
            _inputDirection = Vector2.zero;
            Magnitude = 0f;

            if (_handle != null)
            {
                _handle.anchoredPosition = Vector2.zero;
            }

            if (_floatingMode && _hideWhenInactive && _canvasGroup != null)
            {
                _targetAlpha = 0f;
            }
        }

        public void SetHandleRange(float range)
        {
            _handleRange = Mathf.Max(1f, range);
        }

        public void SetFloatingMode(bool enabled)
        {
            _floatingMode = enabled;

            if (_floatingMode && _joystickContainer != null && _canvasGroup == null)
            {
                _canvasGroup = _joystickContainer.GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = _joystickContainer.gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (!_floatingMode && _canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _targetAlpha = 1f;
            }
        }
    }
}
