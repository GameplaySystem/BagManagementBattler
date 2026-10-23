using System;
using System.Collections.Generic;
using UnityEngine;

namespace BagManagementBattler.Bag
{
    public sealed class BagGrid
    {
        private readonly BagGridDefinition definition;
        private readonly BagItemInstance[,] occupancy;

        public BagGrid(BagGridDefinition definition)
        {
            this.definition = definition != null
                ? definition
                : throw new ArgumentNullException(nameof(definition));
            this.definition.Validate();

            occupancy = new BagItemInstance[definition.Width, definition.Height];
        }

        public int Width => definition.Width;
        public int Height => definition.Height;

        public bool IsInBounds(Vector2Int cell)
        {
            return definition.IsInBounds(cell);
        }

        public bool IsUnlocked(Vector2Int cell)
        {
            return definition.IsInBounds(cell) && definition.IsInitiallyUnlocked(cell);
        }

        public BagItemInstance GetOccupant(Vector2Int cell)
        {
            if (!IsInBounds(cell))
            {
                throw new ArgumentOutOfRangeException(nameof(cell), $"Cell {cell} is outside the grid.");
            }

            return occupancy[cell.x, cell.y];
        }

        public PlacementFailureReason ValidatePlacement(BagItemInstance item, Vector2Int origin)
        {
            EnsureCanBeginPlacement(item);

            IReadOnlyList<Vector2Int> footprint = item.Definition.Footprint;

            for (int index = 0; index < footprint.Count; index++)
            {
                if (!IsInBounds(origin + footprint[index]))
                {
                    return PlacementFailureReason.OutOfBounds;
                }
            }

            for (int index = 0; index < footprint.Count; index++)
            {
                if (!IsUnlocked(origin + footprint[index]))
                {
                    return PlacementFailureReason.Locked;
                }
            }

            for (int index = 0; index < footprint.Count; index++)
            {
                if (GetOccupant(origin + footprint[index]) != null)
                {
                    return PlacementFailureReason.Occupied;
                }
            }

            return PlacementFailureReason.None;
        }

        public bool TryPlace(
            BagItemInstance item,
            Vector2Int origin,
            out PlacementFailureReason failureReason)
        {
            failureReason = ValidatePlacement(item, origin);
            if (failureReason != PlacementFailureReason.None)
            {
                return false;
            }

            IReadOnlyList<Vector2Int> footprint = item.Definition.Footprint;
            for (int index = 0; index < footprint.Count; index++)
            {
                Vector2Int cell = origin + footprint[index];
                occupancy[cell.x, cell.y] = item;
            }

            item.SetPlacement(origin);
            return true;
        }

        public bool TryRemove(BagItemInstance item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (!item.Placement.HasValue)
            {
                return false;
            }

            Vector2Int origin = item.Placement.Value;
            IReadOnlyList<Vector2Int> footprint = item.Definition.Footprint;

            for (int index = 0; index < footprint.Count; index++)
            {
                Vector2Int cell = origin + footprint[index];
                if (!IsInBounds(cell) || !ReferenceEquals(occupancy[cell.x, cell.y], item))
                {
                    return false;
                }
            }

            for (int index = 0; index < footprint.Count; index++)
            {
                Vector2Int cell = origin + footprint[index];
                occupancy[cell.x, cell.y] = null;
            }

            item.ClearPlacement();
            return true;
        }

        private static void EnsureCanBeginPlacement(BagItemInstance item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (item.IsPlaced)
            {
                throw new InvalidOperationException("Remove an item from its current grid before placing it again.");
            }
        }
    }
}
