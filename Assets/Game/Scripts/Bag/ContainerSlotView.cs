using UnityEngine;

namespace BagManagementBattler.Bag
{
    public sealed class ContainerSlotView : MonoBehaviour
    {
        public BagItemInstance Item { get; private set; }
        public RectTransform RectTransform => (RectTransform)transform;

        public void Bind(BagItemInstance item)
        {
            Item = item;
        }
    }
}
