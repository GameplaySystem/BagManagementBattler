using System;
using UnityEngine;

namespace BagManagementBattler.Bag
{
    public sealed class BagItemInstance
    {
        public BagItemInstance(BagItemDefinition definition)
        {
            Definition = definition != null
                ? definition
                : throw new ArgumentNullException(nameof(definition));
            Definition.Validate();

            InstanceId = Guid.NewGuid();
        }

        public Guid InstanceId { get; }
        public BagItemDefinition Definition { get; private set; }
        public int Level => Definition.Level;
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

        internal void AdvanceTo(BagItemDefinition nextLevelDefinition)
        {
            if (nextLevelDefinition == null)
            {
                throw new ArgumentNullException(nameof(nextLevelDefinition));
            }

            Definition = nextLevelDefinition;
        }
    }
}
