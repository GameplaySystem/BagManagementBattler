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
            return EvaluateDrop(origin).FailureReason;
        }

        public bool TryDropToBag(Vector2Int origin, out PlacementFailureReason failureReason)
        {
            EnsureDragging();

            DropPlan plan = EvaluateDrop(origin);
            failureReason = plan.FailureReason;
            if (failureReason != PlacementFailureReason.None)
            {
                RestoreSource();
                return false;
            }

            switch (plan.Operation)
            {
                case DropOperation.Place:
                    PlaceDraggedItem(origin);
                    break;
                case DropOperation.Merge:
                    plan.ObstructingItem.AdvanceTo(activeDrag.Item.Definition.NextLevelDefinition);
                    activeDrag = null;
                    break;
                case DropOperation.Replace:
                    ReplaceItem(plan.ObstructingItem, origin);
                    break;
                default:
                    throw new InvalidOperationException("The evaluated bag drop has no valid operation.");
            }

            return true;
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

        private DropPlan EvaluateDrop(Vector2Int origin)
        {
            BagItemInstance draggedItem = activeDrag.Item;
            PlacementFailureReason directFailure = Grid.ValidatePlacement(draggedItem, origin);
            if (directFailure == PlacementFailureReason.None)
            {
                return DropPlan.Valid(DropOperation.Place, null);
            }

            if (directFailure != PlacementFailureReason.Occupied)
            {
                return DropPlan.Invalid(directFailure);
            }

            BagItemInstance obstruction = null;
            foreach (Vector2Int offset in draggedItem.Definition.Footprint)
            {
                BagItemInstance occupant = Grid.GetOccupant(origin + offset);
                if (occupant == null)
                {
                    continue;
                }

                if (obstruction == null)
                {
                    obstruction = occupant;
                }
                else if (!ReferenceEquals(obstruction, occupant))
                {
                    return DropPlan.Invalid(PlacementFailureReason.Occupied);
                }
            }

            if (obstruction == null)
            {
                throw new InvalidOperationException("An occupied placement did not report an obstructing item.");
            }

            if (CanMerge(draggedItem, obstruction))
            {
                return DropPlan.Valid(DropOperation.Merge, obstruction);
            }

            PlacementFailureReason replacementFailure =
                Grid.ValidatePlacementIgnoring(draggedItem, origin, obstruction);
            return replacementFailure == PlacementFailureReason.None
                ? DropPlan.Valid(DropOperation.Replace, obstruction)
                : DropPlan.Invalid(replacementFailure);
        }

        private void PlaceDraggedItem(Vector2Int origin)
        {
            if (!Grid.TryPlace(activeDrag.Item, origin, out PlacementFailureReason failureReason))
            {
                throw new InvalidOperationException(
                    $"A validated bag placement unexpectedly failed: {failureReason}.");
            }

            activeDrag = null;
        }

        private void ReplaceItem(BagItemInstance obstruction, Vector2Int origin)
        {
            Vector2Int obstructionOrigin = obstruction.Placement.Value;
            if (!Grid.TryRemove(obstruction))
            {
                throw new InvalidOperationException("Could not remove the validated replacement obstruction.");
            }

            if (!Grid.TryPlace(activeDrag.Item, origin, out PlacementFailureReason failureReason))
            {
                if (!Grid.TryPlace(obstruction, obstructionOrigin, out _))
                {
                    throw new InvalidOperationException("Could not restore an obstruction after replacement failed.");
                }

                RestoreSource();
                throw new InvalidOperationException(
                    $"A validated replacement unexpectedly failed: {failureReason}.");
            }

            Container.AddBottom(obstruction);
            activeDrag = null;
        }

        private static bool CanMerge(BagItemInstance draggedItem, BagItemInstance obstruction)
        {
            return ReferenceEquals(draggedItem.Definition, obstruction.Definition) &&
                   draggedItem.Definition.NextLevelDefinition != null;
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

        private enum DropOperation
        {
            None,
            Place,
            Merge,
            Replace
        }

        private readonly struct DropPlan
        {
            private DropPlan(
                DropOperation operation,
                BagItemInstance obstructingItem,
                PlacementFailureReason failureReason)
            {
                Operation = operation;
                ObstructingItem = obstructingItem;
                FailureReason = failureReason;
            }

            public DropOperation Operation { get; }
            public BagItemInstance ObstructingItem { get; }
            public PlacementFailureReason FailureReason { get; }

            public static DropPlan Valid(DropOperation operation, BagItemInstance obstruction)
            {
                return new DropPlan(operation, obstruction, PlacementFailureReason.None);
            }

            public static DropPlan Invalid(PlacementFailureReason failureReason)
            {
                return new DropPlan(DropOperation.None, null, failureReason);
            }
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
