using System;
using BagManagementBattler.Bag;

namespace BagManagementBattler.Combat
{
    public readonly struct CombatUnitLoadout
    {
        public CombatUnitLoadout(
            UnitDefinition unitDefinition,
            BagItemDefinition weaponDefinition)
        {
            UnitDefinition = unitDefinition != null
                ? unitDefinition
                : throw new ArgumentNullException(nameof(unitDefinition));
            WeaponDefinition = weaponDefinition != null
                ? weaponDefinition
                : throw new ArgumentNullException(nameof(weaponDefinition));

            UnitDefinition.Validate();
            WeaponDefinition.Validate();
            if (WeaponDefinition.Role != BagItemRole.Weapon)
            {
                throw new ArgumentException(
                    "A combat loadout requires a weapon definition.",
                    nameof(weaponDefinition));
            }
        }

        public UnitDefinition UnitDefinition { get; }
        public BagItemDefinition WeaponDefinition { get; }
        public float MovementSpeed =>
            UnitDefinition.BaseMovementSpeed * WeaponDefinition.MovementSpeedMultiplier;
        public float EngagementRange => WeaponDefinition.EngagementRange;
    }
}
