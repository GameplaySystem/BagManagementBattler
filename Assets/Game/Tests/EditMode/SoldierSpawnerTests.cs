using System.Collections.Generic;
using BagManagementBattler.Bag;
using BagManagementBattler.Combat;
using NUnit.Framework;
using UnityEngine;

namespace BagManagementBattler.Tests
{
    public sealed class SoldierSpawnerTests
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
        public void Spawn_InitializesUnitAtAnchor_AndReleaseResetsItForReuse()
        {
            SoldierSpawner spawner = CreateSpawner(1, out Transform anchor);
            ProductionRequest request = CreateRequest(2f);

            CombatUnit firstSpawn = spawner.Spawn(request);

            Assert.That(spawner.ActiveCount, Is.EqualTo(1));
            Assert.That(spawner.AvailableCount, Is.Zero);
            Assert.That(firstSpawn.gameObject.activeSelf, Is.True);
            Assert.That(firstSpawn.IsInitialized, Is.True);
            Assert.That(firstSpawn.transform.position, Is.EqualTo(anchor.position));
            Assert.That(firstSpawn.UnitDefinition, Is.SameAs(request.UnitDefinition));
            Assert.That(firstSpawn.WeaponDefinition, Is.SameAs(request.WeaponDefinition));
            Assert.That(firstSpawn.Body.bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));

            firstSpawn.Body.linearVelocity = new Vector2(4f, 2f);
            Assert.That(spawner.Release(firstSpawn), Is.True);

            Assert.That(spawner.ActiveCount, Is.Zero);
            Assert.That(spawner.AvailableCount, Is.EqualTo(1));
            Assert.That(firstSpawn.gameObject.activeSelf, Is.False);
            Assert.That(firstSpawn.IsInitialized, Is.False);
            Assert.That(firstSpawn.UnitDefinition, Is.Null);
            Assert.That(firstSpawn.WeaponDefinition, Is.Null);
            Assert.That(firstSpawn.Body.linearVelocity, Is.EqualTo(Vector2.zero));
            Assert.That(firstSpawn.transform.position, Is.EqualTo(anchor.position));

            CombatUnit reused = spawner.Spawn(request);
            Assert.That(reused, Is.SameAs(firstSpawn));
            Assert.That(reused.IsInitialized, Is.True);
        }

        [Test]
        public void Spawn_ExpandsPoolWhenEveryPrewarmedUnitIsActive()
        {
            SoldierSpawner spawner = CreateSpawner(1, out _);
            ProductionRequest request = CreateRequest(1f);

            CombatUnit first = spawner.Spawn(request);
            CombatUnit second = spawner.Spawn(request);

            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(spawner.ActiveCount, Is.EqualTo(2));
            Assert.That(spawner.AvailableCount, Is.Zero);
            Assert.That(spawner.Release(first), Is.True);
            Assert.That(spawner.Release(second), Is.True);
            Assert.That(spawner.AvailableCount, Is.EqualTo(2));
        }

        [Test]
        public void Release_RejectsForeignAndAlreadyReleasedUnits()
        {
            SoldierSpawner spawner = CreateSpawner(1, out _);
            ProductionRequest request = CreateRequest(1f);
            CombatUnit owned = spawner.Spawn(request);

            GameObject foreignObject = new GameObject("Foreign Soldier");
            createdObjects.Add(foreignObject);
            CombatUnit foreign = foreignObject.AddComponent<CombatUnit>();

            Assert.That(spawner.Release(foreign), Is.False);
            Assert.That(spawner.Release(owned), Is.True);
            Assert.That(spawner.Release(owned), Is.False);
        }

        private SoldierSpawner CreateSpawner(int prewarmCount, out Transform anchor)
        {
            GameObject prefabObject = new GameObject("Combat Soldier Prefab");
            createdObjects.Add(prefabObject);
            CombatUnit prefab = prefabObject.AddComponent<CombatUnit>();

            GameObject anchorObject = new GameObject("Tent Anchor");
            createdObjects.Add(anchorObject);
            anchorObject.transform.position = new Vector3(-2.5f, 0.3f, 0f);
            anchor = anchorObject.transform;

            GameObject spawnerObject = new GameObject("Soldier Spawner");
            createdObjects.Add(spawnerObject);
            SoldierSpawner spawner = spawnerObject.AddComponent<SoldierSpawner>();
            spawner.Initialize(prefab, anchor, prewarmCount);
            return spawner;
        }

        private ProductionRequest CreateRequest(float movementSpeed)
        {
            UnitDefinition unitDefinition = UnitDefinition.CreateTransient(
                CombatTeam.Friendly,
                UnitArchetype.Basic,
                1,
                movementSpeed);
            BagItemDefinition soldier = BagItemDefinition.CreateTransientSoldier(
                "soldier",
                1,
                unitDefinition,
                new[] { Vector2Int.zero });
            BagItemDefinition weapon = BagItemDefinition.CreateTransientWeapon(
                "weapon",
                1,
                1f,
                1f,
                0.75f,
                new[] { Vector2Int.zero });
            createdObjects.Add(unitDefinition);
            createdObjects.Add(soldier);
            createdObjects.Add(weapon);
            return new ProductionRequest(soldier, weapon);
        }
    }
}
