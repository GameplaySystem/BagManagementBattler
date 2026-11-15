using System;
using BagManagementBattler.Bag;
using BagManagementBattler.Combat;
using NUnit.Framework;
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
