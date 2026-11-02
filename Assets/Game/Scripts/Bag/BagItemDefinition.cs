using System;
using System.Collections.Generic;
using UnityEngine;

namespace BagManagementBattler.Bag
{
    public enum BagItemRole
    {
        Soldier,
        Weapon
    }

    [Serializable]
    public sealed class BagItemVisualPart
    {
        [SerializeField] private Sprite sprite;
        [SerializeField] private Vector2 localPositionInCells;
        [SerializeField] private float scaleInCells = 1f;

        public Sprite Sprite => sprite;
        public Vector2 LocalPositionInCells => localPositionInCells;
        public float ScaleInCells => scaleInCells;
    }

    [CreateAssetMenu(fileName = "BagItemDefinition", menuName = "Bag Game/Bag Item Definition")]
    public sealed class BagItemDefinition : ScriptableObject
    {
        [SerializeField] private string itemTypeId = "item";
        [SerializeField] private int level = 1;
        [SerializeField] private BagItemDefinition nextLevelDefinition;
        [SerializeField] private BagItemRole role = BagItemRole.Soldier;
        [SerializeField, Tooltip("Used only when Role is Weapon.")]
        private float productionDurationSeconds;
        [SerializeField, Tooltip("Used only when Role is Soldier.")]
        private float movementSpeed = 1f;
        [SerializeField] private List<Vector2Int> footprint = new List<Vector2Int> { Vector2Int.zero };
        [SerializeField] private Sprite iconSprite;
        [SerializeField] private Color itemColor = Color.white;
        [SerializeField] private List<BagItemVisualPart> visualParts = new List<BagItemVisualPart>();
        [SerializeField, Tooltip("Weapon visual position relative to a soldier HandPoint.")]
        private Vector2 equippedLocalPosition;
        [SerializeField, Tooltip("Weapon visual Z rotation relative to a soldier HandPoint.")]
        private float equippedLocalRotationDegrees;
        [SerializeField, Tooltip("Weapon visual scale relative to a soldier HandPoint.")]
        private Vector2 equippedLocalScale = Vector2.one;

        public string ItemTypeId => itemTypeId;
        public int Level => level;
        public BagItemDefinition NextLevelDefinition => nextLevelDefinition;
        public BagItemRole Role => role;
        public float ProductionDurationSeconds => productionDurationSeconds;
        public float MovementSpeed => movementSpeed;
        public IReadOnlyList<Vector2Int> Footprint => footprint;
        public Sprite IconSprite => iconSprite;
        public Color ItemColor => itemColor;
        public IReadOnlyList<BagItemVisualPart> VisualParts => visualParts;
        public Vector2 EquippedLocalPosition => equippedLocalPosition;
        public float EquippedLocalRotationDegrees => equippedLocalRotationDegrees;
        public Vector2 EquippedLocalScale => equippedLocalScale;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(itemTypeId))
            {
                throw new InvalidOperationException("An item type identifier is required.");
            }

            if (level < 1)
            {
                throw new InvalidOperationException("An item definition level must be at least one.");
            }

            if (!Enum.IsDefined(typeof(BagItemRole), role))
            {
                throw new InvalidOperationException($"Unknown bag item role {role}.");
            }

            if (role == BagItemRole.Weapon &&
                (float.IsNaN(productionDurationSeconds) ||
                 float.IsInfinity(productionDurationSeconds) ||
                 productionDurationSeconds <= 0f))
            {
                throw new InvalidOperationException("A weapon production duration must be finite and greater than zero.");
            }

            if (role == BagItemRole.Weapon &&
                (!IsFinite(equippedLocalPosition.x) ||
                 !IsFinite(equippedLocalPosition.y) ||
                 !IsFinite(equippedLocalRotationDegrees) ||
                 !IsFinite(equippedLocalScale.x) ||
                 !IsFinite(equippedLocalScale.y) ||
                 equippedLocalScale.x <= 0f ||
                 equippedLocalScale.y <= 0f))
            {
                throw new InvalidOperationException(
                    "A weapon equipped pose requires finite position/rotation and positive scale.");
            }

