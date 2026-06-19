using System;
using System.Collections.Generic;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class DeathProcessingOrchestrator
    {
        private readonly DeathOutcomePlanner _planner;
        private readonly DeathOutcomeExecutionPlanner _executionPlanner;

        public DeathProcessingOrchestrator()
            : this(new DeathOutcomePlanner(), new DeathOutcomeExecutionPlanner())
        {
        }

        public DeathProcessingOrchestrator(DeathOutcomePlanner planner)
            : this(planner, new DeathOutcomeExecutionPlanner())
        {
        }

        public DeathProcessingOrchestrator(DeathOutcomePlanner planner, DeathOutcomeExecutionPlanner executionPlanner)
        {
            _planner = planner ?? throw new ArgumentNullException(nameof(planner));
            _executionPlanner = executionPlanner ?? throw new ArgumentNullException(nameof(executionPlanner));
        }

        public DeathProcessingResult ProcessDeath(
            string sessionId,
            ulong steamId,
            IEnumerable<PlayerAsset> assets,
            IEnumerable<OutcomeRule> rules)
        {
            var plan = _planner.PlanDeathSession(assets, rules);
            var executionPlan = _executionPlanner.Plan(plan);
            var session = DeathSessionFactory.CreateFromDeathPlan(sessionId, steamId, plan);
            return new DeathProcessingResult(plan, executionPlan, session);
        }
    }
}
