using System;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class OutcomeRuleEvaluation
    {
        public OutcomeRuleEvaluation(OutcomeRule rule, double? sampledRoll, bool outcomeOccurred)
        {
            Rule = rule ?? throw new ArgumentNullException(nameof(rule));
            SampledRoll = sampledRoll;
            OutcomeOccurred = outcomeOccurred;
        }

        public OutcomeRule Rule { get; }

        public double? SampledRoll { get; }

        public bool OutcomeOccurred { get; }
    }
}
