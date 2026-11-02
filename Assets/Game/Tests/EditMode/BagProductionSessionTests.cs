using System.Collections.Generic;
using BagManagementBattler.Bag;
using NUnit.Framework;
using UnityEngine;

namespace BagManagementBattler.Tests
{
    public sealed class BagProductionSessionTests
    {
        private readonly List<ScriptableObject> transientDefinitions =
            new List<ScriptableObject>();

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
        public void ResolveConnections_UsesEveryFootprintCellAndDeduplicatesSoldierInstances()
        {
            BagGrid grid = CreateGrid(6, 4);
            BagItemDefinition weaponDefinition = CreateDefinition(
                "weapon",
                BagItemRole.Weapon,
                1f,
                Vector2Int.zero,
                Vector2Int.right);
            BagItemDefinition sharedEdgeSoldierDefinition = CreateDefinition(
                "soldier.shared-edge",
                BagItemRole.Soldier,
                0f,
                Vector2Int.zero,
                Vector2Int.right);
            BagItemDefinition rightSoldierDefinition = CreateDefinition(
                "soldier.right",
                BagItemRole.Soldier,
                0f,
                Vector2Int.zero);
            BagItemDefinition diagonalSoldierDefinition = CreateDefinition(
                "soldier.diagonal",
                BagItemRole.Soldier,
                0f,
                Vector2Int.zero);

            Place(grid, weaponDefinition, new Vector2Int(1, 1));
            Place(grid, sharedEdgeSoldierDefinition, new Vector2Int(1, 2));
            Place(grid, rightSoldierDefinition, new Vector2Int(3, 1));
            Place(grid, diagonalSoldierDefinition, Vector2Int.zero);

            BagProductionSession session = new BagProductionSession(grid);
            IReadOnlyList<ProductionRequest> requests = session.Tick(1f);

            Assert.That(session.ActiveWeaponCount, Is.EqualTo(1));
            Assert.That(requests.Count, Is.EqualTo(2));
            Assert.That(requests[0].SoldierDefinition, Is.SameAs(sharedEdgeSoldierDefinition));
            Assert.That(requests[1].SoldierDefinition, Is.SameAs(rightSoldierDefinition));
            Assert.That(requests, Has.All.Matches<ProductionRequest>(
                request => ReferenceEquals(request.WeaponDefinition, weaponDefinition)));
        }

        [Test]
        public void UnconnectedWeapon_DoesNotCreateActiveProductionState()
        {
            BagGrid grid = CreateGrid(4, 3);
            BagItemDefinition weaponDefinition = CreateDefinition(
                "weapon",
                BagItemRole.Weapon,
                1f,
                Vector2Int.zero);
            Place(grid, weaponDefinition, Vector2Int.zero);

            BagProductionSession session = new BagProductionSession(grid);

            Assert.That(session.ActiveWeaponCount, Is.Zero);
            Assert.That(session.Tick(10f), Is.Empty);
        }

        [Test]
        public void SeparateWeapons_OwnIndependentTimersAndMayProduceForSameSoldier()
        {
            BagGrid grid = CreateGrid(5, 3);
            BagItemDefinition soldierDefinition = CreateDefinition(
                "soldier",
                BagItemRole.Soldier,
                0f,
                Vector2Int.zero);
            BagItemDefinition fastWeaponDefinition = CreateDefinition(
                "weapon.fast",
                BagItemRole.Weapon,
                1f,
                Vector2Int.zero);
            BagItemDefinition slowWeaponDefinition = CreateDefinition(
                "weapon.slow",
                BagItemRole.Weapon,
                2f,
                Vector2Int.zero);

            Place(grid, fastWeaponDefinition, new Vector2Int(1, 1));
            Place(grid, soldierDefinition, new Vector2Int(2, 1));
            Place(grid, slowWeaponDefinition, new Vector2Int(3, 1));

            BagProductionSession session = new BagProductionSession(grid);
            IReadOnlyList<ProductionRequest> firstTick = session.Tick(1f);
            IReadOnlyList<ProductionRequest> secondTick = session.Tick(1f);

            Assert.That(session.ActiveWeaponCount, Is.EqualTo(2));
            Assert.That(firstTick.Count, Is.EqualTo(1));
            Assert.That(firstTick[0].WeaponDefinition, Is.SameAs(fastWeaponDefinition));
            Assert.That(secondTick.Count, Is.EqualTo(2));
            Assert.That(secondTick[0].WeaponDefinition, Is.SameAs(fastWeaponDefinition));
            Assert.That(secondTick[1].WeaponDefinition, Is.SameAs(slowWeaponDefinition));
            Assert.That(firstTick[0].SoldierDefinition, Is.SameAs(soldierDefinition));
            Assert.That(secondTick, Has.All.Matches<ProductionRequest>(
                request => ReferenceEquals(request.SoldierDefinition, soldierDefinition)));
        }

