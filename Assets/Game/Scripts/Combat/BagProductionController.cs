using System;
using System.Collections.Generic;
using BagManagementBattler.Bag;
using UnityEngine;

namespace BagManagementBattler.Combat
{
    public sealed class BagProductionController : MonoBehaviour
    {
        [SerializeField] private SoldierSpawner soldierSpawner;
        [SerializeField, Tooltip("Temporary Task 2C fixture. GameFlow will begin production later.")]
        private bool autoBeginForDebug = true;

        private BagGrid grid;
        private BagProductionSession session;

        public bool IsProducing => session != null;
        public int ActiveWeaponCount => session?.ActiveWeaponCount ?? 0;

        private void Start()
        {
            if (!autoBeginForDebug)
            {
                return;
            }

            if (grid == null || soldierSpawner == null)
            {
                Debug.LogError("Production auto-begin is missing its bag grid or soldier spawner.", this);
                enabled = false;
                return;
            }

            BeginProduction();
        }

        private void Update()
        {
            if (session != null)
            {
                Tick(Time.deltaTime);
            }
        }

        public void Initialize(BagGrid configuredGrid, SoldierSpawner configuredSpawner = null)
        {
            if (grid != null)
            {
                throw new InvalidOperationException("The production controller is already initialized.");
            }

            grid = configuredGrid ?? throw new ArgumentNullException(nameof(configuredGrid));
            if (configuredSpawner != null)
            {
                soldierSpawner = configuredSpawner;
            }

            if (soldierSpawner == null)
            {
                throw new InvalidOperationException("The production controller needs a soldier spawner.");
            }
        }

        public void BeginProduction()
        {
            if (grid == null)
            {
                throw new InvalidOperationException("Initialize the production controller before beginning.");
            }

            if (!soldierSpawner.IsInitialized)
            {
                throw new InvalidOperationException("Initialize the soldier spawner before beginning production.");
            }

            session = new BagProductionSession(grid);
        }

        public void EndProduction()
        {
            session = null;
        }

        public void Tick(float deltaTime)
        {
            if (session == null)
            {
                return;
            }

            IReadOnlyList<ProductionRequest> requests = session.Tick(deltaTime);
            foreach (ProductionRequest request in requests)
            {
                soldierSpawner.Spawn(request);
            }
        }

        public void CopyProgressTo(List<WeaponProductionProgress> destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (session == null)
            {
                destination.Clear();
                return;
            }

            session.CopyProgressTo(destination);
        }
    }
}
