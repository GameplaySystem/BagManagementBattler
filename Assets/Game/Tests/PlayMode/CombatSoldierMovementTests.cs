using System.Collections;
using BagManagementBattler.Bag;
using BagManagementBattler.Combat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BagManagementBattler.Tests
{
    public sealed class CombatSoldierMovementTests
    {
        [UnityTest]
        public IEnumerator SpawnedKinematicSoldier_AdvancesTowardPositiveX()
        {
            BagItemDefinition soldierDefinition = BagItemDefinition.CreateTransient(
                "soldier",
                1,
                BagItemRole.Soldier,
                0f,
                2f,
                new[] { Vector2Int.zero });
            BagItemDefinition weaponDefinition = BagItemDefinition.CreateTransient(
                "weapon",
                1,
                BagItemRole.Weapon,
                1f,
                0f,
                new[] { Vector2Int.zero });

            GameObject prefabObject = new GameObject("Combat Soldier Prefab");
            CombatSoldier prefab = prefabObject.AddComponent<CombatSoldier>();
            GameObject anchorObject = new GameObject("Tent Anchor");
            anchorObject.transform.position = new Vector3(-2.5f, 0.3f, 0f);
            GameObject spawnerObject = new GameObject("Soldier Spawner");
            SoldierSpawner spawner = spawnerObject.AddComponent<SoldierSpawner>();
            spawner.Initialize(prefab, anchorObject.transform, 1);

            CombatSoldier unit = spawner.Spawn(
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
            yield return null;
        }
    }
}
