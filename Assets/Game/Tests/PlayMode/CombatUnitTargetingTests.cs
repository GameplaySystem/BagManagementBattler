using System.Collections;
using BagManagementBattler.Bag;
using BagManagementBattler.Combat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BagManagementBattler.Tests
{
    public sealed class CombatUnitTargetingTests
    {
        [UnityTest]
        public IEnumerator OpposingUnits_AcquireChaseStopAndResume()
        {
            UnitDefinition friendlyDefinition = UnitDefinition.CreateTransient(
                CombatTeam.Friendly, UnitArchetype.Basic, 1, 1f);
            UnitDefinition enemyDefinition = UnitDefinition.CreateTransient(
                CombatTeam.Enemy, UnitArchetype.Basic, 1, 1f);
            BagItemDefinition weapon = BagItemDefinition.CreateTransientWeapon(
                "weapon.test", 1, 1f, 1f, 0.5f, new[] { Vector2Int.zero });

            GameObject friendlyObject = new GameObject("Friendly Test Unit");
            CombatUnit friendly = friendlyObject.AddComponent<CombatUnit>();
            friendly.Initialize(
                new CombatUnitLoadout(friendlyDefinition, weapon),
                new Vector3(-1f, 0f, 0f));

            GameObject enemyObject = new GameObject("Enemy Test Unit");
            CombatUnit enemy = enemyObject.AddComponent<CombatUnit>();
            enemy.Initialize(
                new CombatUnitLoadout(enemyDefinition, weapon),
                new Vector3(1f, 0f, 0f));

            Physics2D.SyncTransforms();
            friendly.RefreshTargetNow();
            enemy.RefreshTargetNow();
            yield return new WaitForFixedUpdate();

            Assert.That(friendly.CurrentTarget, Is.SameAs(enemy));
            Assert.That(enemy.CurrentTarget, Is.SameAs(friendly));
            Assert.That(friendly.Body.linearVelocity.x, Is.GreaterThan(0f));
            Assert.That(enemy.Body.linearVelocity.x, Is.LessThan(0f));

            friendly.transform.position = new Vector3(-0.2f, 0f, 0f);
            enemy.transform.position = new Vector3(0.2f, 0f, 0f);
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.That(friendly.Body.linearVelocity, Is.EqualTo(Vector2.zero));
            Assert.That(enemy.Body.linearVelocity, Is.EqualTo(Vector2.zero));

            enemyObject.SetActive(false);
            yield return new WaitForSeconds(0.12f);
            yield return new WaitForFixedUpdate();
            Assert.That(friendly.CurrentTarget, Is.Null);
            Assert.That(friendly.Body.linearVelocity.x, Is.GreaterThan(0f));

            Object.Destroy(friendlyObject);
            Object.Destroy(enemyObject);
            Object.Destroy(friendlyDefinition);
            Object.Destroy(enemyDefinition);
            Object.Destroy(weapon);
            yield return null;
        }
    }
}
