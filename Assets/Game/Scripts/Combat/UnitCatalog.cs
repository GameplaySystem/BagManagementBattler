using System;
using System.Collections.Generic;
using UnityEngine;

namespace BagManagementBattler.Combat
{
    [CreateAssetMenu(fileName = "UnitCatalog", menuName = "Bag Game/Unit Catalog")]
    public sealed class UnitCatalog : ScriptableObject
    {
        [SerializeField] private List<UnitDefinition> entries = new List<UnitDefinition>();

        public IReadOnlyList<UnitDefinition> Entries => entries;

        public void Validate()
        {
            HashSet<UnitKey> keys = new HashSet<UnitKey>();
            foreach (UnitDefinition entry in entries)
            {
                if (entry == null)
                {
                    throw new InvalidOperationException("A unit catalog cannot contain a null entry.");
                }

                entry.Validate();
                UnitKey key = new UnitKey(entry.Team, entry.Archetype, entry.Level);
                if (!keys.Add(key))
                {
                    throw new InvalidOperationException(
                        $"Duplicate unit catalog entry: {entry.Team} {entry.Archetype} level {entry.Level}.");
                }
            }
        }

        public static UnitCatalog CreateTransient(IEnumerable<UnitDefinition> configuredEntries)
        {
            if (configuredEntries == null)
            {
                throw new ArgumentNullException(nameof(configuredEntries));
            }

            UnitCatalog catalog = CreateInstance<UnitCatalog>();
            catalog.hideFlags = HideFlags.HideAndDontSave;
            catalog.entries = new List<UnitDefinition>(configuredEntries);
            catalog.Validate();
            return catalog;
        }

        private readonly struct UnitKey : IEquatable<UnitKey>
        {
            private readonly CombatTeam team;
            private readonly UnitArchetype archetype;
            private readonly int level;

            public UnitKey(CombatTeam team, UnitArchetype archetype, int level)
            {
                this.team = team;
                this.archetype = archetype;
                this.level = level;
            }

            public bool Equals(UnitKey other)
            {
                return team == other.team && archetype == other.archetype && level == other.level;
            }

            public override bool Equals(object obj)
            {
                return obj is UnitKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = (int)team;
                    hash = (hash * 397) ^ (int)archetype;
                    hash = (hash * 397) ^ level;
                    return hash;
                }
            }
        }
    }
}
