using System.Collections.Generic;
using BagManagementBattler.Bag;
using NUnit.Framework;
using UnityEngine;

namespace BagManagementBattler.Tests
{
    public sealed class BagManagerTests
    {
        private readonly List<ScriptableObject> transientDefinitions = new List<ScriptableObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (ScriptableObject definition in transientDefinitions)
            {
                Object.DestroyImmediate(definition);
            }

            transientDefinitions.Clear();
        }

        [Test]
        public void BeginDrag_FromBag_ReleasesEveryOccupiedCell()
        {
            BagManager manager = CreateManager();
            BagItemInstance item = CreateItem("soldier", Vector2Int.zero, Vector2Int.up);
            Assert.That(manager.Grid.TryPlace(item, new Vector2Int(1, 0), out _), Is.True);

            bool began = manager.BeginDrag(item);

            Assert.That(began, Is.True);
            Assert.That(manager.DraggedItem, Is.SameAs(item));
            Assert.That(item.IsPlaced, Is.False);
            Assert.That(manager.Grid.GetOccupant(new Vector2Int(1, 0)), Is.Null);
            Assert.That(manager.Grid.GetOccupant(new Vector2Int(1, 1)), Is.Null);
        }

        [Test]
        public void TryDropToBag_ValidDestination_CommitsSameRuntimeInstance()
        {
            BagManager manager = CreateManager();
            BagItemInstance item = CreateItem("soldier", Vector2Int.zero, Vector2Int.up);
            manager.Container.AddBottom(item);
            Assert.That(manager.BeginDrag(item), Is.True);

            bool dropped = manager.TryDropToBag(new Vector2Int(2, 1), out PlacementFailureReason failure);

            Assert.That(dropped, Is.True);
            Assert.That(failure, Is.EqualTo(PlacementFailureReason.None));
            Assert.That(manager.IsDragging, Is.False);
            Assert.That(manager.Container.Count, Is.Zero);
            Assert.That(manager.Grid.GetOccupant(new Vector2Int(2, 1)), Is.SameAs(item));
            Assert.That(manager.Grid.GetOccupant(new Vector2Int(2, 2)), Is.SameAs(item));
        }

        [Test]
        public void TryDropToBag_InvalidDestination_RestoresBagOrigin()
        {
            BagManager manager = CreateManager();
            BagItemInstance item = CreateItem("soldier", Vector2Int.zero, Vector2Int.up);
            Vector2Int originalOrigin = new Vector2Int(1, 0);
            Assert.That(manager.Grid.TryPlace(item, originalOrigin, out _), Is.True);
            Assert.That(manager.BeginDrag(item), Is.True);

            bool dropped = manager.TryDropToBag(new Vector2Int(4, 3), out PlacementFailureReason failure);

            Assert.That(dropped, Is.False);
            Assert.That(failure, Is.EqualTo(PlacementFailureReason.OutOfBounds));
            Assert.That(item.Placement, Is.EqualTo(originalOrigin));
            Assert.That(manager.Grid.GetOccupant(originalOrigin), Is.SameAs(item));
            Assert.That(manager.Grid.GetOccupant(originalOrigin + Vector2Int.up), Is.SameAs(item));
        }

        [Test]
        public void TryDropToBag_MatchingItems_MergesIntoDestinationInstance()
        {
            BagManager manager = CreateManager();
            BagItemDefinition levelTwo = CreateDefinition("crowbar", 2, null, Vector2Int.zero);
            BagItemDefinition levelOne = CreateDefinition("crowbar", 1, levelTwo, Vector2Int.zero);
            BagItemInstance destination = new BagItemInstance(levelOne);
            BagItemInstance dragged = new BagItemInstance(levelOne);
            Assert.That(manager.Grid.TryPlace(destination, Vector2Int.zero, out _), Is.True);
            manager.Container.AddBottom(dragged);
            Assert.That(manager.BeginDrag(dragged), Is.True);
            Assert.That(manager.ValidateDropToBag(Vector2Int.zero), Is.EqualTo(PlacementFailureReason.None));

            bool dropped = manager.TryDropToBag(Vector2Int.zero, out PlacementFailureReason failure);

            Assert.That(dropped, Is.True);
            Assert.That(failure, Is.EqualTo(PlacementFailureReason.None));
            Assert.That(manager.IsDragging, Is.False);
            Assert.That(destination.Definition, Is.SameAs(levelTwo));
            Assert.That(destination.Level, Is.EqualTo(2));
            Assert.That(manager.Grid.GetOccupant(Vector2Int.zero), Is.SameAs(destination));
            Assert.That(manager.Container.Count, Is.Zero);
            Assert.That(dragged.IsPlaced, Is.False);
        }

