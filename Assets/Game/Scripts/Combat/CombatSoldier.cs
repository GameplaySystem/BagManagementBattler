using System;
using System.Collections.Generic;
using BagManagementBattler.Bag;
using UnityEngine;

namespace BagManagementBattler.Combat
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class CombatSoldier : MonoBehaviour
    {
        [SerializeField] private float visualCellSize = 0.65f;
        [SerializeField] private Transform handPoint;

        private readonly List<SpriteRenderer> soldierVisualRenderers =
            new List<SpriteRenderer>();
        private SpriteRenderer weaponRenderer;
        private Rigidbody2D body;

        public BagItemDefinition SoldierDefinition { get; private set; }
        public BagItemDefinition WeaponDefinition { get; private set; }
        public Rigidbody2D Body => body;
        public Transform HandPoint => handPoint;
        public bool IsInitialized { get; private set; }

        private void Awake()
        {
            EnsureBody();
            EnsureHandPoint();
        }

        private void FixedUpdate()
        {
            if (!IsInitialized)
            {
                return;
            }

            body.linearVelocity = new Vector2(SoldierDefinition.MovementSpeed, body.linearVelocity.y);
        }

        public void Initialize(ProductionRequest request, Vector3 spawnPosition)
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException("The combat soldier is already initialized.");
            }

            EnsureBody();
            EnsureHandPoint();
            SoldierDefinition = request.SoldierDefinition;
            WeaponDefinition = request.WeaponDefinition;
            SoldierDefinition.Validate();
            WeaponDefinition.Validate();

            transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
            body.linearVelocity = Vector2.zero;
            ConfigureSoldierVisuals();
            ConfigureWeaponVisual();
            IsInitialized = true;
        }

        internal void ResetForPool(Vector3 pooledPosition)
        {
            EnsureBody();
            EnsureHandPoint();
            IsInitialized = false;
            body.linearVelocity = Vector2.zero;
            transform.SetPositionAndRotation(pooledPosition, Quaternion.identity);
            SoldierDefinition = null;
            WeaponDefinition = null;
            ClearVisuals();
        }

        private void EnsureBody()
        {
            if (body != null)
            {
                return;
            }

            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.linearVelocity = Vector2.zero;
        }

        private void EnsureHandPoint()
        {
            if (handPoint != null)
            {
                return;
            }

            handPoint = transform.Find("HandPoint");
            if (handPoint == null)
            {
                handPoint = new GameObject("HandPoint").transform;
                handPoint.SetParent(transform, false);
            }
        }

        private void ConfigureSoldierVisuals()
        {
            int rendererIndex = 0;
            foreach (BagItemVisualPart part in SoldierDefinition.VisualParts)
            {
                if (part.Sprite == null)
                {
                    continue;
                }

                SpriteRenderer renderer = GetOrCreateSoldierRenderer(rendererIndex++);
                renderer.sprite = part.Sprite;
                renderer.transform.localPosition = new Vector3(
                    part.LocalPositionInCells.x * visualCellSize,
                    part.LocalPositionInCells.y * visualCellSize,
                    0f);
                renderer.transform.localRotation = Quaternion.identity;
                renderer.transform.localScale = Vector3.one * part.ScaleInCells * visualCellSize;
                renderer.sortingOrder = 30 + rendererIndex;
                renderer.enabled = true;
            }

            if (rendererIndex == 0 && SoldierDefinition.IconSprite != null)
            {
                SpriteRenderer renderer = GetOrCreateSoldierRenderer(rendererIndex++);
                renderer.sprite = SoldierDefinition.IconSprite;
                renderer.transform.localPosition = Vector3.zero;
                renderer.transform.localRotation = Quaternion.identity;
                renderer.transform.localScale = Vector3.one * visualCellSize;
                renderer.sortingOrder = 31;
                renderer.enabled = true;
            }

            for (int index = rendererIndex; index < soldierVisualRenderers.Count; index++)
            {
                soldierVisualRenderers[index].sprite = null;
                soldierVisualRenderers[index].enabled = false;
            }
        }

        private void ConfigureWeaponVisual()
        {
            Sprite weaponSprite = WeaponDefinition.IconSprite;
            if (weaponSprite == null)
            {
                foreach (BagItemVisualPart part in WeaponDefinition.VisualParts)
                {
                    if (part.Sprite != null)
                    {
                        weaponSprite = part.Sprite;
                        break;
                    }
                }
            }

            if (weaponSprite == null)
            {
                ClearWeaponVisual();
                return;
            }

            SpriteRenderer renderer = GetOrCreateWeaponRenderer();
            renderer.sprite = weaponSprite;
            renderer.transform.localPosition = WeaponDefinition.EquippedLocalPosition;
            renderer.transform.localRotation = Quaternion.Euler(
                0f,
                0f,
                WeaponDefinition.EquippedLocalRotationDegrees);
            Vector2 scale = WeaponDefinition.EquippedLocalScale;
            renderer.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            renderer.sortingOrder = 40;
            renderer.enabled = true;
        }

        private void ClearVisuals()
        {
            foreach (SpriteRenderer renderer in soldierVisualRenderers)
            {
                renderer.sprite = null;
                renderer.enabled = false;
            }

            ClearWeaponVisual();
        }

        private void ClearWeaponVisual()
        {
            if (weaponRenderer == null)
            {
                return;
            }

            weaponRenderer.sprite = null;
            weaponRenderer.enabled = false;
        }

        private SpriteRenderer GetOrCreateSoldierRenderer(int index)
        {
            if (index < soldierVisualRenderers.Count)
            {
                return soldierVisualRenderers[index];
            }

            GameObject visualObject = new GameObject($"Soldier Visual {index}");
            visualObject.transform.SetParent(transform, false);
            SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
            soldierVisualRenderers.Add(renderer);
            return renderer;
        }

        private SpriteRenderer GetOrCreateWeaponRenderer()
        {
            if (weaponRenderer != null)
            {
                return weaponRenderer;
            }

            GameObject visualObject = new GameObject("Weapon Visual");
            visualObject.transform.SetParent(handPoint, false);
            weaponRenderer = visualObject.AddComponent<SpriteRenderer>();
            return weaponRenderer;
        }
    }
}
