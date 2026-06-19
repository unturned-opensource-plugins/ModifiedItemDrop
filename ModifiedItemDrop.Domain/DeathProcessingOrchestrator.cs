using System;
using System.Collections.Generic;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class DeathProcessingOrchestrator
    {
        private readonly DeathOutcomePlanner _planner;

        public DeathProcessingOrchestrator()
            : this(new DeathOutcomePlanner())
        {
        }

        public DeathProcessingOrchestrator(DeathOutcomePlanner planner)
        {
            _planner = planner ?? throw new ArgumentNullException(nameof(planner));
        }

        public DeathProcessingResult ProcessDeath(
            string sessionId,
            ulong steamId,
            IEnumerable<PlayerAsset> assets,
            IEnumerable<OutcomeRule> rules)
        {
            var plan = _planner.PlanDeathSession(assets, rules);
            var session = DeathSessionFactory.CreateFromDeathPlan(sessionId, steamId, plan);
            return new DeathProcessingResult(plan, session);
        }
    }
}
