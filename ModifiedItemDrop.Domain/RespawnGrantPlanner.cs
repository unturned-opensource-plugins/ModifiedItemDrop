using System;
using System.Collections.Generic;
using System.Linq;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class RespawnGrantPlanner
    {
        public IReadOnlyList<RespawnGrantOutcome> PlanAfterDeathRespawn(IEnumerable<OutcomeRule> rules)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            return rules
                .Where(rule => rule.TriggerKind == OutcomeRuleTriggerKind.AfterDeathRespawn)
                .OrderByDescending(rule => rule.Priority)
                .Select(rule => new RespawnGrantOutcome(
                    rule,
                    rule.GrantItemId.GetValueOrDefault(),
                    rule.GrantAmount.GetValueOrDefault(),
                    rule.GrantQuality.GetValueOrDefault()))
                .ToList()
                .AsReadOnly();
        }
    }
}
