using FFEmqo.ModifiedItemDrop.Domain;
using Xunit;

namespace FFEmqo.ModifiedItemDrop.Domain.Tests;

public sealed class DeathSessionRespawnGrantPlannerTests
{
    [Fact]
    public void TrackedDeathSessionRespawnProducesConfiguredGrantOnce()
    {
        var session = new DeathSession("session-1", steamId: 76561198000000001UL, outcomes: Array.Empty<PlayerAssetOutcome>());
        var rules = new[]
        {
            OutcomeRule.Grant(
                "Respawn grant medkit",
                priority: 100,
                OutcomeRuleTriggerKind.AfterDeathRespawn,
                itemId: 15,
                amount: 1,
                quality: 100)
        };
        var planner = new DeathSessionRespawnGrantPlanner();

        var first = planner.PlanAfterDeathRespawn(session, rules);
        var second = planner.PlanAfterDeathRespawn(session, rules);

        var grant = Assert.Single(first.Grants);
        Assert.Equal(15, grant.ItemId);
        Assert.True(first.SessionMarkedRespawnGrantConsumed);
        Assert.Empty(second.Grants);
    }

    [Fact]
    public void RespawnWithoutTrackedDeathSessionProducesNoGrant()
    {
        var rules = new[]
        {
            OutcomeRule.Grant("Respawn grant medkit", 100, OutcomeRuleTriggerKind.AfterDeathRespawn, itemId: 15, amount: 1, quality: 100)
        };

        var result = new DeathSessionRespawnGrantPlanner().PlanAfterDeathRespawn(null, rules);

        Assert.Empty(result.Grants);
        Assert.False(result.SessionMarkedRespawnGrantConsumed);
    }
}
