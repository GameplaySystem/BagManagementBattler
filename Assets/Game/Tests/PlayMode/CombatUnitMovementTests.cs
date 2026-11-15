using System.Collections;
using BagManagementBattler.Bag;
using BagManagementBattler.Combat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BagManagementBattler.Tests
{
    public sealed class CombatUnitMovementTests
    {
        [UnityTest]
        public IEnumerator SpawnedKinematicSoldier_AdvancesTowardPositiveX()
        {
            UnitDefinition unitDefinition = UnitDefinition.CreateTransient(
                CombatTeam.Friendly,
                UnitArchetype.Basic,
                1,
                2f);
            BagItemDefinition soldierDefinition = BagItemDefinition.CreateTransientSoldier(
                "soldier",
                1,
                unitDefinition,
                new[] { Vector2Int.zero });
            BagItemDefinition weaponDefinition = BagItemDefinition.CreateTransientWeapon(
                "weapon",
                1,
                1f,
                1f,
                0.75f,
                new[] { Vector2Int.zero });

            GameObject prefabObject = new GameObject("Combat Soldier Prefab");
            CombatUnit prefab = prefabObject.AddComponent<CombatUnit>();
            GameObject anchorObject = new GameObject("Tent Anchor");
            anchorObject.transform.position = new Vector3(-2.5f, 0.3f, 0f);
            GameObject spawnerObject = new GameObject("Soldier Spawner");
            SoldierSpawner spawner = spawnerObject.AddComponent<SoldierSpawner>();
            spawner.Initialize(prefab, anchorObject.transform, 1);

            CombatUnit unit = spawner.Spawn(
                new ProductionRequest(soldierDefinition, weaponDefinition));
            float startingX = unit.transform.position.x;

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(unit.Body.bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));
            Assert.That(unit.Body.linearVelocity.x, Is.EqualTo(2f).Within(0.001f));
            Assert.That(unit.transform.position.x, Is.GreaterThan(startingX));
            Assert.That(unit.transform.position.y, Is.EqualTo(anchorObject.transform.position.y).Within(0.001f));

            Object.Destroy(spawnerObject);
            Object.Destroy(anchorObject);
            Object.Destroy(prefabObject);
            Object.Destroy(soldierDefinition);
            Object.Destroy(weaponDefinition);
            Object.Destroy(unitDefinition);
            yield return null;
        }
    }
}
