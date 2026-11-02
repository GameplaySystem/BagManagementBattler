using System;

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

            if (WeaponDefinition.Role != BagItemRole.Weapon)
            {
                throw new ArgumentException("The weapon definition must have the Weapon role.", nameof(weaponDefinition));
            }
        }

        public BagItemDefinition SoldierDefinition { get; }
        public BagItemDefinition WeaponDefinition { get; }
    }
}
