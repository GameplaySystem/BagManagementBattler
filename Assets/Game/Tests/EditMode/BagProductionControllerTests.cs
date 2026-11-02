using System.Collections.Generic;
using BagManagementBattler.Bag;
using BagManagementBattler.Combat;
using NUnit.Framework;
using UnityEngine;

namespace BagManagementBattler.Tests
{
    public sealed class BagProductionControllerTests
    {
        private readonly List<Object> createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                {
                    Object.DestroyImmediate(createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void Tick_ForwardsEverySessionRequestToSpawner()
        {
            BagGrid grid = CreateGrid();
            BagItemDefinition soldier = CreateDefinition("soldier", BagItemRole.Soldier, 0f);
            BagItemDefinition weapon = CreateDefinition("weapon", BagItemRole.Weapon, 1f);
            Place(grid, weapon, Vector2Int.zero);
            Place(grid, soldier, Vector2Int.right);
            SoldierSpawner spawner = CreateSpawner();
            BagProductionController controller = CreateController(grid, spawner);

            controller.BeginProduction();
            controller.Tick(2f);

            Assert.That(spawner.ActiveCount, Is.EqualTo(2));
            Assert.That(controller.IsProducing, Is.True);
            Assert.That(controller.ActiveWeaponCount, Is.EqualTo(1));
        }

        [Test]
        public void EndAndRestart_ClearProgressAndStartFreshTimer()
        {
            BagGrid grid = CreateGrid();
            BagItemDefinition soldier = CreateDefinition("soldier", BagItemRole.Soldier, 0f);
            BagItemDefinition weapon = CreateDefinition("weapon", BagItemRole.Weapon, 2f);
            Place(grid, weapon, Vector2Int.zero);
            Place(grid, soldier, Vector2Int.right);
            SoldierSpawner spawner = CreateSpawner();
            BagProductionController controller = CreateController(grid, spawner);
            List<WeaponProductionProgress> progress = new List<WeaponProductionProgress>();

            controller.BeginProduction();
            controller.Tick(1.5f);
            controller.CopyProgressTo(progress);
            Assert.That(progress[0].NormalizedProgress, Is.EqualTo(0.75f).Within(0.001f));

            controller.EndProduction();
            controller.CopyProgressTo(progress);
            Assert.That(progress, Is.Empty);
            Assert.That(controller.IsProducing, Is.False);

            controller.BeginProduction();
            controller.CopyProgressTo(progress);
            Assert.That(progress[0].NormalizedProgress, Is.Zero);
            controller.Tick(0.5f);
            Assert.That(spawner.ActiveCount, Is.Zero);
        }

        [Test]
        public void UnconnectedWeapon_ProducesNothing()
        {
            BagGrid grid = CreateGrid();
            Place(grid, CreateDefinition("weapon", BagItemRole.Weapon, 1f), Vector2Int.zero);
            SoldierSpawner spawner = CreateSpawner();
            BagProductionController controller = CreateController(grid, spawner);

            controller.BeginProduction();
            controller.Tick(10f);

            Assert.That(controller.ActiveWeaponCount, Is.Zero);
            Assert.That(spawner.ActiveCount, Is.Zero);
        }

        private BagProductionController CreateController(BagGrid grid, SoldierSpawner spawner)
        {
            GameObject controllerObject = new GameObject("Production Controller");
            createdObjects.Add(controllerObject);
            BagProductionController controller =
                controllerObject.AddComponent<BagProductionController>();
            controller.Initialize(grid, spawner);
            return controller;
        }

        private SoldierSpawner CreateSpawner()
        {
            GameObject prefabObject = new GameObject("Combat Soldier Prefab");
            createdObjects.Add(prefabObject);
            CombatSoldier prefab = prefabObject.AddComponent<CombatSoldier>();
            GameObject anchorObject = new GameObject("Tent Anchor");
            createdObjects.Add(anchorObject);
            GameObject spawnerObject = new GameObject("Spawner");
            createdObjects.Add(spawnerObject);
            SoldierSpawner spawner = spawnerObject.AddComponent<SoldierSpawner>();
            spawner.Initialize(prefab, anchorObject.transform, 0);
            return spawner;
        }

        private BagGrid CreateGrid()
        {
            BagGridDefinition definition = BagGridDefinition.CreateTransient(
                3,
                2,
                new[]
                {
                    new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
                    new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1)
                });
            createdObjects.Add(definition);
            return new BagGrid(definition);
        }

        private BagItemDefinition CreateDefinition(
            string id,
            BagItemRole role,
            float productionDuration)
        {
            BagItemDefinition definition = BagItemDefinition.CreateTransient(
                id,
                1,
                role,
                productionDuration,
                new[] { Vector2Int.zero });
            createdObjects.Add(definition);
            return definition;
        }

        private static void Place(
            BagGrid grid,
            BagItemDefinition definition,
            Vector2Int origin)
        {
            Assert.That(
                grid.TryPlace(
                    new BagItemInstance(definition),
                    origin,
                    out PlacementFailureReason failure),
                Is.True,
                failure.ToString());
        }
    }
}
