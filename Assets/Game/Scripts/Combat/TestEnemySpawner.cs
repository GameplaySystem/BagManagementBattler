using System;
using BagManagementBattler.Bag;
using UnityEngine;

namespace BagManagementBattler.Combat
{
    public sealed class TestEnemySpawner : MonoBehaviour
    {
        [SerializeField] private CombatUnit unitPrefab;
        [SerializeField] private Transform spawnAnchor;
        [SerializeField] private UnitDefinition enemyDefinition;
        [SerializeField] private BagItemDefinition weaponDefinition;
        [SerializeField] private bool spawnOnStart = true;

        public CombatUnit SpawnedUnit { get; private set; }

        private void Start()
        {
            if (spawnOnStart)
            {
                Spawn();
            }
        }

        public CombatUnit Spawn()
        {
            if (SpawnedUnit != null)
            {
                throw new InvalidOperationException("The test enemy has already been spawned.");
            }

            ValidateReferences();
            CombatUnit unit = Instantiate(unitPrefab, transform);
            unit.name = "Test Enemy Unit";
            unit.Initialize(
                new CombatUnitLoadout(enemyDefinition, weaponDefinition),
                spawnAnchor.position);
            SpawnedUnit = unit;
            return unit;
        }

        private void ValidateReferences()
        {
            if (unitPrefab == null || spawnAnchor == null ||
                enemyDefinition == null || weaponDefinition == null)
            {
                throw new InvalidOperationException(
                    "The test enemy spawner is missing a required reference.");
            }

            if (enemyDefinition.Team != CombatTeam.Enemy)
            {
                throw new InvalidOperationException(
                    "The test enemy spawner requires an enemy unit definition.");
            }

            if (weaponDefinition.Role != BagItemRole.Weapon)
            {
                throw new InvalidOperationException(
                    "The test enemy spawner requires a weapon definition.");
            }
        }
    }
}
