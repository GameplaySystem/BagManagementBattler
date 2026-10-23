using System;
using System.Collections.Generic;
using UnityEngine;

namespace BagManagementBattler.Bag
{
    [CreateAssetMenu(fileName = "BagGridDefinition", menuName = "Bag Game/Bag Grid Definition")]
    public sealed class BagGridDefinition : ScriptableObject
    {
        [Min(1)]
        [SerializeField] private int width = 5;

        [Min(1)]
        [SerializeField] private int height = 4;

        [SerializeField] private List<Vector2Int> initiallyUnlockedCells = new List<Vector2Int>();

        public int Width => width;
        public int Height => height;
        public IReadOnlyList<Vector2Int> InitiallyUnlockedCells => initiallyUnlockedCells;

        public bool IsInBounds(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;
        }

        public bool IsInitiallyUnlocked(Vector2Int cell)
        {
            return initiallyUnlockedCells.Contains(cell);
        }

        public void Validate()
        {
            if (width <= 0 || height <= 0)
            {
                throw new InvalidOperationException("Bag grid dimensions must be greater than zero.");
            }

            if (initiallyUnlockedCells == null)
            {
                throw new InvalidOperationException("The initially unlocked cell collection is missing.");
            }

            HashSet<Vector2Int> uniqueCells = new HashSet<Vector2Int>();
            foreach (Vector2Int cell in initiallyUnlockedCells)
            {
                if (!IsInBounds(cell))
                {
                    throw new InvalidOperationException($"Unlocked cell {cell} is outside the definition.");
                }

                if (!uniqueCells.Add(cell))
                {
                    throw new InvalidOperationException($"Unlocked cell {cell} is duplicated.");
                }
            }
        }

        public static BagGridDefinition CreateTransient(
            int width,
            int height,
            IEnumerable<Vector2Int> initiallyUnlockedCells)
        {
            if (initiallyUnlockedCells == null)
            {
                throw new ArgumentNullException(nameof(initiallyUnlockedCells));
            }

            BagGridDefinition definition = CreateInstance<BagGridDefinition>();
            definition.hideFlags = HideFlags.HideAndDontSave;
            definition.Configure(width, height, initiallyUnlockedCells);
            return definition;
        }

        private void Configure(
            int configuredWidth,
            int configuredHeight,
            IEnumerable<Vector2Int> unlockedCells)
        {
            if (configuredWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(configuredWidth));
            }

            if (configuredHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(configuredHeight));
            }

            width = configuredWidth;
            height = configuredHeight;
            initiallyUnlockedCells = new List<Vector2Int>();

            HashSet<Vector2Int> uniqueCells = new HashSet<Vector2Int>();
            foreach (Vector2Int cell in unlockedCells)
            {
                if (!IsInBounds(cell))
                {
                    throw new ArgumentException($"Unlocked cell {cell} is outside the definition.", nameof(unlockedCells));
                }

                if (uniqueCells.Add(cell))
                {
                    initiallyUnlockedCells.Add(cell);
                }
            }

            Validate();
        }
    }
}
