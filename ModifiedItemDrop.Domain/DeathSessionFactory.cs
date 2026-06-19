using System;
using System.Linq;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public static class DeathSessionFactory
    {
        public static DeathSession CreateFromDeathPlan(string sessionId, ulong steamId, DeathOutcomePlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            return new DeathSession(
                sessionId,
                steamId,
                plan.Outcomes.Where(outcome => outcome.Kind == PlayerAssetOutcomeKind.Keep));
        }
    }
}
