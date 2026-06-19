using System;
using System.Collections.Generic;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class DeathSessionRespawnGrantPlanner
    {
        private readonly HashSet<string> _consumedSessionIds = new HashSet<string>();
        private readonly RespawnGrantPlanner _grantPlanner = new RespawnGrantPlanner();

        public DeathSessionRespawnGrantResult PlanAfterDeathRespawn(DeathSession? session, IEnumerable<OutcomeRule> rules)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            if (session == null)
            {
                return new DeathSessionRespawnGrantResult(Array.Empty<RespawnGrantOutcome>(), sessionMarkedRespawnGrantConsumed: false);
            }

            if (_consumedSessionIds.Contains(session.Id))
            {
                return new DeathSessionRespawnGrantResult(Array.Empty<RespawnGrantOutcome>(), sessionMarkedRespawnGrantConsumed: false);
            }

            var grants = _grantPlanner.PlanAfterDeathRespawn(rules);
            _consumedSessionIds.Add(session.Id);
            return new DeathSessionRespawnGrantResult(grants, sessionMarkedRespawnGrantConsumed: true);
        }
    }
}
