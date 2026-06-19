using System;
using System.Collections.Generic;
using System.Linq;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class DeathOutcomePlanner
    {
        private readonly IRollProvider _rollProvider;

        public DeathOutcomePlanner()
            : this(new RandomRollProvider())
        {
        }

        public DeathOutcomePlanner(IRollProvider rollProvider)
        {
            _rollProvider = rollProvider ?? throw new ArgumentNullException(nameof(rollProvider));
        }

        public PlayerAssetOutcome Plan(PlayerAsset asset, IEnumerable<OutcomeRule> rules)
        {
            if (asset == null)
            {
                throw new ArgumentNullException(nameof(asset));
            }

            var plan = PlanDeathSession(new[] { asset }, rules);
            return plan.ForAsset(asset.Id);
        }

        public DeathOutcomePlan PlanDeathSession(IEnumerable<PlayerAsset> assets, IEnumerable<OutcomeRule> rules)
        {
            if (assets == null)
            {
                throw new ArgumentNullException(nameof(assets));
            }

            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            var ruleList = rules.ToList();
            EnsureCatchAllRuleExists(ruleList);

            var outcomes = assets.Select(asset => PlanAsset(asset, ruleList));
            return new DeathOutcomePlan(outcomes);
        }

        private static void EnsureCatchAllRuleExists(IEnumerable<OutcomeRule> rules)
        {
            if (!rules.Any(rule => rule.Target.IsCatchAll))
            {
                throw new InvalidOutcomeRuleConfigurationException(
                    "Outcome Rule configuration must include an explicit catch-all rule, such as Target kind Any.");
            }
        }

        private PlayerAssetOutcome PlanAsset(PlayerAsset asset, IEnumerable<OutcomeRule> rules)
        {
            var matchingPriorityGroups = rules
                .Where(rule => rule.Target.Matches(asset))
                .GroupBy(rule => rule.Priority)
                .OrderByDescending(group => group.Key);

            foreach (var priorityGroup in matchingPriorityGroups)
            {
                var matchingRules = priorityGroup.ToList();
                if (matchingRules.Count > 1)
                {
                    throw new InvalidOutcomeRuleConfigurationException(
                        "Multiple outcome rules matched asset '" + asset.Id + "' at priority " + priorityGroup.Key + ": " +
                        string.Join(", ", matchingRules.Select(rule => rule.Name)) + ".");
                }

                var selectedRule = matchingRules[0];
                var roll = SampleRollFor(selectedRule);
                if (RuleOutcomeOccurs(selectedRule, roll))
                {
                    return new PlayerAssetOutcome(asset, selectedRule.OutcomeKind, selectedRule, roll);
                }
            }

            throw new InvalidOperationException("No outcome rule matched the player asset.");
        }

        private double? SampleRollFor(OutcomeRule rule)
        {
            if (rule.Chance <= 0 || rule.Chance >= 1)
            {
                return null;
            }

            return _rollProvider.NextRoll();
        }

        private static bool RuleOutcomeOccurs(OutcomeRule rule, double? roll)
        {
            if (rule.Chance <= 0)
            {
                return false;
            }

            if (rule.Chance >= 1)
            {
                return true;
            }

            return roll < rule.Chance;
        }
    }
}
