using BagManagementBattler.Combat;
using UnityEngine;

namespace BagManagementBattler.Bag
{
    public sealed class BagInteractionBootstrap : MonoBehaviour
    {
        [Header("Runtime Definitions")]
        [SerializeField] private BagGridDefinition gridDefinition;
        [SerializeField] private BagItemDefinition blueSoldier;
        [SerializeField] private BagItemDefinition rifle;
        [SerializeField] private BagItemDefinition crowbar;

        [Header("Bag Presentation")]
        [SerializeField] private Sprite gridCellSprite;
        [SerializeField] private Sprite itemBackground1x1;
        [SerializeField] private Sprite itemBackground1x2;
        [SerializeField] private Sprite itemBackground2x1;
        [SerializeField] private Vector2 bagWorldOrigin = new Vector2(-1.625f, -3.55f);
        [SerializeField] private float cellSize = 0.65f;

        [Header("Production")]
        [SerializeField] private BagProductionController productionController;

        private void Awake()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            BagGrid grid = new BagGrid(gridDefinition);
            ItemContainer container = new ItemContainer();
            BagManager manager = new BagManager(grid, container);

            BagItemInstance soldierInstance = new BagItemInstance(blueSoldier);
            if (!grid.TryPlace(soldierInstance, new Vector2Int(2, 1), out PlacementFailureReason failure))
            {
                throw new UnityException($"Could not place the initial soldier: {failure}.");
            }

            BagItemInstance crowbarInstance = new BagItemInstance(crowbar);
            if (!grid.TryPlace(crowbarInstance, Vector2Int.zero, out failure))
            {
                throw new UnityException($"Could not place the initial crowbar: {failure}.");
            }

            BagItemInstance productionRifle = new BagItemInstance(rifle);
            if (!grid.TryPlace(productionRifle, new Vector2Int(0, 1), out failure))
            {
                throw new UnityException($"Could not place the initial production rifle: {failure}.");
            }

            BagItemInstance productionCrowbar = new BagItemInstance(crowbar);
            if (!grid.TryPlace(productionCrowbar, new Vector2Int(3, 1), out failure))
            {
                throw new UnityException($"Could not place the initial production crowbar: {failure}.");
            }

            container.AddBottom(new BagItemInstance(rifle));
            container.AddBottom(new BagItemInstance(crowbar));

            GameObject bagObject = new GameObject("Bag Board");
            bagObject.transform.SetParent(transform, false);
            BagBoardView bagView = bagObject.AddComponent<BagBoardView>();
            bagView.Initialize(
                manager,
                bagWorldOrigin,
                cellSize,
                gridCellSprite,
                itemBackground1x1,
                itemBackground1x2,
                itemBackground2x1);

            productionController.Initialize(grid);
            BagProductionProgressView progressView =
                bagObject.AddComponent<BagProductionProgressView>();
            progressView.Initialize(productionController, bagView);

            GameObject containerObject = new GameObject("Item Container View");
            containerObject.transform.SetParent(transform, false);
            ItemContainerView containerView = containerObject.AddComponent<ItemContainerView>();
            containerView.Initialize(
                container,
                itemBackground1x1,
                itemBackground1x2,
                itemBackground2x1);

            BagDragController dragController = gameObject.AddComponent<BagDragController>();
            dragController.Initialize(manager, bagView, containerView, Camera.main);
        }

        private bool ValidateReferences()
        {
            bool valid = gridDefinition != null &&
                         blueSoldier != null &&
                         rifle != null &&
                         crowbar != null &&
                         gridCellSprite != null &&
                         itemBackground1x1 != null &&
                         itemBackground1x2 != null &&
                         itemBackground2x1 != null &&
                         productionController != null &&
                         Camera.main != null;

            if (!valid)
            {
                Debug.LogError("Bag interaction bootstrap is missing one or more required references.", this);
            }

            return valid;
        }
    }
}
