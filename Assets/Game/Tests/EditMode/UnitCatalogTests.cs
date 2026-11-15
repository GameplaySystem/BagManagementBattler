using System;
using BagManagementBattler.Bag;
using BagManagementBattler.Combat;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BagManagementBattler.Tests
{
    public sealed class UnitCatalogTests
    {
        [Test]
        public void Validate_RejectsDuplicateTeamArchetypeAndLevel()
        {
            UnitDefinition first = UnitDefinition.CreateTransient(
                CombatTeam.Friendly, UnitArchetype.Basic, 1, 1f);
            UnitDefinition duplicate = UnitDefinition.CreateTransient(
                CombatTeam.Friendly, UnitArchetype.Basic, 1, 2f);

            Assert.Throws<InvalidOperationException>(() =>
                UnitCatalog.CreateTransient(new[] { first, duplicate }));

            UnityEngine.Object.DestroyImmediate(first);
            UnityEngine.Object.DestroyImmediate(duplicate);
        }

        [Test]
        public void SoldierDefinition_RejectsMissingUnitDefinition()
        {
            Assert.Throws<InvalidOperationException>(() =>
                BagItemDefinition.CreateTransientSoldier(
                    "soldier.test", 1, null, new[] { Vector2Int.zero }));
        }

        [Test]
        public void SoldierDefinition_RejectsEnemyUnitDefinition()
        {
            UnitDefinition enemy = UnitDefinition.CreateTransient(
                CombatTeam.Enemy, UnitArchetype.Basic, 1, 1f);

            Assert.Throws<InvalidOperationException>(() =>
                BagItemDefinition.CreateTransientSoldier(
                    "soldier.test", 1, enemy, new[] { Vector2Int.zero }));

            UnityEngine.Object.DestroyImmediate(enemy);
        }

        [Test]
        public void AuthoredConfigs_PassValidation()
        {
            string[] configFolders = { "Assets/Game/Configs" };
            string[] itemGuids = AssetDatabase.FindAssets("t:BagItemDefinition", configFolders);
            string[] catalogGuids = AssetDatabase.FindAssets("t:UnitCatalog", configFolders);
            Assert.That(itemGuids, Is.Not.Empty);
            Assert.That(catalogGuids, Is.Not.Empty);

            foreach (string guid in itemGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                BagItemDefinition definition = AssetDatabase.LoadAssetAtPath<BagItemDefinition>(path);
                Assert.DoesNotThrow(definition.Validate, path);
            }

            foreach (string guid in catalogGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                UnitCatalog catalog = AssetDatabase.LoadAssetAtPath<UnitCatalog>(path);
                Assert.DoesNotThrow(catalog.Validate, path);
            }
        }

        [Test]
        public void Loadout_ComposesUnitBaseSpeedWithWeaponMultiplier()
        {
            UnitDefinition unit = UnitDefinition.CreateTransient(
                CombatTeam.Enemy, UnitArchetype.Basic, 2, 1.5f);
            BagItemDefinition weapon = BagItemDefinition.CreateTransientWeapon(
                "weapon.test", 1, 2f, 0.8f, 1.25f, new[] { Vector2Int.zero });

            CombatUnitLoadout loadout = new CombatUnitLoadout(unit, weapon);

            Assert.That(loadout.MovementSpeed, Is.EqualTo(1.2f).Within(0.001f));
            Assert.That(loadout.EngagementRange, Is.EqualTo(1.25f));

            UnityEngine.Object.DestroyImmediate(unit);
            UnityEngine.Object.DestroyImmediate(weapon);
        }
    }
}
