using System;
using System.Collections.Generic;
using UnityEngine;

namespace BagManagementBattler.Bag
{
    public sealed class BagBoardView : MonoBehaviour
    {
        private static readonly Color CellColor = new Color(0.75f, 0.86f, 1f, 0.9f);
        private static readonly Color ValidColor = new Color(0.45f, 1f, 0.55f, 0.95f);
        private static readonly Color InvalidColor = new Color(1f, 0.42f, 0.42f, 0.95f);
        private static readonly Color ProductionFillColor =
            new Color(0.35f, 1f, 0.45f, 1f);

        private BagManager manager;
        private Vector2 worldOrigin;
        private float cellSize;
        private Sprite itemBackground1x1;
        private Sprite itemBackground1x2;
        private Sprite itemBackground2x1;
        private SpriteRenderer[,] cellRenderers;
        private Transform itemRoot;
        private readonly Dictionary<Guid, ProductionFill> productionFills =
            new Dictionary<Guid, ProductionFill>();

        public void Initialize(
            BagManager manager,
            Vector2 worldOrigin,
            float cellSize,
            Sprite cellSprite,
            Sprite itemBackground1x1,
            Sprite itemBackground1x2,
            Sprite itemBackground2x1)
        {
            this.manager = manager ?? throw new ArgumentNullException(nameof(manager));
            this.worldOrigin = worldOrigin;
            this.cellSize = cellSize;
            this.itemBackground1x1 = itemBackground1x1;
            this.itemBackground1x2 = itemBackground1x2;
            this.itemBackground2x1 = itemBackground2x1;

            transform.position = Vector3.zero;
            cellRenderers = new SpriteRenderer[manager.Grid.Width, manager.Grid.Height];

            Transform cellRoot = new GameObject("Cells").transform;
            cellRoot.SetParent(transform, false);

            for (int x = 0; x < manager.Grid.Width; x++)
            {
                for (int y = 0; y < manager.Grid.Height; y++)
                {
                    GameObject cellObject = new GameObject($"Cell {x},{y}");
                    cellObject.transform.SetParent(cellRoot, false);
                    cellObject.transform.position = CellCenter(new Vector2Int(x, y));

                    SpriteRenderer renderer = cellObject.AddComponent<SpriteRenderer>();
                    renderer.sprite = cellSprite;
                    renderer.color = CellColor;
                    renderer.sortingOrder = 10;
                    FitSpriteToSize(
                        cellObject.transform,
                        cellSprite,
                        Vector2.one * cellSize * 0.94f);
                    cellRenderers[x, y] = renderer;
                }
            }

            itemRoot = new GameObject("Items").transform;
            itemRoot.SetParent(transform, false);
            RefreshItems();
        }

        public bool TryGetCell(Vector2 screenPosition, Camera camera, out Vector2Int cell)
        {
            Vector2 relative = ScreenToBoardPosition(screenPosition, camera);
            cell = new Vector2Int(
                Mathf.FloorToInt(relative.x / cellSize),
                Mathf.FloorToInt(relative.y / cellSize));
            return manager.Grid.IsInBounds(cell);
        }

        public bool TryGetPlacementOrigin(
            Vector2 screenPosition,
            Camera camera,
            BagItemInstance item,
            out Vector2Int origin)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            bool pointerIsOverGrid = TryGetCell(screenPosition, camera, out _);
            Vector2 relativeInCells = ScreenToBoardPosition(screenPosition, camera) / cellSize;
            GetFootprintBounds(item.Definition.Footprint, out Vector2Int min, out Vector2Int max);
            Vector2 footprintCenterFromOrigin = new Vector2(
                (min.x + max.x + 1) * 0.5f,
                (min.y + max.y + 1) * 0.5f);
            Vector2 floatingOrigin = relativeInCells - footprintCenterFromOrigin;
            origin = new Vector2Int(
                Mathf.FloorToInt(floatingOrigin.x + 0.5f),
                Mathf.FloorToInt(floatingOrigin.y + 0.5f));
            return pointerIsOverGrid;
        }

        public BagItemInstance GetItemAt(Vector2Int cell)
        {
            return manager.Grid.GetOccupant(cell);
        }

        public bool SetProductionProgress(Guid instanceId, float normalizedProgress)
        {
            if (!productionFills.TryGetValue(instanceId, out ProductionFill fill))
            {
                return false;
            }

            fill.SetProgress(normalizedProgress);
            return true;
        }

        public void ClearProductionProgress(Guid instanceId)
        {
            if (productionFills.TryGetValue(instanceId, out ProductionFill fill))
            {
                fill.SetProgress(0f);
            }
        }

