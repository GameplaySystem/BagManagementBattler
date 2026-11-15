using System;
using System.Collections.Generic;
using BagManagementBattler.Bag;
using UnityEngine;

namespace BagManagementBattler.Combat
{
    public sealed class SoldierSpawner : MonoBehaviour
    {
        [SerializeField] private CombatUnit unitPrefab;
        [SerializeField] private Transform spawnAnchor;
        [SerializeField, Min(0)] private int prewarmCount = 4;

        private readonly Queue<CombatUnit> availableUnits = new Queue<CombatUnit>();
        private readonly HashSet<CombatUnit> activeUnits = new HashSet<CombatUnit>();
        private readonly HashSet<CombatUnit> ownedUnits = new HashSet<CombatUnit>();
        private bool initialized;

        public int ActiveCount => activeUnits.Count;
        public int AvailableCount => availableUnits.Count;
        public IReadOnlyCollection<CombatUnit> ActiveUnits => activeUnits;
        public bool IsInitialized => initialized;

        private void Awake()
        {
            if (unitPrefab != null && spawnAnchor != null)
            {
                Initialize(unitPrefab, spawnAnchor, prewarmCount);
            }
        }

        private void Start()
        {
            if (initialized)
            {
                return;
            }

            if (unitPrefab == null || spawnAnchor == null)
            {
                Debug.LogError("Soldier spawner is missing its prefab or spawn anchor.", this);
                enabled = false;
                return;
            }

            Initialize(unitPrefab, spawnAnchor, prewarmCount);
        }

        public void Initialize(
            CombatUnit configuredPrefab,
            Transform configuredSpawnAnchor,
            int configuredPrewarmCount = 4)
        {
            if (initialized)
            {
                throw new InvalidOperationException("The soldier spawner is already initialized.");
            }

            unitPrefab = configuredPrefab != null
                ? configuredPrefab
                : throw new ArgumentNullException(nameof(configuredPrefab));
            spawnAnchor = configuredSpawnAnchor != null
                ? configuredSpawnAnchor
                : throw new ArgumentNullException(nameof(configuredSpawnAnchor));

            if (configuredPrewarmCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(configuredPrewarmCount));
            }

            prewarmCount = configuredPrewarmCount;
            initialized = true;
            for (int index = 0; index < prewarmCount; index++)
            {
                availableUnits.Enqueue(CreateUnit());
            }
        }

        public CombatUnit Spawn(ProductionRequest request)
        {
            EnsureInitialized();

            CombatUnit unit = availableUnits.Count > 0
                ? availableUnits.Dequeue()
                : CreateUnit();
            unit.Initialize(request.ToLoadout(), spawnAnchor.position);
            activeUnits.Add(unit);
            unit.gameObject.SetActive(true);
            return unit;
        }

        public bool Release(CombatUnit unit)
        {
            EnsureInitialized();

            if (unit == null || !ownedUnits.Contains(unit) || !activeUnits.Remove(unit))
            {
                return false;
            }

            unit.ResetForPool(spawnAnchor.position);
            unit.gameObject.SetActive(false);
            availableUnits.Enqueue(unit);
            return true;
        }

        private CombatUnit CreateUnit()
        {
            CombatUnit unit = Instantiate(unitPrefab, transform);
            unit.name = $"{unitPrefab.name} Pooled {ownedUnits.Count}";
            unit.gameObject.SetActive(false);
            unit.ResetForPool(spawnAnchor.position);
            ownedUnits.Add(unit);
            return unit;
        }

        private void EnsureInitialized()
        {
            if (!initialized)
            {
                throw new InvalidOperationException("Initialize the soldier spawner before use.");
            }
        }
    }
}