        [Test]
        public void TryDropToBag_IncompatibleSingleBlocker_ReplacesAndAppendsBlocker()
        {
            BagManager manager = CreateManager();
            BagItemInstance existing = CreateItem("existing", Vector2Int.zero);
            BagItemInstance dragged = CreateItem("rifle", Vector2Int.zero, Vector2Int.right);
            BagItemInstance retained = CreateItem("retained", Vector2Int.zero);
            Vector2Int destination = new Vector2Int(1, 1);
            Assert.That(manager.Grid.TryPlace(existing, destination, out _), Is.True);
            manager.Container.AddBottom(dragged);
            manager.Container.AddBottom(retained);
            Assert.That(manager.BeginDrag(dragged), Is.True);
            Assert.That(manager.ValidateDropToBag(destination), Is.EqualTo(PlacementFailureReason.None));

            bool dropped = manager.TryDropToBag(destination, out PlacementFailureReason failure);

            Assert.That(dropped, Is.True);
            Assert.That(failure, Is.EqualTo(PlacementFailureReason.None));
            Assert.That(manager.Grid.GetOccupant(destination), Is.SameAs(dragged));
            Assert.That(manager.Grid.GetOccupant(destination + Vector2Int.right), Is.SameAs(dragged));
            Assert.That(manager.Container.Count, Is.EqualTo(2));
            Assert.That(manager.Container.Items[0], Is.SameAs(retained));
            Assert.That(manager.Container.Items[1], Is.SameAs(existing));
        }

        [Test]
        public void TryDropToBag_MaxLevelMatch_UsesReplacement()
        {
            BagManager manager = CreateManager();
            BagItemDefinition maximumLevel = CreateDefinition("crowbar", 2, null, Vector2Int.zero);
            BagItemInstance existing = new BagItemInstance(maximumLevel);
            BagItemInstance dragged = new BagItemInstance(maximumLevel);
            Assert.That(manager.Grid.TryPlace(existing, Vector2Int.zero, out _), Is.True);
            manager.Container.AddBottom(dragged);
            Assert.That(manager.BeginDrag(dragged), Is.True);

            bool dropped = manager.TryDropToBag(Vector2Int.zero, out PlacementFailureReason failure);

            Assert.That(dropped, Is.True);
            Assert.That(failure, Is.EqualTo(PlacementFailureReason.None));
            Assert.That(manager.Grid.GetOccupant(Vector2Int.zero), Is.SameAs(dragged));
            Assert.That(manager.Container.Count, Is.EqualTo(1));
            Assert.That(manager.Container.Items[0], Is.SameAs(existing));
            Assert.That(existing.Level, Is.EqualTo(2));
            Assert.That(dragged.Level, Is.EqualTo(2));
        }

        [Test]
        public void TryDropToBag_DifferentLevelsOfSameFamily_UsesReplacement()
        {
            BagManager manager = CreateManager();
            BagItemDefinition levelTwo = CreateDefinition("crowbar", 2, null, Vector2Int.zero);
            BagItemDefinition levelOne = CreateDefinition("crowbar", 1, levelTwo, Vector2Int.zero);
            BagItemInstance existing = new BagItemInstance(levelTwo);
            BagItemInstance dragged = new BagItemInstance(levelOne);
            Assert.That(manager.Grid.TryPlace(existing, Vector2Int.zero, out _), Is.True);
            manager.Container.AddBottom(dragged);
            Assert.That(manager.BeginDrag(dragged), Is.True);

            bool dropped = manager.TryDropToBag(Vector2Int.zero, out PlacementFailureReason failure);

            Assert.That(dropped, Is.True);
            Assert.That(failure, Is.EqualTo(PlacementFailureReason.None));
            Assert.That(manager.Grid.GetOccupant(Vector2Int.zero), Is.SameAs(dragged));
            Assert.That(manager.Container.Items, Is.EqualTo(new[] { existing }));
            Assert.That(existing.Level, Is.EqualTo(2));
            Assert.That(dragged.Level, Is.EqualTo(1));
        }

