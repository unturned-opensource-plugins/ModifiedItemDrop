using System;
using System.Collections.Generic;
using System.Linq;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class DeathOutcomePlanner
    {
        public PlayerAssetOutcome Plan(PlayerAsset asset, IEnumerable<OutcomeRule> rules)
        {
            if (asset == null)
            {
                throw new ArgumentNullException(nameof(asset));
            }

            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            var selectedRule = rules
                .Where(rule => rule.Target.Matches(asset))
                .OrderByDescending(rule => rule.Priority)
                .FirstOrDefault();

            if (selectedRule == null)
            {
                throw new InvalidOperationException("No outcome rule matched the player asset.");
            }

            return new PlayerAssetOutcome(asset, selectedRule.OutcomeKind, selectedRule);
        }
    }
}
