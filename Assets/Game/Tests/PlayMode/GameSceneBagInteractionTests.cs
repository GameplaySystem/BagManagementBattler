using System.Collections;
using BagManagementBattler.Bag;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BagManagementBattler.Tests
{
    public sealed class GameSceneBagInteractionTests
    {
        [UnityTest]
        public IEnumerator GameScene_CreatesInitialBagAndContainerPresentation()
        {
            yield return SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Single);
            yield return null;

            GameObject interaction = GameObject.Find("Bag Interaction");
            Assert.That(interaction, Is.Not.Null);
            Assert.That(interaction.GetComponent<BagInteractionBootstrap>(), Is.Not.Null);
            Assert.That(interaction.GetComponent<BagDragController>(), Is.Not.Null);

            GameObject cells = GameObject.Find("Cells");
            GameObject items = GameObject.Find("Items");
            GameObject slots = GameObject.Find("Slots");

            Assert.That(cells, Is.Not.Null);
            Assert.That(cells.transform.childCount, Is.EqualTo(20));
            SpriteRenderer firstCell = cells.transform.GetChild(0).GetComponent<SpriteRenderer>();
            Assert.That(firstCell.bounds.size.x, Is.EqualTo(0.611f).Within(0.01f));
            Assert.That(firstCell.bounds.size.y, Is.EqualTo(0.611f).Within(0.01f));
            Assert.That(items, Is.Not.Null);
            Assert.That(items.transform.childCount, Is.EqualTo(2));
            Assert.That(slots, Is.Not.Null);
            Assert.That(slots.transform.childCount, Is.EqualTo(2));

            RectTransform rifleSlot = slots.transform.Find("weapon.rifle.basic") as RectTransform;
            RectTransform crowbarSlot = slots.transform.Find("weapon.crowbar.basic") as RectTransform;
            Assert.That(rifleSlot, Is.Not.Null);
            Assert.That(crowbarSlot, Is.Not.Null);
            Assert.That(rifleSlot.sizeDelta, Is.EqualTo(new Vector2(192f, 96f)));
            Assert.That(crowbarSlot.sizeDelta, Is.EqualTo(new Vector2(96f, 96f)));

            Transform soldierObject = null;
            for (int index = 0; index < items.transform.childCount; index++)
            {
                Transform candidate = items.transform.GetChild(index);
                if (candidate.name.StartsWith("soldier.blue"))
                {
                    soldierObject = candidate;
                    break;
                }
            }

            Assert.That(soldierObject, Is.Not.Null);
            SpriteRenderer[] soldierRenderers = soldierObject.GetComponentsInChildren<SpriteRenderer>();
            Assert.That(soldierRenderers.Length, Is.EqualTo(3));
            Assert.That(soldierRenderers, Has.All.Matches<SpriteRenderer>(renderer => renderer.sprite != null));
            Transform soldierBackground = soldierObject.Find("Background");
            Assert.That(soldierBackground, Is.Not.Null);
            SpriteRenderer soldierBackgroundRenderer = soldierBackground.GetComponent<SpriteRenderer>();
            Assert.That(soldierBackgroundRenderer.bounds.size.x, Is.EqualTo(0.611f).Within(0.01f));
            Assert.That(soldierBackgroundRenderer.bounds.size.y, Is.EqualTo(1.222f).Within(0.01f));

            Image[] slotImages = slots.GetComponentsInChildren<Image>();
            int populatedBackgrounds = 0;
            int populatedVisualParts = 0;
            foreach (Image image in slotImages)
            {
                if (image.gameObject.name.StartsWith("Visual ") && image.sprite != null)
                {
                    populatedVisualParts++;
                }
                else if (image.transform.parent == slots.transform && image.sprite != null)
                {
                    populatedBackgrounds++;
                }
            }

            Assert.That(populatedBackgrounds, Is.EqualTo(2));
            Assert.That(populatedVisualParts, Is.EqualTo(2));

            BagBoardView bagView = interaction.GetComponentInChildren<BagBoardView>();
            ItemContainerView containerView = interaction.GetComponentInChildren<ItemContainerView>();
            ContainerSlotView rifleSlotView = rifleSlot.GetComponent<ContainerSlotView>();
            Assert.That(rifleSlotView, Is.Not.Null);
            Transform bottomLeftCell = cells.transform.Find("Cell 0,0");
            Transform bottomSecondCell = cells.transform.Find("Cell 1,0");
            Assert.That(bottomLeftCell, Is.Not.Null);
            Assert.That(bottomSecondCell, Is.Not.Null);
            Vector3 forgivingDropWorld = Vector3.Lerp(
                bottomSecondCell.position,
                bottomLeftCell.position,
                0.25f);
            Vector2 forgivingDropScreen = Camera.main.WorldToScreenPoint(forgivingDropWorld);
            Assert.That(
                bagView.TryGetCell(forgivingDropScreen, Camera.main, out Vector2Int pointerCell),
                Is.True);
            Assert.That(pointerCell, Is.EqualTo(new Vector2Int(1, 0)));
            Assert.That(
                bagView.TryGetPlacementOrigin(
                    forgivingDropScreen,
                    Camera.main,
                    rifleSlotView.Item,
                    out Vector2Int rifleOrigin),
                Is.True);
            Assert.That(rifleOrigin, Is.EqualTo(Vector2Int.zero));

            BagItemInstance crowbar = bagView.GetItemAt(Vector2Int.zero);
            Assert.That(crowbar, Is.Not.Null);
            Assert.That(crowbar.Level, Is.EqualTo(1));
            Assert.That(crowbar.Definition.NextLevelDefinition, Is.Not.Null);
            Assert.That(crowbar.Definition.NextLevelDefinition.Level, Is.EqualTo(2));
            Assert.That(crowbar.Definition.NextLevelDefinition.IconSprite, Is.Not.Null);

            BagItemInstance soldier = bagView.GetItemAt(new Vector2Int(2, 1));
            containerView.ShowGhost(soldier, Vector2.zero);

            RectTransform dragGhost = containerView.transform.Find("Bag Interface Canvas/Drag Ghost") as RectTransform;
            Assert.That(dragGhost, Is.Not.Null);
            Assert.That(dragGhost.sizeDelta, Is.EqualTo(new Vector2(130f, 260f)));
            Assert.That(dragGhost.childCount, Is.EqualTo(3));
            Assert.That(
                dragGhost.GetComponentsInChildren<Image>(),
                Has.All.Matches<Image>(image => image.sprite != null));

            containerView.HideGhost();
        }
    }
}
