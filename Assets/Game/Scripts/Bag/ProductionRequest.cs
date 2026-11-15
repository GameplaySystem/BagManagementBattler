using System;
using BagManagementBattler.Combat;

namespace BagManagementBattler.Bag
{
    public readonly struct ProductionRequest
    {
        public ProductionRequest(
            BagItemDefinition soldierDefinition,
            BagItemDefinition weaponDefinition)
        {
            SoldierDefinition = soldierDefinition != null
                ? soldierDefinition
                : throw new ArgumentNullException(nameof(soldierDefinition));
            WeaponDefinition = weaponDefinition != null
                ? weaponDefinition
                : throw new ArgumentNullException(nameof(weaponDefinition));

            if (SoldierDefinition.Role != BagItemRole.Soldier)
            {
                throw new ArgumentException("The soldier definition must have the Soldier role.", nameof(soldierDefinition));
            }

            UnitDefinition = SoldierDefinition.UnitDefinition != null
                ? SoldierDefinition.UnitDefinition
                : throw new ArgumentException(
                    "The soldier definition must reference a unit definition.",
                    nameof(soldierDefinition));

            if (UnitDefinition.Team != CombatTeam.Friendly)
            {
                throw new ArgumentException(
                    "Bag production requires a friendly unit definition.",
                    nameof(soldierDefinition));
            }

            if (WeaponDefinition.Role != BagItemRole.Weapon)
            {
                throw new ArgumentException("The weapon definition must have the Weapon role.", nameof(weaponDefinition));
            }
        }

        public BagItemDefinition SoldierDefinition { get; }
        public UnitDefinition UnitDefinition { get; }
        public BagItemDefinition WeaponDefinition { get; }

        public CombatUnitLoadout ToLoadout()
        {
            return new CombatUnitLoadout(UnitDefinition, WeaponDefinition);
        }
    }
}
