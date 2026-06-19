using System;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class DeathProcessingResult
    {
        public DeathProcessingResult(DeathOutcomePlan plan, DeathOutcomeExecutionPlan executionPlan, DeathSession deathSession)
        {
            Plan = plan ?? throw new ArgumentNullException(nameof(plan));
            ExecutionPlan = executionPlan ?? throw new ArgumentNullException(nameof(executionPlan));
            DeathSession = deathSession;
        }

        public DeathOutcomePlan Plan { get; }

        public DeathOutcomeExecutionPlan ExecutionPlan { get; }

        public DeathSession DeathSession { get; }

        public bool HasPendingDeathSession
        {
            get { return DeathSession != null && DeathSession.KeptOutcomes.Count > 0; }
        }
    }
}