        [Test]
        public void LargeTick_EmitsEveryCompletedCycleAndKeepsRemainder()
        {
            BagGrid grid = CreateGrid(3, 2);
            BagItemDefinition soldierDefinition = CreateDefinition(
                "soldier",
                BagItemRole.Soldier,
                0f,
                Vector2Int.zero);
            BagItemDefinition weaponDefinition = CreateDefinition(
                "weapon",
                BagItemRole.Weapon,
                2f,
                Vector2Int.zero);
            Place(grid, weaponDefinition, Vector2Int.zero);
            Place(grid, soldierDefinition, Vector2Int.right);

            BagProductionSession session = new BagProductionSession(grid);

            Assert.That(session.Tick(5f).Count, Is.EqualTo(2));
            Assert.That(session.Tick(0.9f), Is.Empty);
            IReadOnlyList<ProductionRequest> remainderCompletion = session.Tick(0.2f);
            Assert.That(remainderCompletion.Count, Is.EqualTo(1));
            Assert.That(remainderCompletion[0].SoldierDefinition, Is.SameAs(soldierDefinition));
            Assert.That(remainderCompletion[0].WeaponDefinition, Is.SameAs(weaponDefinition));
        }

        [Test]
        public void NewSession_StartsEveryWeaponTimerAtZero()
        {
            BagGrid grid = CreateGrid(3, 2);
            BagItemDefinition soldierDefinition = CreateDefinition(
                "soldier",
                BagItemRole.Soldier,
                0f,
                Vector2Int.zero);
            BagItemDefinition weaponDefinition = CreateDefinition(
                "weapon",
                BagItemRole.Weapon,
                2f,
                Vector2Int.zero);
            Place(grid, weaponDefinition, Vector2Int.zero);
            Place(grid, soldierDefinition, Vector2Int.right);

            BagProductionSession firstBattle = new BagProductionSession(grid);
            Assert.That(firstBattle.Tick(1.5f), Is.Empty);

            BagProductionSession nextBattle = new BagProductionSession(grid);
            Assert.That(nextBattle.Tick(0.5f), Is.Empty);
            Assert.That(nextBattle.Tick(1.5f).Count, Is.EqualTo(1));
        }

        [Test]
        public void Progress_IdentifiesRuntimeWeaponAndTracksTimerRemainder()
        {
            BagGrid grid = CreateGrid(3, 2);
            BagItemDefinition soldierDefinition = CreateDefinition(
                "soldier",
                BagItemRole.Soldier,
                0f,
                Vector2Int.zero);
            BagItemDefinition weaponDefinition = CreateDefinition(
                "weapon",
                BagItemRole.Weapon,
                2f,
                Vector2Int.zero);
            BagItemInstance weapon = Place(grid, weaponDefinition, Vector2Int.zero);
            Place(grid, soldierDefinition, Vector2Int.right);
            BagProductionSession session = new BagProductionSession(grid);
            List<WeaponProductionProgress> progress = new List<WeaponProductionProgress>();

            session.CopyProgressTo(progress);
            Assert.That(progress.Count, Is.EqualTo(1));
            Assert.That(progress[0].WeaponInstanceId, Is.EqualTo(weapon.InstanceId));
            Assert.That(progress[0].WeaponDefinition, Is.SameAs(weaponDefinition));
            Assert.That(progress[0].NormalizedProgress, Is.Zero);

            session.Tick(2.5f);
            session.CopyProgressTo(progress);
            Assert.That(progress[0].NormalizedProgress, Is.EqualTo(0.25f).Within(0.001f));
        }

        private BagGrid CreateGrid(int width, int height)
        {
            BagGridDefinition definition =
                BagGridDefinition.CreateTransient(width, height, AllCells(width, height));
            transientDefinitions.Add(definition);
            return new BagGrid(definition);
        }

        private BagItemDefinition CreateDefinition(
            string itemTypeId,
            BagItemRole role,
            float productionDuration,
            params Vector2Int[] footprint)
        {
            BagItemDefinition definition = BagItemDefinition.CreateTransient(
                itemTypeId,
                1,
                role,
                productionDuration,
                footprint);
            transientDefinitions.Add(definition);
            return definition;
        }

        private static BagItemInstance Place(
            BagGrid grid,
            BagItemDefinition definition,
            Vector2Int origin)
        {
            BagItemInstance item = new BagItemInstance(definition);
            Assert.That(
                grid.TryPlace(item, origin, out PlacementFailureReason failure),
                Is.True,
                $"Could not place {definition.ItemTypeId}: {failure}.");
            return item;
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
