using System;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class PlayerAssetOutcome
    {
        public PlayerAssetOutcome(PlayerAsset asset, PlayerAssetOutcomeKind kind, OutcomeRule rule)
            : this(asset, kind, rule, sampledRoll: null)
        {
        }

        public PlayerAssetOutcome(PlayerAsset asset, PlayerAssetOutcomeKind kind, OutcomeRule rule, double? sampledRoll)
        {
            Asset = asset ?? throw new ArgumentNullException(nameof(asset));
            Kind = kind;
            Rule = rule ?? throw new ArgumentNullException(nameof(rule));
            SampledRoll = sampledRoll;
        }

        public PlayerAsset Asset { get; }

        public PlayerAssetOutcomeKind Kind { get; }

        public OutcomeRule Rule { get; }

        public double? SampledRoll { get; }

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