            if (role == BagItemRole.Soldier &&
                (float.IsNaN(movementSpeed) ||
                 float.IsInfinity(movementSpeed) ||
                 movementSpeed <= 0f))
            {
                throw new InvalidOperationException("A soldier movement speed must be finite and greater than zero.");
            }

            if (footprint == null || footprint.Count == 0)
            {
                throw new InvalidOperationException("An item footprint must contain at least one cell.");
            }

            HashSet<Vector2Int> uniqueCells = new HashSet<Vector2Int>();
            foreach (Vector2Int cell in footprint)
            {
                if (!uniqueCells.Add(cell))
                {
                    throw new InvalidOperationException($"Footprint contains duplicate cell {cell}.");
                }
            }
        }

        public static BagItemDefinition CreateTransient(
            string itemTypeId,
            IEnumerable<Vector2Int> footprint)
        {
            return CreateTransient(itemTypeId, 1, footprint, null);
        }

        public static BagItemDefinition CreateTransient(
            string itemTypeId,
            int level,
            IEnumerable<Vector2Int> footprint,
            BagItemDefinition nextLevelDefinition = null)
        {
            return CreateTransient(
                itemTypeId,
                level,
                BagItemRole.Soldier,
                0f,
                1f,
                footprint,
                nextLevelDefinition);
        }

        public static BagItemDefinition CreateTransient(
            string itemTypeId,
            int level,
            BagItemRole role,
            float productionDurationSeconds,
            IEnumerable<Vector2Int> footprint,
            BagItemDefinition nextLevelDefinition = null)
        {
            return CreateTransient(
                itemTypeId,
                level,
                role,
                productionDurationSeconds,
                role == BagItemRole.Soldier ? 1f : 0f,
                footprint,
                nextLevelDefinition);
        }

        public static BagItemDefinition CreateTransient(
            string itemTypeId,
            int level,
            BagItemRole role,
            float productionDurationSeconds,
            float movementSpeed,
            IEnumerable<Vector2Int> footprint,
            BagItemDefinition nextLevelDefinition = null)
        {
            if (footprint == null)
            {
                throw new ArgumentNullException(nameof(footprint));
            }

            BagItemDefinition definition = CreateInstance<BagItemDefinition>();
            definition.hideFlags = HideFlags.HideAndDontSave;
            definition.Configure(
                itemTypeId,
                level,
                role,
                productionDurationSeconds,
                movementSpeed,
                footprint,
                nextLevelDefinition);
            return definition;
        }

        private void Configure(
            string configuredItemTypeId,
            int configuredLevel,
            BagItemRole configuredRole,
            float configuredProductionDurationSeconds,
            float configuredMovementSpeed,
            IEnumerable<Vector2Int> configuredFootprint,
            BagItemDefinition configuredNextLevelDefinition)
        {
            if (string.IsNullOrWhiteSpace(configuredItemTypeId))
            {
                throw new ArgumentException("An item type identifier is required.", nameof(configuredItemTypeId));
            }

            itemTypeId = configuredItemTypeId;
            level = configuredLevel;
            role = configuredRole;
            productionDurationSeconds = configuredProductionDurationSeconds;
            movementSpeed = configuredMovementSpeed;
            nextLevelDefinition = configuredNextLevelDefinition;
            footprint = new List<Vector2Int>();

            HashSet<Vector2Int> uniqueCells = new HashSet<Vector2Int>();
            foreach (Vector2Int cell in configuredFootprint)
            {
                if (!uniqueCells.Add(cell))
                {
                    throw new ArgumentException($"Footprint contains duplicate cell {cell}.", nameof(configuredFootprint));
                }

                footprint.Add(cell);
            }

            if (footprint.Count == 0)
            {
                throw new ArgumentException("An item footprint must contain at least one cell.", nameof(configuredFootprint));
            }

            Validate();
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
