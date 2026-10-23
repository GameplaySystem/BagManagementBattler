using System;
using UnityEngine;

namespace BagManagementBattler.Bag
{
    public sealed class BagItemInstance
    {
        public BagItemInstance(BagItemDefinition definition, int level = 1)
        {
            Definition = definition != null
                ? definition
                : throw new ArgumentNullException(nameof(definition));
            Definition.Validate();

            if (level < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(level), "Item level must be at least one.");
            }

            InstanceId = Guid.NewGuid();
            Level = level;
        }

        public Guid InstanceId { get; }
        public BagItemDefinition Definition { get; }
        public int Level { get; }
        public Vector2Int? Placement { get; private set; }
        public bool IsPlaced => Placement.HasValue;

        internal void SetPlacement(Vector2Int origin)
        {
            Placement = origin;
        }

        internal void ClearPlacement()
        {
            Placement = null;
        }
    }
}
