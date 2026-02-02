using UnityEngine;
using SpaceCombat.Interfaces;

namespace SpaceCombat.ScriptableObjects
{
    [CreateAssetMenu(fileName = "NewShipConfig", menuName = "SpaceCombat/Ship Configuration")]
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
        public float rotationSpeed = 180f;

        [Header("Combat")]
        public WeaponConfig[] availableWeapons;
        [Header("Audio")]
        public string shieldHitSoundId = "shield_hit";
        public string hullHitSoundId = "hull_hit";
        public string explosionSoundId = "explosion_medium";
    }
}