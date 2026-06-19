using System;
using System.Collections.Generic;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class DeathOutcomeExecutionPlanner
    {
        public DeathOutcomeExecutionPlan Plan(DeathOutcomePlan deathOutcomePlan)
        {
            if (deathOutcomePlan == null)
            {
                throw new ArgumentNullException(nameof(deathOutcomePlan));
            }

            var actions = new List<DeathOutcomeExecutionAction>();
            foreach (var outcome in deathOutcomePlan.Outcomes)
            {
                actions.Add(new DeathOutcomeExecutionAction(outcome, ToActionKind(outcome.Kind)));
            }

            return new DeathOutcomeExecutionPlan(actions);
        }

        private static DeathOutcomeExecutionActionKind ToActionKind(PlayerAssetOutcomeKind outcomeKind)
        {
            switch (outcomeKind)
            {
                case PlayerAssetOutcomeKind.Drop:
                    return DeathOutcomeExecutionActionKind.Drop;
                case PlayerAssetOutcomeKind.Keep:
                    return DeathOutcomeExecutionActionKind.KeepForRestore;
                case PlayerAssetOutcomeKind.Delete:
                    return DeathOutcomeExecutionActionKind.Delete;
                default:
                    throw new InvalidOutcomeRuleConfigurationException(
                        "Death execution does not support outcome kind '" + outcomeKind + "'.");
            }
        }
    }
}
