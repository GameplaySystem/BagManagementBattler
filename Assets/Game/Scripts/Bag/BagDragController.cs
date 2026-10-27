using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BagManagementBattler.Bag
{
    public sealed class BagDragController : MonoBehaviour
    {
        private BagManager manager;
        private BagBoardView bagView;
        private ItemContainerView containerView;
        private Camera worldCamera;
        private InputAction pointerPosition;
        private InputAction pointerPress;
        private InputAction cancel;
        private bool initialized;

        public void Initialize(
            BagManager manager,
            BagBoardView bagView,
            ItemContainerView containerView,
            Camera worldCamera)
        {
            this.manager = manager ?? throw new ArgumentNullException(nameof(manager));
            this.bagView = bagView ?? throw new ArgumentNullException(nameof(bagView));
            this.containerView = containerView ?? throw new ArgumentNullException(nameof(containerView));
            this.worldCamera = worldCamera ?? throw new ArgumentNullException(nameof(worldCamera));
            initialized = true;
        }

        private void Awake()
        {
            pointerPosition = new InputAction("Bag Pointer Position", InputActionType.PassThrough, "<Pointer>/position");
            pointerPress = new InputAction("Bag Pointer Press", InputActionType.Button, "<Pointer>/press");
            cancel = new InputAction("Cancel Bag Drag", InputActionType.Button, "<Keyboard>/escape");
        }

        private void OnEnable()
        {
            pointerPress.started += OnPressStarted;
            pointerPress.canceled += OnPressCanceled;
            cancel.performed += OnCancelPerformed;
            pointerPosition.Enable();
            pointerPress.Enable();
            cancel.Enable();
        }

        private void OnDisable()
        {
            pointerPress.started -= OnPressStarted;
            pointerPress.canceled -= OnPressCanceled;
            cancel.performed -= OnCancelPerformed;
            pointerPosition.Disable();
            pointerPress.Disable();
            cancel.Disable();

            if (initialized && manager.IsDragging)
            {
                manager.CancelDrag();
            }
        }

        private void OnDestroy()
        {
            pointerPosition?.Dispose();
            pointerPress?.Dispose();
            cancel?.Dispose();
        }

        private void Update()
        {
            if (!initialized || !manager.IsDragging)
            {
                return;
            }

            Vector2 screenPosition = pointerPosition.ReadValue<Vector2>();
            bool validDestination = false;

            if (bagView.TryGetPlacementOrigin(
                    screenPosition,
                    worldCamera,
                    manager.DraggedItem,
                    out Vector2Int origin))
            {
                PlacementFailureReason failure = manager.ValidateDropToBag(origin);
                validDestination = failure == PlacementFailureReason.None;
                bagView.ShowPlacementPreview(manager.DraggedItem, origin, failure);
            }
            else
            {
                bagView.ClearPlacementPreview();
                validDestination = containerView.Contains(screenPosition);
            }

            containerView.MoveGhost(screenPosition, validDestination);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && initialized && manager.IsDragging)
            {
                CancelAndRefresh();
            }
        }

        private void OnPressStarted(InputAction.CallbackContext context)
        {
            if (!initialized || manager.IsDragging)
            {
                return;
            }

            Vector2 screenPosition = pointerPosition.ReadValue<Vector2>();
            BagItemInstance item = null;

            if (!containerView.TryGetItem(screenPosition, out item) &&
                bagView.TryGetCell(screenPosition, worldCamera, out Vector2Int cell))
            {
                item = bagView.GetItemAt(cell);
            }

            if (item == null || !manager.BeginDrag(item))
            {
                return;
            }

            bagView.RefreshItems();
            containerView.Refresh();
            containerView.ShowGhost(item, screenPosition);
        }

        private void OnPressCanceled(InputAction.CallbackContext context)
        {
            if (!initialized || !manager.IsDragging)
            {
                return;
            }

            Vector2 screenPosition = pointerPosition.ReadValue<Vector2>();

            if (containerView.Contains(screenPosition))
            {
                manager.DropToContainer();
            }
            else if (bagView.TryGetPlacementOrigin(
                         screenPosition,
                         worldCamera,
                         manager.DraggedItem,
                         out Vector2Int origin))
            {
                manager.TryDropToBag(origin, out _);
            }
            else
            {
                manager.CancelDrag();
            }

            RefreshAfterDrag();
        }

        private void OnCancelPerformed(InputAction.CallbackContext context)
        {
            if (initialized && manager.IsDragging)
            {
                CancelAndRefresh();
            }
        }

        private void CancelAndRefresh()
        {
            manager.CancelDrag();
            RefreshAfterDrag();
        }

        private void RefreshAfterDrag()
        {
            bagView.ClearPlacementPreview();
            bagView.RefreshItems();
            containerView.Refresh();
            containerView.HideGhost();
        }
    }
}
