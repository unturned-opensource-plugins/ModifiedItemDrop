using System;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class DeathProcessingResult
    {
        public DeathProcessingResult(DeathOutcomePlan plan, DeathSession deathSession)
        {
            Plan = plan ?? throw new ArgumentNullException(nameof(plan));
            DeathSession = deathSession;
        }

        public DeathOutcomePlan Plan { get; }

        public DeathSession DeathSession { get; }

        public bool HasPendingDeathSession
        {
            get { return DeathSession != null && DeathSession.KeptOutcomes.Count > 0; }
        }
    }
}
