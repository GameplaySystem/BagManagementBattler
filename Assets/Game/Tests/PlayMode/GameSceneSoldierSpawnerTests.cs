using System.Collections;
using System.Collections.Generic;
using BagManagementBattler.Bag;
using BagManagementBattler.Combat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BagManagementBattler.Tests
{
    public sealed class GameSceneSoldierSpawnerTests
    {
        [UnityTest]
        public IEnumerator GameScene_ProducesConfiguredWeaponsFromAdjacentItems()
        {
            yield return SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Single);
            yield return null;

            GameObject tent = GameObject.Find("Friendly blue base tent sprite-1");
            GameObject spawnerObject = GameObject.Find("Friendly Soldier Spawner");
            GameObject enemySpawnerObject = GameObject.Find("Test Enemy Spawner");
            Assert.That(tent, Is.Not.Null);
            Assert.That(spawnerObject, Is.Not.Null);
            Assert.That(enemySpawnerObject, Is.Not.Null);

            SoldierSpawner spawner = spawnerObject.GetComponent<SoldierSpawner>();
            BagProductionController controller =
                spawnerObject.GetComponent<BagProductionController>();
            Assert.That(spawner, Is.Not.Null);
            Assert.That(controller, Is.Not.Null);
            TestEnemySpawner enemySpawner =
                enemySpawnerObject.GetComponent<TestEnemySpawner>();
            Assert.That(enemySpawner, Is.Not.Null);
            Assert.That(enemySpawner.SpawnedUnit, Is.Not.Null);
            Assert.That(enemySpawner.SpawnedUnit.Team, Is.EqualTo(CombatTeam.Enemy));
            Assert.That(spawner.IsInitialized, Is.True);
            Assert.That(controller.IsProducing, Is.True);
            Assert.That(controller.ActiveWeaponCount, Is.EqualTo(2));
            Assert.That(spawner.ActiveCount, Is.Zero);
            Assert.That(spawner.AvailableCount, Is.EqualTo(4));

            SpriteRenderer rifleFill = FindRifleFill();
            Assert.That(rifleFill, Is.Not.Null);
            Assert.That(rifleFill.transform.parent.Find("Background")
                .GetComponent<SpriteRenderer>().sortingOrder, Is.EqualTo(20));
            Assert.That(rifleFill.sortingOrder, Is.EqualTo(21));
            Assert.That(rifleFill.transform.parent.Find("Visual 0")
                .GetComponent<SpriteRenderer>().sortingOrder, Is.EqualTo(22));
            Assert.That(rifleFill.enabled, Is.True);

            yield return new WaitForSeconds(1.05f);
            Assert.That(spawner.ActiveCount, Is.EqualTo(2));
            Assert.That(spawner.AvailableCount, Is.EqualTo(2));

            List<CombatUnit> producedUnits =
                new List<CombatUnit>(spawner.ActiveUnits);
            CombatUnit rifleUnit = null;
            CombatUnit crowbarUnit = null;
            foreach (CombatUnit unit in producedUnits)
            {
                AssertConfiguredWeaponVisual(unit);
                if (unit.WeaponDefinition.ItemTypeId == "weapon.rifle.basic")
                {
                    rifleUnit = unit;
                }
                else if (unit.WeaponDefinition.ItemTypeId == "weapon.crowbar.basic")
                {
                    crowbarUnit = unit;
                }
            }

            Assert.That(rifleUnit, Is.Not.Null);
            Assert.That(crowbarUnit, Is.Not.Null);
            float startingX = rifleUnit.transform.position.x;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(rifleUnit.CurrentTarget, Is.SameAs(enemySpawner.SpawnedUnit));
            Assert.That(enemySpawner.SpawnedUnit.CurrentTarget, Is.Not.Null);
            Assert.That(rifleUnit.transform.position.x, Is.GreaterThan(startingX));
            Assert.That(rifleUnit.Body.linearVelocity.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(rifleUnit.transform.position.y, Is.EqualTo(tent.transform.position.y).Within(0.001f));

            UnitDefinition unitDefinition = rifleUnit.UnitDefinition;
            BagItemDefinition soldierDefinition = FindSoldierDefinition();
            Assert.That(soldierDefinition.UnitDefinition, Is.SameAs(unitDefinition));
            BagItemDefinition crowbarDefinition = crowbarUnit.WeaponDefinition;
            controller.EndProduction();
            yield return null;
            Assert.That(rifleFill.enabled, Is.False);

            foreach (CombatUnit unit in producedUnits)
            {
                Assert.That(spawner.Release(unit), Is.True);
                Assert.That(unit.IsInitialized, Is.False);
                Assert.That(unit.gameObject.activeSelf, Is.False);
                Assert.That(
                    unit.GetComponentsInChildren<SpriteRenderer>(true),
                    Has.All.Matches<SpriteRenderer>(renderer =>
                        !renderer.enabled && renderer.sprite == null));
            }

            Assert.That(spawner.ActiveCount, Is.Zero);
            Assert.That(spawner.AvailableCount, Is.EqualTo(4));

            CombatUnit[] reusedUnits = new CombatUnit[4];
            for (int index = 0; index < reusedUnits.Length; index++)
            {
                reusedUnits[index] = spawner.Spawn(
                    new ProductionRequest(soldierDefinition, crowbarDefinition));
                AssertConfiguredWeaponVisual(reusedUnits[index]);
            }

            foreach (CombatUnit reusedUnit in reusedUnits)
            {
                Assert.That(spawner.Release(reusedUnit), Is.True);
            }
        }

        private static SpriteRenderer FindRifleFill()
        {
            GameObject items = GameObject.Find("Items");
            Assert.That(items, Is.Not.Null);
            for (int index = 0; index < items.transform.childCount; index++)
            {
                Transform candidate = items.transform.GetChild(index);
                if (candidate.name.StartsWith("weapon.rifle.basic"))
                {
                    return candidate.Find("Production Fill")
                        .GetComponent<SpriteRenderer>();
                }
            }

            return null;
        }

        private static BagItemDefinition FindSoldierDefinition()
        {
            BagInteractionBootstrap bootstrap =
                Object.FindFirstObjectByType<BagInteractionBootstrap>();
            Assert.That(bootstrap, Is.Not.Null);
            return bootstrap.Manager.Grid.GetOccupant(new Vector2Int(2, 1)).Definition;
        }

        private static void AssertConfiguredWeaponVisual(CombatUnit unit)
        {
            Assert.That(unit.IsInitialized, Is.True);
            Assert.That(unit.HandPoint, Is.Not.Null);
            Assert.That(unit.HandPoint.name, Is.EqualTo("HandPoint"));
            Assert.That(unit.UnitDefinition.Team, Is.EqualTo(CombatTeam.Friendly));
            Assert.That(unit.WeaponDefinition.Role, Is.EqualTo(BagItemRole.Weapon));
            Assert.That(unit.Body.bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));
            Assert.That(unit.GetComponent<CapsuleCollider2D>(), Is.Not.Null);

            Transform weaponVisual = unit.HandPoint.Find("Weapon Visual");
            Assert.That(weaponVisual, Is.Not.Null);
            Assert.That(weaponVisual.parent, Is.SameAs(unit.HandPoint));
            Assert.That(
                (Vector2)weaponVisual.localPosition,
                Is.EqualTo(unit.WeaponDefinition.EquippedLocalPosition));
            Assert.That(
                Mathf.DeltaAngle(
                    weaponVisual.localEulerAngles.z,
                    unit.WeaponDefinition.EquippedLocalRotationDegrees),
                Is.Zero.Within(0.001f));
            Assert.That(
                (Vector2)weaponVisual.localScale,
                Is.EqualTo(unit.WeaponDefinition.EquippedLocalScale));

            SpriteRenderer weaponRenderer = weaponVisual.GetComponent<SpriteRenderer>();
            Assert.That(weaponRenderer, Is.Not.Null);
            Assert.That(weaponRenderer.sprite, Is.EqualTo(unit.WeaponDefinition.IconSprite));
            Assert.That(weaponRenderer.sortingOrder, Is.EqualTo(40));
        }
    }
}
