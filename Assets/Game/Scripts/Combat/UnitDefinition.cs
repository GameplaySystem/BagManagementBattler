using System;
using UnityEngine;

namespace BagManagementBattler.Combat
{
    public enum CombatTeam
    {
        Friendly,
        Enemy
    }

    public enum UnitArchetype
    {
        Basic
    }

    [CreateAssetMenu(fileName = "UnitDefinition", menuName = "Bag Game/Unit Definition")]
    public sealed class UnitDefinition : ScriptableObject
    {
        [SerializeField] private CombatTeam team;
        [SerializeField] private UnitArchetype archetype;
        [SerializeField, Min(1)] private int level = 1;
        [SerializeField] private Sprite headSprite;
        [SerializeField] private Sprite bodySprite;
        [SerializeField, Min(0.01f)] private float baseMovementSpeed = 1f;

        public CombatTeam Team => team;
        public UnitArchetype Archetype => archetype;
        public int Level => level;
        public Sprite HeadSprite => headSprite;
        public Sprite BodySprite => bodySprite;
        public float BaseMovementSpeed => baseMovementSpeed;

        public void Validate()
        {
            if (!Enum.IsDefined(typeof(CombatTeam), team))
            {
                throw new InvalidOperationException($"Unknown combat team {team}.");
            }

            if (!Enum.IsDefined(typeof(UnitArchetype), archetype))
            {
                throw new InvalidOperationException($"Unknown unit archetype {archetype}.");
            }

            if (level < 1)
            {
                throw new InvalidOperationException("A unit level must be at least one.");
            }

            if (!IsFinite(baseMovementSpeed) || baseMovementSpeed <= 0f)
            {
                throw new InvalidOperationException(
                    "A unit base movement speed must be finite and greater than zero.");
            }
        }

        public static UnitDefinition CreateTransient(
            CombatTeam team,
            UnitArchetype archetype,
            int level,
            float baseMovementSpeed,
            Sprite bodySprite = null,
            Sprite headSprite = null)
        {
            UnitDefinition definition = CreateInstance<UnitDefinition>();
            definition.hideFlags = HideFlags.HideAndDontSave;
            definition.team = team;
            definition.archetype = archetype;
            definition.level = level;
            definition.baseMovementSpeed = baseMovementSpeed;
            definition.bodySprite = bodySprite;
            definition.headSprite = headSprite;
            definition.Validate();
            return definition;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