        [Test]
        public void TryDropToBag_MultipleBlockers_RestoresContainerIndex()
        {
            BagManager manager = CreateManager();
            BagItemInstance first = CreateItem("first", Vector2Int.zero);
            BagItemInstance dragged = CreateItem("dragged", Vector2Int.zero, Vector2Int.right);
            BagItemInstance last = CreateItem("last", Vector2Int.zero);
            BagItemInstance firstBlocker = CreateItem("first-blocker", Vector2Int.zero);
            BagItemInstance secondBlocker = CreateItem("second-blocker", Vector2Int.zero);
            manager.Container.AddBottom(first);
            manager.Container.AddBottom(dragged);
            manager.Container.AddBottom(last);
            Assert.That(manager.Grid.TryPlace(firstBlocker, Vector2Int.zero, out _), Is.True);
            Assert.That(manager.Grid.TryPlace(secondBlocker, Vector2Int.right, out _), Is.True);
            Assert.That(manager.BeginDrag(dragged), Is.True);

            bool dropped = manager.TryDropToBag(Vector2Int.zero, out PlacementFailureReason failure);

            Assert.That(dropped, Is.False);
            Assert.That(failure, Is.EqualTo(PlacementFailureReason.Occupied));
            Assert.That(manager.Container.Items[0], Is.SameAs(first));
            Assert.That(manager.Container.Items[1], Is.SameAs(dragged));
            Assert.That(manager.Container.Items[2], Is.SameAs(last));
            Assert.That(manager.Grid.GetOccupant(Vector2Int.zero), Is.SameAs(firstBlocker));
            Assert.That(manager.Grid.GetOccupant(Vector2Int.right), Is.SameAs(secondBlocker));
        }

        [Test]
        public void DropToContainer_FromBag_AppendsItemOnce()
        {
            BagManager manager = CreateManager();
            BagItemInstance existing = CreateItem("existing", Vector2Int.zero);
            BagItemInstance item = CreateItem("weapon", Vector2Int.zero);
            manager.Container.AddBottom(existing);
            Assert.That(manager.Grid.TryPlace(item, Vector2Int.zero, out _), Is.True);
            Assert.That(manager.BeginDrag(item), Is.True);

            manager.DropToContainer();

            Assert.That(manager.Container.Count, Is.EqualTo(2));
            Assert.That(manager.Container.Items[0], Is.SameAs(existing));
            Assert.That(manager.Container.Items[1], Is.SameAs(item));
            Assert.That(item.IsPlaced, Is.False);
        }

        [Test]
        public void DropToContainer_FromContainer_RestoresOriginalIndex()
        {
            BagManager manager = CreateManager();
            BagItemInstance first = CreateItem("first", Vector2Int.zero);
            BagItemInstance dragged = CreateItem("dragged", Vector2Int.zero);
            BagItemInstance last = CreateItem("last", Vector2Int.zero);
            manager.Container.AddBottom(first);
            manager.Container.AddBottom(dragged);
            manager.Container.AddBottom(last);
            Assert.That(manager.BeginDrag(dragged), Is.True);

            manager.DropToContainer();

            Assert.That(manager.Container.Items[0], Is.SameAs(first));
            Assert.That(manager.Container.Items[1], Is.SameAs(dragged));
            Assert.That(manager.Container.Items[2], Is.SameAs(last));
        }

        [Test]
        public void CancelDrag_RestoresExactSource()
        {
            BagManager manager = CreateManager();
            BagItemInstance item = CreateItem("weapon", Vector2Int.zero, Vector2Int.right);
            Vector2Int originalOrigin = new Vector2Int(2, 2);
            Assert.That(manager.Grid.TryPlace(item, originalOrigin, out _), Is.True);
            Assert.That(manager.BeginDrag(item), Is.True);

            manager.CancelDrag();

            Assert.That(item.Placement, Is.EqualTo(originalOrigin));
            Assert.That(manager.Grid.GetOccupant(originalOrigin), Is.SameAs(item));
            Assert.That(manager.Grid.GetOccupant(originalOrigin + Vector2Int.right), Is.SameAs(item));
            Assert.That(manager.Container.Count, Is.Zero);
        }

        private BagManager CreateManager()
        {
            BagGridDefinition definition = BagGridDefinition.CreateTransient(5, 4, AllCells(5, 4));
            transientDefinitions.Add(definition);
            return new BagManager(new BagGrid(definition), new ItemContainer());
        }

        private BagItemInstance CreateItem(string itemTypeId, params Vector2Int[] footprint)
        {
            BagItemDefinition definition = CreateDefinition(itemTypeId, 1, null, footprint);
            return new BagItemInstance(definition);
        }

        private BagItemDefinition CreateDefinition(
            string itemTypeId,
            int level,
            BagItemDefinition nextLevel,
            params Vector2Int[] footprint)
        {
            BagItemDefinition definition =
                BagItemDefinition.CreateTransient(itemTypeId, level, footprint, nextLevel);
            transientDefinitions.Add(definition);
            return definition;
        }

        private static IEnumerable<Vector2Int> AllCells(int width, int height)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    yield return new Vector2Int(x, y);
                }
            }
        }
    }
}
