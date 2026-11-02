using System;
using System.Collections.Generic;
using BagManagementBattler.Bag;
using UnityEngine;

namespace BagManagementBattler.Combat
{
    public sealed class SoldierSpawner : MonoBehaviour
    {
        [SerializeField] private CombatSoldier soldierPrefab;
        [SerializeField] private Transform spawnAnchor;
        [SerializeField, Min(0)] private int prewarmCount = 4;

        private readonly Queue<CombatSoldier> availableUnits = new Queue<CombatSoldier>();
        private readonly HashSet<CombatSoldier> activeUnits = new HashSet<CombatSoldier>();
        private readonly HashSet<CombatSoldier> ownedUnits = new HashSet<CombatSoldier>();
        private bool initialized;

        public int ActiveCount => activeUnits.Count;
        public int AvailableCount => availableUnits.Count;
        public IReadOnlyCollection<CombatSoldier> ActiveUnits => activeUnits;
        public bool IsInitialized => initialized;

        private void Awake()
        {
            if (soldierPrefab != null && spawnAnchor != null)
            {
                Initialize(soldierPrefab, spawnAnchor, prewarmCount);
            }
        }

        private void Start()
        {
            if (initialized)
            {
                return;
            }

            if (soldierPrefab == null || spawnAnchor == null)
            {
                Debug.LogError("Soldier spawner is missing its prefab or spawn anchor.", this);
                enabled = false;
                return;
            }

            Initialize(soldierPrefab, spawnAnchor, prewarmCount);
        }

        public void Initialize(
            CombatSoldier configuredPrefab,
            Transform configuredSpawnAnchor,
            int configuredPrewarmCount = 4)
        {
            if (initialized)
            {
                throw new InvalidOperationException("The soldier spawner is already initialized.");
            }

            soldierPrefab = configuredPrefab != null
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

        public CombatSoldier Spawn(ProductionRequest request)
        {
            EnsureInitialized();

            CombatSoldier unit = availableUnits.Count > 0
                ? availableUnits.Dequeue()
                : CreateUnit();
            unit.Initialize(request, spawnAnchor.position);
            activeUnits.Add(unit);
            unit.gameObject.SetActive(true);
            return unit;
        }

        public bool Release(CombatSoldier unit)
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

        private CombatSoldier CreateUnit()
        {
            CombatSoldier unit = Instantiate(soldierPrefab, transform);
            unit.name = $"{soldierPrefab.name} Pooled {ownedUnits.Count}";
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
