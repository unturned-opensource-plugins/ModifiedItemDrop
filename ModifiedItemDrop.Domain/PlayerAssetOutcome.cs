using System;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class PlayerAssetOutcome
    {
        public PlayerAssetOutcome(PlayerAsset asset, PlayerAssetOutcomeKind kind, OutcomeRule rule)
        {
            Asset = asset ?? throw new ArgumentNullException(nameof(asset));
            Kind = kind;
            Rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        public PlayerAsset Asset { get; }

        public PlayerAssetOutcomeKind Kind { get; }

        public OutcomeRule Rule { get; }

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
