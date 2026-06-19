using System;
using System.Collections.Generic;
using System.Linq;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class DurableClaimFallbackPlanner
    {
        public IReadOnlyList<DurableClaimFallbackDecision> PlanAfterCreateFailure(
            IEnumerable<PlayerAssetOutcome> keptOutcomes,
            DurableClaimCreateResult claimCreateResult,
            bool immediateRestoreAvailable)
        {
            if (keptOutcomes == null)
            {
                throw new ArgumentNullException(nameof(keptOutcomes));
            }

            if (claimCreateResult == null)
            {
                throw new ArgumentNullException(nameof(claimCreateResult));
            }

            if (claimCreateResult.Created)
            {
                return Array.Empty<DurableClaimFallbackDecision>();
            }

            var fallbackKind = immediateRestoreAvailable
                ? DurableClaimFallbackKind.ImmediateRestore
                : DurableClaimFallbackKind.DropFallback;
            var reason = "Durable Claim creation failed: " + (claimCreateResult.ErrorMessage ?? "unknown error");

            return keptOutcomes
                .Where(outcome => outcome.Kind == PlayerAssetOutcomeKind.Keep)
                .GroupBy(outcome => outcome.Asset.Id)
                .Select(group => new DurableClaimFallbackDecision(group.First().Asset, fallbackKind, reason))
                .ToList()
                .AsReadOnly();
        }
    }
}
