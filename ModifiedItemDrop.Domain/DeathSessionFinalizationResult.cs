using System.Collections.Generic;
using System.Linq;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class DeathSessionFinalizationResult
    {
        public DeathSessionFinalizationResult(
            bool sessionEnded,
            bool durableClaimCreated,
            IEnumerable<DurableClaimFallbackDecision> fallbackDecisions)
        {
            SessionEnded = sessionEnded;
            DurableClaimCreated = durableClaimCreated;
            FallbackDecisions = fallbackDecisions.ToList().AsReadOnly();
        }

        public bool SessionEnded { get; }

        public bool DurableClaimCreated { get; }

        public IReadOnlyList<DurableClaimFallbackDecision> FallbackDecisions { get; }
    }
}
