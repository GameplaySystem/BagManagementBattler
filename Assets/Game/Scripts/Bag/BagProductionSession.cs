using System;
using System.Collections.Generic;
using UnityEngine;

namespace BagManagementBattler.Bag
{
    public sealed class BagProductionSession
    {
        private static readonly Vector2Int[] OrthogonalDirections =
        {
            Vector2Int.left,
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.up
        };

        private readonly List<WeaponProductionState> activeWeapons =
            new List<WeaponProductionState>();

        public BagProductionSession(BagGrid grid)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            ResolveConnections(grid);
        }

        public int ActiveWeaponCount => activeWeapons.Count;

        public void CopyProgressTo(List<WeaponProductionProgress> destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            destination.Clear();
            foreach (WeaponProductionState weapon in activeWeapons)
            {
                destination.Add(weapon.GetProgress());
            }
        }

        public IReadOnlyList<ProductionRequest> Tick(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaTime),
                    "Production delta time must be finite and non-negative.");
            }

            if (deltaTime == 0f || activeWeapons.Count == 0)
            {
                return Array.Empty<ProductionRequest>();
            }

            List<ProductionRequest> requests = new List<ProductionRequest>();
            foreach (WeaponProductionState weapon in activeWeapons)
            {
                weapon.Advance(deltaTime, requests);
            }

            return requests;
        }

        private void ResolveConnections(BagGrid grid)
        {
            HashSet<BagItemInstance> resolvedWeapons = new HashSet<BagItemInstance>();

            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    BagItemInstance item = grid.GetOccupant(new Vector2Int(x, y));
                    if (item == null ||
                        item.Definition.Role != BagItemRole.Weapon ||
                        !resolvedWeapons.Add(item))
                    {
                        continue;
                    }

                    item.Definition.Validate();
                    List<BagItemDefinition> connectedSoldiers =
                        ResolveConnectedSoldiers(grid, item);
                    if (connectedSoldiers.Count > 0)
                    {
                        activeWeapons.Add(new WeaponProductionState(
                            item.InstanceId,
                            item.Definition,
                            connectedSoldiers));
                    }
                }
            }
        }

        private static List<BagItemDefinition> ResolveConnectedSoldiers(
            BagGrid grid,
            BagItemInstance weapon)
        {
            HashSet<BagItemInstance> uniqueSoldiers = new HashSet<BagItemInstance>();
            List<BagItemDefinition> connectedDefinitions = new List<BagItemDefinition>();
            Vector2Int origin = weapon.Placement.Value;

            foreach (Vector2Int footprintCell in weapon.Definition.Footprint)
            {
                Vector2Int occupiedCell = origin + footprintCell;
                foreach (Vector2Int direction in OrthogonalDirections)
                {
                    Vector2Int neighborCell = occupiedCell + direction;
                    if (!grid.IsInBounds(neighborCell))
                    {
                        continue;
                    }

                    BagItemInstance neighbor = grid.GetOccupant(neighborCell);
                    if (neighbor == null ||
                        neighbor.Definition.Role != BagItemRole.Soldier ||
                        !uniqueSoldiers.Add(neighbor))
                    {
                        continue;
                    }

                    neighbor.Definition.Validate();
                    connectedDefinitions.Add(neighbor.Definition);
                }
            }

            return connectedDefinitions;
        }

        private sealed class WeaponProductionState
        {
            private readonly Guid weaponInstanceId;
            private readonly BagItemDefinition weaponDefinition;
            private readonly IReadOnlyList<BagItemDefinition> connectedSoldiers;
            private double elapsedSeconds;

            public WeaponProductionState(
                Guid weaponInstanceId,
                BagItemDefinition weaponDefinition,
                IReadOnlyList<BagItemDefinition> connectedSoldiers)
            {
                this.weaponInstanceId = weaponInstanceId;
                this.weaponDefinition = weaponDefinition;
                this.connectedSoldiers = connectedSoldiers;
            }

            public WeaponProductionProgress GetProgress()
            {
                float normalized = (float)(elapsedSeconds /
                    weaponDefinition.ProductionDurationSeconds);
                return new WeaponProductionProgress(
                    weaponInstanceId,
                    weaponDefinition,
                    Mathf.Clamp01(normalized));
            }

            public void Advance(float deltaTime, List<ProductionRequest> requests)
            {
                elapsedSeconds += deltaTime;
                double duration = weaponDefinition.ProductionDurationSeconds;
                double completedCyclesValue = Math.Floor(elapsedSeconds / duration);
                if (completedCyclesValue <= 0d)
                {
                    return;
                }

                if (completedCyclesValue > int.MaxValue)
                {
                    throw new InvalidOperationException("A production tick completed too many cycles.");
                }

                int completedCycles = (int)completedCyclesValue;
                elapsedSeconds -= completedCycles * duration;

                for (int cycle = 0; cycle < completedCycles; cycle++)
                {
                    foreach (BagItemDefinition soldierDefinition in connectedSoldiers)
                    {
                        requests.Add(new ProductionRequest(soldierDefinition, weaponDefinition));
                    }
                }
            }
        }
    }
}
