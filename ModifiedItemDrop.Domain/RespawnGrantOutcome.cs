using System;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class RespawnGrantOutcome
    {
        public RespawnGrantOutcome(OutcomeRule rule, ushort itemId, byte amount, byte quality)
        {
            Rule = rule ?? throw new ArgumentNullException(nameof(rule));
            ItemId = itemId;
            Amount = amount;
            Quality = quality;
        }

        public OutcomeRule Rule { get; }

        public ushort ItemId { get; }

        public byte Amount { get; }

        public byte Quality { get; }
    }
}
