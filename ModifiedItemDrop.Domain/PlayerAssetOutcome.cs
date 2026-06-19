using System;
using System.Collections.Generic;
using System.Linq;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class PlayerAssetOutcome
    {
        public PlayerAssetOutcome(PlayerAsset asset, PlayerAssetOutcomeKind kind, OutcomeRule rule)
            : this(asset, kind, rule, sampledRoll: null, ruleEvaluations: null)
        {
        }

        public PlayerAssetOutcome(PlayerAsset asset, PlayerAssetOutcomeKind kind, OutcomeRule rule, double? sampledRoll)
            : this(asset, kind, rule, sampledRoll, ruleEvaluations: null)
        {
        }

        public PlayerAssetOutcome(
            PlayerAsset asset,
            PlayerAssetOutcomeKind kind,
            OutcomeRule rule,
            double? sampledRoll,
            IEnumerable<OutcomeRuleEvaluation>? ruleEvaluations)
        {
            Asset = asset ?? throw new ArgumentNullException(nameof(asset));
            Kind = kind;
            Rule = rule ?? throw new ArgumentNullException(nameof(rule));
            SampledRoll = sampledRoll;
            RuleEvaluations = (ruleEvaluations ?? new[] { new OutcomeRuleEvaluation(rule, sampledRoll, outcomeOccurred: true) })
                .ToList()
                .AsReadOnly();
        }

        public PlayerAsset Asset { get; }

        public PlayerAssetOutcomeKind Kind { get; }

        public OutcomeRule Rule { get; }

        public double? SampledRoll { get; }

        public IReadOnlyList<OutcomeRuleEvaluation> RuleEvaluations { get; }

        public bool RequiresRestoration
        {
            get { return Kind == PlayerAssetOutcomeKind.Keep; }
        }

        public bool IsDurableClaimEligible
        {
            get { return Kind == PlayerAssetOutcomeKind.Keep; }
        }
    }
}
