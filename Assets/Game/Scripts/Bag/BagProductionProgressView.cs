using System;
using System.Collections.Generic;
using BagManagementBattler.Combat;
using UnityEngine;

namespace BagManagementBattler.Bag
{
    public sealed class BagProductionProgressView : MonoBehaviour
    {
        private readonly List<WeaponProductionProgress> progress =
            new List<WeaponProductionProgress>();
        private readonly HashSet<Guid> activeWeaponIds = new HashSet<Guid>();
        private readonly HashSet<Guid> previousWeaponIds = new HashSet<Guid>();

        private BagProductionController controller;
        private BagBoardView bagView;

        public void Initialize(
            BagProductionController configuredController,
            BagBoardView configuredBagView)
        {
            controller = configuredController ??
                throw new ArgumentNullException(nameof(configuredController));
            bagView = configuredBagView ??
                throw new ArgumentNullException(nameof(configuredBagView));
        }

        private void LateUpdate()
        {
            if (controller == null)
            {
                return;
            }

            controller.CopyProgressTo(progress);
            activeWeaponIds.Clear();
            foreach (WeaponProductionProgress snapshot in progress)
            {
                activeWeaponIds.Add(snapshot.WeaponInstanceId);
                bagView.SetProductionProgress(
                    snapshot.WeaponInstanceId,
                    snapshot.NormalizedProgress);
            }

            foreach (Guid previousId in previousWeaponIds)
            {
                if (!activeWeaponIds.Contains(previousId))
                {
                    bagView.ClearProductionProgress(previousId);
                }
            }

            previousWeaponIds.Clear();
            foreach (Guid activeId in activeWeaponIds)
            {
                previousWeaponIds.Add(activeId);
            }
        }
    }
}
