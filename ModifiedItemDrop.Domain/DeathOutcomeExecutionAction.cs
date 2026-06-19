using System;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class DeathOutcomeExecutionAction
    {
        public DeathOutcomeExecutionAction(PlayerAssetOutcome outcome, DeathOutcomeExecutionActionKind kind)
        {
            Outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));
            Kind = kind;
        }

        public PlayerAssetOutcome Outcome { get; }

        public PlayerAsset Asset
        {
            get { return Outcome.Asset; }
        }

        public DeathOutcomeExecutionActionKind Kind { get; }
    }
}
