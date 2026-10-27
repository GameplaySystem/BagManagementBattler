using System;
using System.Collections.Generic;

namespace BagManagementBattler.Bag
{
    public sealed class ItemContainer
    {
        private readonly List<BagItemInstance> items = new List<BagItemInstance>();

        public IReadOnlyList<BagItemInstance> Items => items;
        public int Count => items.Count;

        public int IndexOf(BagItemInstance item)
        {
            return items.IndexOf(item);
        }

        public void AddBottom(BagItemInstance item)
        {
            Insert(items.Count, item);
        }

        public void Insert(int index, BagItemInstance item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (item.IsPlaced)
            {
                throw new InvalidOperationException("A placed bag item cannot also be stored in the container.");
            }

            if (items.Contains(item))
            {
                throw new InvalidOperationException("The container already contains this runtime item instance.");
            }

            if (index < 0 || index > items.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            items.Insert(index, item);
        }

        public bool TryRemove(BagItemInstance item, out int originalIndex)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            originalIndex = items.IndexOf(item);
            if (originalIndex < 0)
            {
                return false;
            }

            items.RemoveAt(originalIndex);
            return true;
        }
    }
}
