using System;

namespace BagManagementBattler.Bag
{
    public readonly struct WeaponProductionProgress
    {
        public WeaponProductionProgress(
            Guid weaponInstanceId,
            BagItemDefinition weaponDefinition,
            float normalizedProgress)
        {
            WeaponInstanceId = weaponInstanceId;
            WeaponDefinition = weaponDefinition ??
                throw new ArgumentNullException(nameof(weaponDefinition));
            NormalizedProgress = normalizedProgress;
        }

        public Guid WeaponInstanceId { get; }
        public BagItemDefinition WeaponDefinition { get; }
        public float NormalizedProgress { get; }
    }
}
