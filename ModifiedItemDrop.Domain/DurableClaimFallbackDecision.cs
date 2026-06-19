using System;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class DurableClaimFallbackDecision
    {
        public DurableClaimFallbackDecision(PlayerAsset asset, DurableClaimFallbackKind kind, string reason)
        {
            Asset = asset ?? throw new ArgumentNullException(nameof(asset));
            Kind = kind;
            Reason = reason ?? string.Empty;
        }

        public PlayerAsset Asset { get; }

        public DurableClaimFallbackKind Kind { get; }

        public string Reason { get; }
    }
}
