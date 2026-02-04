// ============================================
// WEAPON SLOT BAR - Container for weapon slots
// Manages 4 weapon slot buttons (LA-1 to LA-4)
// ============================================

using UnityEngine;
using SpaceCombat.Events;
using SpaceCombat.Entities;

namespace SpaceCombat.UI.Mobile
{
    /// <summary>
    /// Container that manages 4 weapon slot buttons.
    /// Subscribes to WeaponSwitchedEvent to highlight active slot.
    ///
    /// Usage:
    /// - Attach to a parent GameObject containing 4 WeaponSlotButton children
    /// - Buttons will auto-highlight based on weapon switch events
    /// </summary>
    public class WeaponSlotBar : MonoBehaviour
    {
        // ============================================
        // CONFIGURATION
        // ============================================

        [Header("Slot Buttons")]
        [SerializeField] private WeaponSlotButton[] _slots = new WeaponSlotButton[4];

        [Header("Settings")]
        [SerializeField] private int _defaultSlot = 0;

        // ============================================
        // RUNTIME STATE
        // ============================================

        private PlayerShip _playerShip;
        private int _currentSlot;

        // ============================================
        // UNITY LIFECYCLE
        // ============================================

        private void OnEnable()
        {
            EventBus.Subscribe<WeaponSwitchedEvent>(OnWeaponSwitched);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<WeaponSwitchedEvent>(OnWeaponSwitched);
        }

        private void Start()
        {
            // Set default selection
            SetSelectedSlot(_defaultSlot);
        }

        // ============================================
        // PUBLIC METHODS
        // ============================================

        /// <summary>
        /// Initialize all slot buttons with PlayerShip reference.
        /// Call this when player spawns.
        /// </summary>
        public void Initialize(PlayerShip playerShip)
        {
            _playerShip = playerShip;

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null)
                {
                    _slots[i].SetPlayerShip(playerShip);

                    // Set weapon names from config if available
                    if (playerShip != null)
                    {
                        var config = playerShip.Config;
                        if (config != null && config.availableWeapons != null && i < config.availableWeapons.Length)
                        {
                            var weapon = config.availableWeapons[i];
                            if (weapon != null)
                            {
                                _slots[i].SetWeaponName(weapon.weaponName);
                            }
                        }
                    }
                }
            }

            // Set initial selection to player's current weapon slot
            if (playerShip != null)
            {
                SetSelectedSlot(playerShip.CurrentWeaponSlot);
            }
        }

        /// <summary>
        /// Manually set the selected slot (0-3).
        /// </summary>
        public void SetSelectedSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Length) return;

            _currentSlot = slotIndex;

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null)
                {
                    _slots[i].SetSelected(i == slotIndex);
                }
            }
        }

        /// <summary>
        /// Get a specific slot button.
        /// </summary>
        public WeaponSlotButton GetSlot(int index)
        {
            if (index < 0 || index >= _slots.Length) return null;
            return _slots[index];
        }

        // ============================================
        // EVENT HANDLERS
        // ============================================

        private void OnWeaponSwitched(WeaponSwitchedEvent evt)
        {
            SetSelectedSlot(evt.SlotIndex);
        }
    }
}
