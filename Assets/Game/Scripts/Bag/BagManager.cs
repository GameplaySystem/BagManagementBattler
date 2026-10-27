using System;
using UnityEngine;

namespace BagManagementBattler.Bag
{
    public sealed class BagManager
    {
        private DragSnapshot activeDrag;

        public BagManager(BagGrid grid, ItemContainer container)
        {
            Grid = grid ?? throw new ArgumentNullException(nameof(grid));
            Container = container ?? throw new ArgumentNullException(nameof(container));
        }

        public BagGrid Grid { get; }
        public ItemContainer Container { get; }
        public BagItemInstance DraggedItem => activeDrag?.Item;
        public bool IsDragging => activeDrag != null;

        public bool BeginDrag(BagItemInstance item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (IsDragging)
            {
                return false;
            }

            if (item.Placement.HasValue)
            {
                Vector2Int origin = item.Placement.Value;
                if (!Grid.TryRemove(item))
                {
                    return false;
                }

                activeDrag = DragSnapshot.FromBag(item, origin);
                return true;
            }

            if (!Container.TryRemove(item, out int originalIndex))
            {
                return false;
            }

            activeDrag = DragSnapshot.FromContainer(item, originalIndex);
            return true;
        }

        public PlacementFailureReason ValidateDropToBag(Vector2Int origin)
        {
            EnsureDragging();
            return Grid.ValidatePlacement(activeDrag.Item, origin);
        }

        public bool TryDropToBag(Vector2Int origin, out PlacementFailureReason failureReason)
        {
            EnsureDragging();

            if (Grid.TryPlace(activeDrag.Item, origin, out failureReason))
            {
                activeDrag = null;
                return true;
            }

            RestoreSource();
            return false;
        }

        public void DropToContainer()
        {
            EnsureDragging();

            if (activeDrag.Source == DragSource.Container)
            {
                Container.Insert(activeDrag.ContainerIndex, activeDrag.Item);
            }
            else
            {
                Container.AddBottom(activeDrag.Item);
            }

            activeDrag = null;
        }

        public void CancelDrag()
        {
            EnsureDragging();
            RestoreSource();
        }

        private void RestoreSource()
        {
            DragSnapshot snapshot = activeDrag;

            if (snapshot.Source == DragSource.Bag)
            {
                if (!Grid.TryPlace(snapshot.Item, snapshot.BagOrigin, out PlacementFailureReason failureReason))
                {
                    throw new InvalidOperationException(
                        $"Could not restore dragged item to its bag origin: {failureReason}.");
                }
            }
            else
            {
                Container.Insert(snapshot.ContainerIndex, snapshot.Item);
            }

            activeDrag = null;
        }

        private void EnsureDragging()
        {
            if (!IsDragging)
            {
                throw new InvalidOperationException("No bag item is currently being dragged.");
            }
        }

        private enum DragSource
        {
            Bag,
            Container
        }

        private sealed class DragSnapshot
        {
            private DragSnapshot(
                BagItemInstance item,
                DragSource source,
                Vector2Int bagOrigin,
                int containerIndex)
            {
                Item = item;
                Source = source;
                BagOrigin = bagOrigin;
                ContainerIndex = containerIndex;
            }

            public BagItemInstance Item { get; }
            public DragSource Source { get; }
            public Vector2Int BagOrigin { get; }
            public int ContainerIndex { get; }

            public static DragSnapshot FromBag(BagItemInstance item, Vector2Int origin)
            {
                return new DragSnapshot(item, DragSource.Bag, origin, -1);
            }

            public static DragSnapshot FromContainer(BagItemInstance item, int index)
            {
                return new DragSnapshot(item, DragSource.Container, default, index);
            }
        }
    }
}
