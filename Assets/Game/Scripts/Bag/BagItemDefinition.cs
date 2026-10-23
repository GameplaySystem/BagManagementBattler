using System;
using System.Collections.Generic;
using UnityEngine;

namespace BagManagementBattler.Bag
{
    [CreateAssetMenu(fileName = "BagItemDefinition", menuName = "Bag Game/Bag Item Definition")]
    public sealed class BagItemDefinition : ScriptableObject
    {
        [SerializeField] private string itemTypeId = "item";
        [SerializeField] private List<Vector2Int> footprint = new List<Vector2Int> { Vector2Int.zero };

        public string ItemTypeId => itemTypeId;
        public IReadOnlyList<Vector2Int> Footprint => footprint;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(itemTypeId))
            {
                throw new InvalidOperationException("An item type identifier is required.");
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
            if (footprint == null)
            {
                throw new ArgumentNullException(nameof(footprint));
            }

            BagItemDefinition definition = CreateInstance<BagItemDefinition>();
            definition.hideFlags = HideFlags.HideAndDontSave;
            definition.Configure(itemTypeId, footprint);
            return definition;
        }

        private void Configure(string configuredItemTypeId, IEnumerable<Vector2Int> configuredFootprint)
        {
            if (string.IsNullOrWhiteSpace(configuredItemTypeId))
            {
                throw new ArgumentException("An item type identifier is required.", nameof(configuredItemTypeId));
            }

            itemTypeId = configuredItemTypeId;
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
    }
}
