using System;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class OutcomeRule
    {
        private OutcomeRule(string name, int priority, OutcomeTarget target, PlayerAssetOutcomeKind outcomeKind, double chance)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Rule name must be provided.", nameof(name));
            }

            Name = name;
            Priority = priority;
            Target = target ?? throw new ArgumentNullException(nameof(target));
            OutcomeKind = outcomeKind;
            Chance = chance;
        }

        public string Name { get; }

        public int Priority { get; }

        public OutcomeTarget Target { get; }

        public PlayerAssetOutcomeKind OutcomeKind { get; }

        public double Chance { get; }

        public static OutcomeRule Drop(string name, int priority, OutcomeTarget target, double chance)
        {
            return new OutcomeRule(name, priority, target, PlayerAssetOutcomeKind.Drop, chance);
        }

        public static OutcomeRule Keep(string name, int priority, OutcomeTarget target, double chance)
        {
            return new OutcomeRule(name, priority, target, PlayerAssetOutcomeKind.Keep, chance);
        }

        public static OutcomeRule Delete(string name, int priority, OutcomeTarget target)
        {
            return new OutcomeRule(name, priority, target, PlayerAssetOutcomeKind.Delete, chance: 1.0);
        }
    }
}
