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
            Assert.That(items.transform.childCount, Is.EqualTo(1));
            Assert.That(slots, Is.Not.Null);
            Assert.That(slots.transform.childCount, Is.EqualTo(2));

            RectTransform rifleSlot = slots.transform.Find("weapon.rifle.basic") as RectTransform;
            RectTransform crowbarSlot = slots.transform.Find("weapon.crowbar.basic") as RectTransform;
            Assert.That(rifleSlot, Is.Not.Null);
            Assert.That(crowbarSlot, Is.Not.Null);
            Assert.That(rifleSlot.sizeDelta, Is.EqualTo(new Vector2(192f, 96f)));
            Assert.That(crowbarSlot.sizeDelta, Is.EqualTo(new Vector2(96f, 96f)));

            SpriteRenderer[] soldierRenderers = items.GetComponentsInChildren<SpriteRenderer>();
            Assert.That(soldierRenderers.Length, Is.EqualTo(3));
            Assert.That(soldierRenderers, Has.All.Matches<SpriteRenderer>(renderer => renderer.sprite != null));
            Transform soldierBackground = items.transform.GetChild(0).Find("Background");
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
