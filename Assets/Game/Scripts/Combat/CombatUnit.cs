using System;
using System.Collections.Generic;
using BagManagementBattler.Bag;
using UnityEngine;

namespace BagManagementBattler.Combat
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class CombatUnit : MonoBehaviour
    {
        [SerializeField] private float visualCellSize = 0.65f;
        [SerializeField] private Transform handPoint;
        [SerializeField, Min(0.1f)] private float acquisitionRange = 20f;
        [SerializeField, Min(0.02f)] private float targetRefreshInterval = 0.1f;

        private readonly List<SpriteRenderer> unitVisualRenderers =
            new List<SpriteRenderer>();
        private readonly Collider2D[] targetCandidates = new Collider2D[32];
        private SpriteRenderer weaponRenderer;
        private Rigidbody2D body;
        private CombatUnit currentTarget;
        private float nextTargetRefreshTime;

        public UnitDefinition UnitDefinition { get; private set; }
        public BagItemDefinition WeaponDefinition { get; private set; }
        public CombatTeam Team => UnitDefinition != null ? UnitDefinition.Team : default;
        public float MovementSpeed { get; private set; }
        public float EngagementRange { get; private set; }
        public Rigidbody2D Body => body;
        public Transform HandPoint => handPoint;
        public CombatUnit CurrentTarget => currentTarget;
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

            RefreshTargetIfNeeded();
            body.linearVelocity = new Vector2(ResolveHorizontalVelocity(), 0f);
        }

        private void OnDisable()
        {
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }

            currentTarget = null;
        }

        public void Initialize(CombatUnitLoadout loadout, Vector3 spawnPosition)
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException("The combat unit is already initialized.");
            }

            EnsureBody();
            EnsureHandPoint();
            UnitDefinition = loadout.UnitDefinition;
            WeaponDefinition = loadout.WeaponDefinition;
            MovementSpeed = loadout.MovementSpeed;
            EngagementRange = loadout.EngagementRange;

            transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
            body.linearVelocity = Vector2.zero;
            currentTarget = null;
            nextTargetRefreshTime = 0f;
            ConfigureUnitVisuals();
            ConfigureWeaponVisual();
            IsInitialized = true;
        }

        public void RefreshTargetNow()
        {
            if (!IsInitialized)
            {
                currentTarget = null;
                return;
            }

            currentTarget = FindNearestTarget();
            nextTargetRefreshTime = Time.time + targetRefreshInterval;
        }

        internal void ResetForPool(Vector3 pooledPosition)
        {
            EnsureBody();
            EnsureHandPoint();
            IsInitialized = false;
            body.linearVelocity = Vector2.zero;
            transform.SetPositionAndRotation(pooledPosition, Quaternion.identity);
            currentTarget = null;
            UnitDefinition = null;
            WeaponDefinition = null;
            MovementSpeed = 0f;
            EngagementRange = 0f;
            ClearVisuals();
        }

        private void RefreshTargetIfNeeded()
        {
            if (IsValidTarget(currentTarget))
            {
                return;
            }

            currentTarget = null;
            if (Time.time >= nextTargetRefreshTime)
            {
                RefreshTargetNow();
            }
        }

        private CombatUnit FindNearestTarget()
        {
            int candidateCount = Physics2D.OverlapCircle(
                transform.position,
                acquisitionRange,
                ContactFilter2D.noFilter,
                targetCandidates);
            CombatUnit nearest = null;
            float nearestDistanceSquared = float.PositiveInfinity;

            for (int index = 0; index < candidateCount; index++)
            {
                Collider2D candidateCollider = targetCandidates[index];
                CombatUnit candidate = candidateCollider != null
                    ? candidateCollider.GetComponent<CombatUnit>()
                    : null;
                if (!IsValidTarget(candidate))
                {
                    continue;
                }

                float distanceSquared =
                    ((Vector2)candidate.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (distanceSquared < nearestDistanceSquared)
                {
                    nearest = candidate;
                    nearestDistanceSquared = distanceSquared;
                }
            }

            return nearest;
        }

        private bool IsValidTarget(CombatUnit candidate)
        {
            return candidate != null &&
                   candidate != this &&
                   candidate.IsInitialized &&
                   candidate.isActiveAndEnabled &&
                   candidate.gameObject.activeInHierarchy &&
                   candidate.Team != Team;
        }

        private float ResolveHorizontalVelocity()
        {
            if (IsValidTarget(currentTarget))
            {
                float horizontalDelta = currentTarget.transform.position.x - transform.position.x;
                if (Mathf.Abs(horizontalDelta) <= EngagementRange)
                {
                    return 0f;
                }

                return Mathf.Sign(horizontalDelta) * MovementSpeed;
            }

            return Team == CombatTeam.Friendly ? MovementSpeed : -MovementSpeed;
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

        private void ConfigureUnitVisuals()
        {
            int rendererIndex = 0;
            rendererIndex = ConfigureUnitVisual(
                rendererIndex,
                UnitDefinition.BodySprite,
                UnitVisualLayout.BodyPositionInCells,
                31);
            rendererIndex = ConfigureUnitVisual(
                rendererIndex,
                UnitDefinition.HeadSprite,
                UnitVisualLayout.HeadPositionInCells,
                32);

            for (int index = rendererIndex; index < unitVisualRenderers.Count; index++)
            {
                unitVisualRenderers[index].sprite = null;
                unitVisualRenderers[index].enabled = false;
            }
        }

        private int ConfigureUnitVisual(
            int rendererIndex,
            Sprite sprite,
            Vector2 positionInCells,
            int sortingOrder)
        {
            if (sprite == null)
            {
                return rendererIndex;
            }

            SpriteRenderer renderer = GetOrCreateUnitRenderer(rendererIndex++);
            renderer.sprite = sprite;
            renderer.transform.localPosition = new Vector3(
                positionInCells.x * visualCellSize,
                positionInCells.y * visualCellSize,
                0f);
            renderer.transform.localRotation = Quaternion.identity;
            renderer.transform.localScale =
                Vector3.one * UnitVisualLayout.ScaleInCells * visualCellSize;
            renderer.sortingOrder = sortingOrder;
            renderer.enabled = true;
            return rendererIndex;
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
            foreach (SpriteRenderer renderer in unitVisualRenderers)
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

        private SpriteRenderer GetOrCreateUnitRenderer(int index)
        {
            if (index < unitVisualRenderers.Count)
            {
                return unitVisualRenderers[index];
            }

            GameObject visualObject = new GameObject($"Unit Visual {index}");
            visualObject.transform.SetParent(transform, false);
            SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
            unitVisualRenderers.Add(renderer);
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
