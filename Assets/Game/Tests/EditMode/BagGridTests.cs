using System.Collections.Generic;
using BagManagementBattler.Bag;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BagManagementBattler.Tests
{
    public sealed class BagGridTests
    {
        private readonly List<ScriptableObject> transientDefinitions = new List<ScriptableObject>();

        [Test]
        public void DefaultDefinition_IsFiveByFourWithEveryCellUnlocked()
        {
            BagGridDefinition definition = AssetDatabase.LoadAssetAtPath<BagGridDefinition>(
                "Assets/Game/Configs/DefaultBagGrid.asset");

            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.Width, Is.EqualTo(5));
            Assert.That(definition.Height, Is.EqualTo(4));

            foreach (Vector2Int cell in AllCells(5, 4))
            {
                Assert.That(definition.IsInitiallyUnlocked(cell), Is.True, $"Expected {cell} to be unlocked.");
            }
        }

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
        public void TryPlace_MultiCellItem_UsesOneRuntimeInstanceForEveryCell()
        {
            BagGrid grid = CreateGrid(5, 4, AllCells(5, 4));
            BagItemInstance soldier = CreateItem("soldier", Vector2Int.zero, Vector2Int.up);

            bool placed = grid.TryPlace(soldier, new Vector2Int(2, 1), out PlacementFailureReason failure);

            Assert.That(placed, Is.True);
            Assert.That(failure, Is.EqualTo(PlacementFailureReason.None));
            Assert.That(soldier.Placement, Is.EqualTo(new Vector2Int(2, 1)));
            Assert.That(grid.GetOccupant(new Vector2Int(2, 1)), Is.SameAs(soldier));
            Assert.That(grid.GetOccupant(new Vector2Int(2, 2)), Is.SameAs(soldier));
        }

        [Test]
        public void TryPlace_OutOfBounds_ReturnsReasonWithoutChangingState()
        {
            BagGrid grid = CreateGrid(2, 2, AllCells(2, 2));
            BagItemInstance item = CreateItem("soldier", Vector2Int.zero, Vector2Int.up);

            bool placed = grid.TryPlace(item, new Vector2Int(1, 1), out PlacementFailureReason failure);

            Assert.That(placed, Is.False);
            Assert.That(failure, Is.EqualTo(PlacementFailureReason.OutOfBounds));
            Assert.That(item.IsPlaced, Is.False);
            Assert.That(grid.GetOccupant(new Vector2Int(1, 1)), Is.Null);
        }

        [Test]
        public void TryPlace_LockedCell_ReturnsReasonWithoutChangingState()
        {
            BagGrid grid = CreateGrid(2, 2, new[] { Vector2Int.zero });
            BagItemInstance item = CreateItem("weapon", Vector2Int.zero, Vector2Int.right);

            bool placed = grid.TryPlace(item, Vector2Int.zero, out PlacementFailureReason failure);

            Assert.That(placed, Is.False);
            Assert.That(failure, Is.EqualTo(PlacementFailureReason.Locked));
            Assert.That(item.IsPlaced, Is.False);
            Assert.That(grid.GetOccupant(Vector2Int.zero), Is.Null);
        }

        [Test]
        public void TryPlace_OccupiedCell_DoesNotPartiallyPlaceItem()
        {
            BagGrid grid = CreateGrid(3, 1, AllCells(3, 1));
            BagItemInstance blocker = CreateItem("blocker", Vector2Int.zero);
            BagItemInstance candidate = CreateItem("weapon", Vector2Int.zero, Vector2Int.right);
            Assert.That(grid.TryPlace(blocker, Vector2Int.right, out _), Is.True);

            bool placed = grid.TryPlace(candidate, Vector2Int.zero, out PlacementFailureReason failure);

            Assert.That(placed, Is.False);
            Assert.That(failure, Is.EqualTo(PlacementFailureReason.Occupied));
            Assert.That(candidate.IsPlaced, Is.False);
            Assert.That(grid.GetOccupant(Vector2Int.zero), Is.Null);
            Assert.That(grid.GetOccupant(Vector2Int.right), Is.SameAs(blocker));
        }

        [Test]
        public void TryRemove_ClearsCompleteFootprintAndPlacement()
        {
            BagGrid grid = CreateGrid(3, 2, AllCells(3, 2));
            BagItemInstance item = CreateItem("weapon", Vector2Int.zero, Vector2Int.right);
            Assert.That(grid.TryPlace(item, new Vector2Int(1, 0), out _), Is.True);

            bool removed = grid.TryRemove(item);

            Assert.That(removed, Is.True);
            Assert.That(item.IsPlaced, Is.False);
            Assert.That(grid.GetOccupant(new Vector2Int(1, 0)), Is.Null);
            Assert.That(grid.GetOccupant(new Vector2Int(2, 0)), Is.Null);
        }

        [Test]
        public void InstancesSharingDefinition_HaveDistinctRuntimeIdentity()
        {
            BagItemDefinition definition = CreateItemDefinition("soldier", Vector2Int.zero, Vector2Int.up);

            BagItemInstance first = new BagItemInstance(definition);
            BagItemInstance second = new BagItemInstance(definition);

            Assert.That(first.Definition, Is.SameAs(second.Definition));
            Assert.That(first.InstanceId, Is.Not.EqualTo(second.InstanceId));
            Assert.That(first, Is.Not.SameAs(second));
        }

        private BagGrid CreateGrid(int width, int height, IEnumerable<Vector2Int> unlockedCells)
        {
            BagGridDefinition definition = BagGridDefinition.CreateTransient(width, height, unlockedCells);
            transientDefinitions.Add(definition);
            return new BagGrid(definition);
        }

        private BagItemInstance CreateItem(string itemTypeId, params Vector2Int[] footprint)
        {
            return new BagItemInstance(CreateItemDefinition(itemTypeId, footprint));
        }

        private BagItemDefinition CreateItemDefinition(string itemTypeId, params Vector2Int[] footprint)
        {
            BagItemDefinition definition = BagItemDefinition.CreateTransient(itemTypeId, footprint);
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
