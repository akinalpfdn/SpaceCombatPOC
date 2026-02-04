// ============================================
// INPUT MANAGER
// Handles switching between input types
// ============================================

using UnityEngine;

namespace SpaceCombat.Input
{
    /// <summary>
    /// Input manager that handles switching between input types.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private InputType _preferredInput = InputType.Auto;
        [SerializeField] private KeyboardInputProvider _keyboardProvider;
        [SerializeField] private TouchInputProvider _touchProvider;

        public enum InputType { Auto, Keyboard, Touch }

        private void Start()
        {
            SetupInput();
        }

        private void SetupInput()
        {
            bool isMobile = Application.isMobilePlatform;

            switch (_preferredInput)
            {
                case InputType.Auto:
                    if (isMobile)
                        EnableTouchInput();
                    else
                        EnableKeyboardInput();
                    break;
                case InputType.Keyboard:
                    EnableKeyboardInput();
                    break;
                case InputType.Touch:
                    EnableTouchInput();
                    break;
            }
        }

        private void EnableKeyboardInput()
        {
            if (_keyboardProvider == null)
            {
                _keyboardProvider = gameObject.AddComponent<KeyboardInputProvider>();
            }
            _keyboardProvider.enabled = true;

            if (_touchProvider != null)
                _touchProvider.enabled = false;
        }

        private void EnableTouchInput()
        {
            if (_touchProvider == null)
            {
                _touchProvider = gameObject.AddComponent<TouchInputProvider>();
            }
            _touchProvider.enabled = true;

            if (_keyboardProvider != null)
                _keyboardProvider.enabled = false;
        }
    }
}
