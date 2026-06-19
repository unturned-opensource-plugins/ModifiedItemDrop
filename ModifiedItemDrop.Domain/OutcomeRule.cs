using System;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class OutcomeRule
    {
        private OutcomeRule(
            string name,
            int priority,
            OutcomeTarget? target,
            PlayerAssetOutcomeKind outcomeKind,
            double chance,
            OutcomeRuleTriggerKind? triggerKind,
            ushort? grantItemId,
            byte? grantAmount,
            byte? grantQuality)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Rule name must be provided.", nameof(name));
            }

            Name = name;
            Priority = priority;
            Target = target;
            OutcomeKind = outcomeKind;
            Chance = chance;
            TriggerKind = triggerKind;
            GrantItemId = grantItemId;
            GrantAmount = grantAmount;
            GrantQuality = grantQuality;
        }

        public string Name { get; }

        public int Priority { get; }

        public OutcomeTarget? Target { get; }

        public OutcomeRuleTriggerKind? TriggerKind { get; }

        public PlayerAssetOutcomeKind OutcomeKind { get; }

        public double Chance { get; }

        public ushort? GrantItemId { get; }

        public byte? GrantAmount { get; }

        public byte? GrantQuality { get; }

        public static OutcomeRule Drop(string name, int priority, OutcomeTarget target, double chance)
        {
            return new OutcomeRule(name, priority, target ?? throw new ArgumentNullException(nameof(target)), PlayerAssetOutcomeKind.Drop, chance, null, null, null, null);
        }

        public static OutcomeRule Keep(string name, int priority, OutcomeTarget target, double chance)
        {
            return new OutcomeRule(name, priority, target ?? throw new ArgumentNullException(nameof(target)), PlayerAssetOutcomeKind.Keep, chance, null, null, null, null);
        }

        public static OutcomeRule Delete(string name, int priority, OutcomeTarget target)
        {
            return new OutcomeRule(name, priority, target ?? throw new ArgumentNullException(nameof(target)), PlayerAssetOutcomeKind.Delete, chance: 1.0, null, null, null, null);
        }

        public static OutcomeRule Grant(
            string name,
            int priority,
            OutcomeRuleTriggerKind triggerKind,
            ushort itemId,
            byte amount,
            byte quality)
        {
            return new OutcomeRule(name, priority, null, PlayerAssetOutcomeKind.Grant, chance: 1.0, triggerKind, itemId, amount, quality);
        }
    }
}