        public void RefreshItems()
        {
            productionFills.Clear();
            for (int index = itemRoot.childCount - 1; index >= 0; index--)
            {
                GameObject child = itemRoot.GetChild(index).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            HashSet<Guid> renderedItems = new HashSet<Guid>();
            for (int x = 0; x < manager.Grid.Width; x++)
            {
                for (int y = 0; y < manager.Grid.Height; y++)
                {
                    BagItemInstance item = manager.Grid.GetOccupant(new Vector2Int(x, y));
                    if (item != null && renderedItems.Add(item.InstanceId))
                    {
                        CreateItemVisual(item);
                    }
                }
            }
        }

        public void ShowPlacementPreview(
            BagItemInstance item,
            Vector2Int origin,
            PlacementFailureReason failureReason)
        {
            ClearPlacementPreview();
            Color previewColor = failureReason == PlacementFailureReason.None ? ValidColor : InvalidColor;

            foreach (Vector2Int offset in item.Definition.Footprint)
            {
                Vector2Int cell = origin + offset;
                if (manager.Grid.IsInBounds(cell))
                {
                    cellRenderers[cell.x, cell.y].color = previewColor;
                }
            }
        }

        public void ClearPlacementPreview()
        {
            for (int x = 0; x < manager.Grid.Width; x++)
            {
                for (int y = 0; y < manager.Grid.Height; y++)
                {
                    cellRenderers[x, y].color = CellColor;
                }
            }
        }

        private Vector3 CellCenter(Vector2Int cell)
        {
            return new Vector3(
                worldOrigin.x + (cell.x + 0.5f) * cellSize,
                worldOrigin.y + (cell.y + 0.5f) * cellSize,
                0f);
        }

        private Vector2 ScreenToBoardPosition(Vector2 screenPosition, Camera camera)
        {
            Vector3 world = camera.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, -camera.transform.position.z));
            return (Vector2)world - worldOrigin;
        }

        private void CreateItemVisual(BagItemInstance item)
        {
            GetFootprintBounds(item.Definition.Footprint, out Vector2Int min, out Vector2Int max);
            int width = max.x - min.x + 1;
            int height = max.y - min.y + 1;
            Vector2Int origin = item.Placement.Value;

            GameObject itemObject = new GameObject($"{item.Definition.ItemTypeId} {item.InstanceId}");
            itemObject.transform.SetParent(itemRoot, false);
            itemObject.transform.position = new Vector3(
                worldOrigin.x + (origin.x + min.x + width * 0.5f) * cellSize,
                worldOrigin.y + (origin.y + min.y + height * 0.5f) * cellSize,
                0f);
            GameObject backgroundObject = new GameObject("Background");
            backgroundObject.transform.SetParent(itemObject.transform, false);
            SpriteRenderer background = backgroundObject.AddComponent<SpriteRenderer>();
            Sprite backgroundSprite = SelectBackground(width, height);
            background.sprite = backgroundSprite;
            background.color = item.Definition.ItemColor;
            background.sortingOrder = 20;
            FitSpriteToSize(
                backgroundObject.transform,
                backgroundSprite,
                new Vector2(width * cellSize * 0.94f, height * cellSize * 0.94f));

            if (item.Definition.Role == BagItemRole.Weapon)
            {
                Vector2 fillSize = new Vector2(
                    width * cellSize * 0.94f,
                    height * cellSize * 0.94f);
                GameObject fillObject = new GameObject("Production Fill");
                fillObject.transform.SetParent(itemObject.transform, false);
                SpriteRenderer fillRenderer = fillObject.AddComponent<SpriteRenderer>();
                fillRenderer.sprite = backgroundSprite;
                fillRenderer.color = ProductionFillColor;
                fillRenderer.drawMode = SpriteDrawMode.Sliced;
                fillRenderer.sortingOrder = 21;
                fillRenderer.enabled = false;
                productionFills.Add(
                    item.InstanceId,
                    new ProductionFill(fillRenderer, fillSize));
            }

            int partIndex = 0;
            foreach (BagItemVisualPart part in item.Definition.VisualParts)
            {
                if (part.Sprite == null)
                {
                    continue;
                }

                GameObject partObject = new GameObject($"Visual {partIndex++}");
                partObject.transform.SetParent(itemObject.transform, false);
                partObject.transform.localPosition = new Vector3(
                    part.LocalPositionInCells.x * cellSize,
                    part.LocalPositionInCells.y * cellSize,
                    -0.01f);
                partObject.transform.localScale = Vector3.one * part.ScaleInCells * cellSize;

                SpriteRenderer partRenderer = partObject.AddComponent<SpriteRenderer>();
                partRenderer.sprite = part.Sprite;
                partRenderer.sortingOrder = 22;
            }
        }

        private sealed class ProductionFill
        {
            private readonly SpriteRenderer renderer;
            private readonly Vector2 fullSize;

            public ProductionFill(SpriteRenderer renderer, Vector2 fullSize)
            {
                this.renderer = renderer;
                this.fullSize = fullSize;
            }

            public void SetProgress(float normalizedProgress)
            {
                float progress = Mathf.Clamp01(normalizedProgress);
                float width = fullSize.x * progress;
                renderer.size = new Vector2(Mathf.Max(width, 0.001f), fullSize.y);
                renderer.transform.localPosition = new Vector3(
                    (width - fullSize.x) * 0.5f,
                    0f,
                    -0.01f);
                renderer.enabled = progress > 0f;
            }
        }

        private Sprite SelectBackground(int width, int height)
        {
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

        private static void FitSpriteToSize(Transform target, Sprite sprite, Vector2 targetSize)
        {
            Vector2 sourceSize = sprite.bounds.size;
            target.localScale = new Vector3(
                targetSize.x / sourceSize.x,
                targetSize.y / sourceSize.y,
                1f);
        }

        private static void GetFootprintBounds(
            IReadOnlyList<Vector2Int> footprint,
            out Vector2Int min,
            out Vector2Int max)
        {
            min = footprint[0];
            max = footprint[0];

            for (int index = 1; index < footprint.Count; index++)
            {
                min = Vector2Int.Min(min, footprint[index]);
                max = Vector2Int.Max(max, footprint[index]);
            }
        }
    }
}
