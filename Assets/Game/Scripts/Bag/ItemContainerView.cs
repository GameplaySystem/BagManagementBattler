using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BagManagementBattler.Bag
{
    public sealed class ItemContainerView : MonoBehaviour
    {
        private const float ContainerCellPixels = 96f;
        private const float DragCellPixels = 130f;
        private static readonly Color PanelColor = new Color(0.05f, 0.08f, 0.14f, 0.88f);

        private ItemContainer container;
        private Sprite itemBackground1x1;
        private Sprite itemBackground1x2;
        private Sprite itemBackground2x1;
        private RectTransform panel;
        private RectTransform content;
        private readonly List<ContainerSlotView> slots = new List<ContainerSlotView>();
        private RectTransform dragGhost;
        private CanvasGroup dragGhostCanvasGroup;
        private Canvas canvas;

        public void Initialize(
            ItemContainer container,
            Sprite itemBackground1x1,
            Sprite itemBackground1x2,
            Sprite itemBackground2x1)
        {
            this.container = container ?? throw new ArgumentNullException(nameof(container));
            this.itemBackground1x1 = itemBackground1x1;
            this.itemBackground1x2 = itemBackground1x2;
            this.itemBackground2x1 = itemBackground2x1;
            CreateEventSystemIfNeeded();
            BuildCanvas();
            Refresh();
        }

        public bool Contains(Vector2 screenPosition)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(panel, screenPosition);
        }

        public bool TryGetItem(Vector2 screenPosition, out BagItemInstance item)
        {
            foreach (ContainerSlotView slot in slots)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(slot.RectTransform, screenPosition))
                {
                    item = slot.Item;
                    return true;
                }
            }

            item = null;
            return false;
        }

        public void Refresh()
        {
            slots.Clear();
            for (int index = content.childCount - 1; index >= 0; index--)
            {
                GameObject child = content.GetChild(index).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            foreach (BagItemInstance item in container.Items)
            {
                CreateSlot(item);
            }
        }

        public void ShowGhost(BagItemInstance item, Vector2 screenPosition)
        {
            ClearChildren(dragGhost);
            Vector2Int footprintSize = GetFootprintSize(item.Definition.Footprint);
            dragGhost.sizeDelta = new Vector2(
                footprintSize.x * DragCellPixels,
                footprintSize.y * DragCellPixels);
            CreateCompositeVisual(dragGhost, item, DragCellPixels, false);
            dragGhostCanvasGroup.alpha = 1f;
            dragGhost.position = screenPosition;
            dragGhost.gameObject.SetActive(true);
        }

        public void MoveGhost(Vector2 screenPosition, bool validDestination)
        {
            dragGhost.position = screenPosition;
            dragGhostCanvasGroup.alpha = validDestination ? 1f : 0.65f;
        }

        public void HideGhost()
        {
            dragGhost.gameObject.SetActive(false);
        }

        private void BuildCanvas()
        {
            GameObject canvasObject = new GameObject("Bag Interface Canvas");
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1000f, 2000f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject panelObject = CreateUiObject("Item Container", canvasObject.transform);
            panel = panelObject.GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0f, 0f);
            panel.anchorMax = new Vector2(1f, 0f);
            panel.pivot = new Vector2(0.5f, 0f);
            panel.offsetMin = new Vector2(24f, 24f);
            panel.offsetMax = new Vector2(-24f, 264f);
            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = PanelColor;

            GameObject viewportObject = CreateUiObject("Viewport", panel);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(20f, 20f);
            viewport.offsetMax = new Vector2(-20f, -20f);
            Image viewportImage = viewportObject.AddComponent<Image>();
            viewportImage.color = Color.white;
            Mask mask = viewportObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject contentObject = CreateUiObject("Slots", viewport);
            content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 0.5f);
            content.anchorMax = new Vector2(0f, 0.5f);
            content.pivot = new Vector2(0f, 0.5f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            HorizontalLayoutGroup layout = contentObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scrollRect = panelObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 35f;

            GameObject ghostObject = CreateUiObject("Drag Ghost", canvasObject.transform);
            dragGhost = ghostObject.GetComponent<RectTransform>();
            dragGhost.anchorMin = new Vector2(0.5f, 0.5f);
            dragGhost.anchorMax = new Vector2(0.5f, 0.5f);
            dragGhost.pivot = new Vector2(0.5f, 0.5f);
            dragGhostCanvasGroup = ghostObject.AddComponent<CanvasGroup>();
            dragGhostCanvasGroup.blocksRaycasts = false;
            dragGhostCanvasGroup.interactable = false;
            dragGhost.gameObject.SetActive(false);
        }

        private void CreateSlot(BagItemInstance item)
        {
            GameObject slotObject = CreateUiObject(item.Definition.ItemTypeId, content);
            RectTransform slotRect = slotObject.GetComponent<RectTransform>();
            Vector2Int footprintSize = GetFootprintSize(item.Definition.Footprint);
            Vector2 slotSize = new Vector2(
                footprintSize.x * ContainerCellPixels,
                footprintSize.y * ContainerCellPixels);
            slotRect.sizeDelta = slotSize;
            LayoutElement slotLayout = slotObject.AddComponent<LayoutElement>();
            slotLayout.minWidth = slotSize.x;
            slotLayout.minHeight = slotSize.y;
            slotLayout.preferredWidth = slotSize.x;
            slotLayout.preferredHeight = slotSize.y;
            CreateCompositeVisual(slotRect, item, ContainerCellPixels, true);

            ContainerSlotView slot = slotObject.AddComponent<ContainerSlotView>();
            slot.Bind(item);
            slots.Add(slot);
        }

        private void CreateCompositeVisual(
            RectTransform root,
            BagItemInstance item,
            float cellPixels,
            bool backgroundOnRoot)
        {
            Image background;
            if (backgroundOnRoot)
            {
                background = root.gameObject.AddComponent<Image>();
            }
            else
            {
                GameObject backgroundObject = CreateUiObject("Background", root);
                RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
                backgroundRect.anchorMin = Vector2.zero;
                backgroundRect.anchorMax = Vector2.one;
                backgroundRect.offsetMin = Vector2.zero;
                backgroundRect.offsetMax = Vector2.zero;
                background = backgroundObject.AddComponent<Image>();
                background.raycastTarget = false;
            }

            background.sprite = SelectBackground(item.Definition.Footprint);
            background.type = Image.Type.Simple;
            background.preserveAspect = false;
            background.color = item.Definition.ItemColor;

            int partIndex = 0;
            foreach (BagItemVisualPart part in item.Definition.VisualParts)
            {
                if (part.Sprite == null)
                {
                    continue;
                }

                CreateVisualPart(root, part.Sprite, part.LocalPositionInCells, part.ScaleInCells, cellPixels, partIndex++);
            }

            if (partIndex == 0 && item.Definition.IconSprite != null)
            {
                CreateFallbackIcon(root, item.Definition.IconSprite);
            }
        }

        private static void CreateVisualPart(
            RectTransform root,
            Sprite sprite,
            Vector2 positionInCells,
            float scaleInCells,
            float cellPixels,
            int partIndex)
        {
            GameObject partObject = CreateUiObject($"Visual {partIndex}", root);
            RectTransform partRect = partObject.GetComponent<RectTransform>();
            partRect.anchorMin = new Vector2(0.5f, 0.5f);
            partRect.anchorMax = new Vector2(0.5f, 0.5f);
            partRect.pivot = new Vector2(
                sprite.pivot.x / sprite.rect.width,
                sprite.pivot.y / sprite.rect.height);
            partRect.anchoredPosition = positionInCells * cellPixels;
            Vector2 spriteSizeInCells = sprite.bounds.size * scaleInCells;
            partRect.sizeDelta = spriteSizeInCells * cellPixels;

            Image partImage = partObject.AddComponent<Image>();
            partImage.sprite = sprite;
            partImage.preserveAspect = true;
            partImage.raycastTarget = false;
        }

        private static void CreateFallbackIcon(RectTransform root, Sprite sprite)
        {
            GameObject iconObject = CreateUiObject("Icon", root);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.12f, 0.12f);
            iconRect.anchorMax = new Vector2(0.88f, 0.88f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            Image icon = iconObject.AddComponent<Image>();
            icon.sprite = sprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }

        private static Vector2Int GetFootprintSize(IReadOnlyList<Vector2Int> footprint)
        {
            Vector2Int min = footprint[0];
            Vector2Int max = footprint[0];

            for (int index = 1; index < footprint.Count; index++)
            {
                min = Vector2Int.Min(min, footprint[index]);
                max = Vector2Int.Max(max, footprint[index]);
            }

            return max - min + Vector2Int.one;
        }

        private static void ClearChildren(RectTransform root)
        {
            for (int index = root.childCount - 1; index >= 0; index--)
            {
                GameObject child = root.GetChild(index).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        private Sprite SelectBackground(IReadOnlyList<Vector2Int> footprint)
        {
            Vector2Int min = footprint[0];
            Vector2Int max = footprint[0];

            for (int index = 1; index < footprint.Count; index++)
            {
                min = Vector2Int.Min(min, footprint[index]);
                max = Vector2Int.Max(max, footprint[index]);
            }

            int width = max.x - min.x + 1;
            int height = max.y - min.y + 1;
            if (width == 1 && height == 2 && itemBackground1x2 != null)
            {
                return itemBackground1x2;
            }

            if (width == 2 && height == 1 && itemBackground2x1 != null)
            {
                return itemBackground2x1;
            }

            return itemBackground1x1;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject result = new GameObject(name, typeof(RectTransform));
            result.transform.SetParent(parent, false);
            return result;
        }

        private static void CreateEventSystemIfNeeded()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }
    }
}
