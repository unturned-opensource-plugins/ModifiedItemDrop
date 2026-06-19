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
            var outcomes = assets.Select(asset => PlanAsset(asset, ruleList));
            return new DeathOutcomePlan(outcomes);
        }

        private PlayerAssetOutcome PlanAsset(PlayerAsset asset, IEnumerable<OutcomeRule> rules)
        {
            var selectedRule = rules
                .Where(rule => rule.Target.Matches(asset))
                .OrderByDescending(rule => rule.Priority)
                .FirstOrDefault(RuleOutcomeOccurs);

            if (selectedRule == null)
            {
                throw new InvalidOperationException("No outcome rule matched the player asset.");
            }

            return new PlayerAssetOutcome(asset, selectedRule.OutcomeKind, selectedRule);
        }

        private bool RuleOutcomeOccurs(OutcomeRule rule)
        {
            if (rule.Chance <= 0)
            {
                return false;
            }

            if (rule.Chance >= 1)
            {
                return true;
            }

            return _rollProvider.NextRoll() < rule.Chance;
        }
    }
}
