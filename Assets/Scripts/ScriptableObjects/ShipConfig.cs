using UnityEngine;
using StarReapers.Interfaces;

namespace StarReapers.ScriptableObjects
{
    [CreateAssetMenu(fileName = "NewShipConfig", menuName = "StarReapers/Ship Configuration")]
    public class ShipConfig : ScriptableObject
    {
        [Header("Identity")]
        public string shipName = "Fighter";
        public Sprite shipSprite;

        [Header("Prefab")]
        [Tooltip("The ship prefab to instantiate. Should have WeaponController and fire point Transforms.")]
        public GameObject shipPrefab;

        [Header("Stats")]
        public float maxHealth = 100f;
        public float maxShield = 50f;
        public float shieldRegenRate = 2f;
        public float shieldRegenDelay = 3f;

        [Header("Movement")]
        public float maxSpeed = 10f;
        public float acceleration = 5f;
        public float deceleration = 3f;
        public float rotationSpeed = 3000f;

        [Header("Combat")]
        public WeaponConfig[] availableWeapons;

        [Header("Shield Visual")]
        [Tooltip("Shield visual config for DarkOrbit-style effects. If null, no shield visual is created.")]
        public ShieldVisualConfig shieldVisualConfig;

        [Tooltip("Shield scale specific to this ship. Overrides config default if not zero.")]
        public Vector3 shieldScale = new Vector3(2f, 1.5f, 2.5f);

        [Header("Audio")]
        public string shieldHitSoundId = "shield_hit";
        public string hullHitSoundId = "hull_hit";
        public string explosionSoundId = "explosion_medium";
    }
}