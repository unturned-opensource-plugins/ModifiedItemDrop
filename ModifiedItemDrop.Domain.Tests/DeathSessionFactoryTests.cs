using FFEmqo.ModifiedItemDrop.Domain;
using Xunit;

namespace FFEmqo.ModifiedItemDrop.Domain.Tests;

public sealed class DeathSessionFactoryTests
{
    [Fact]
    public void DeathProcessingCreatesSessionForKeptPlayerAssetsOnly()
    {
        var kept = new PlayerAsset("kept-primary", PlayerAssetSlot.PrimaryWeapon, itemId: 363);
        var dropped = new PlayerAsset("dropped-secondary", PlayerAssetSlot.SecondaryWeapon, itemId: 101);
        var deleted = new PlayerAsset("deleted-banned", PlayerAssetSlot.Hands, itemId: 95);
        var rules = new[]
        {
            OutcomeRule.Keep("keep primary", 100, OutcomeTarget.ForSlot(PlayerAssetSlot.PrimaryWeapon), chance: 1.0),
            OutcomeRule.Drop("drop secondary", 100, OutcomeTarget.ForSlot(PlayerAssetSlot.SecondaryWeapon), chance: 1.0),
            OutcomeRule.Delete("delete banned", 1000, OutcomeTarget.ForItem(95)),
            OutcomeRule.Keep("fallback keep", 0, OutcomeTarget.Any(), chance: 1.0)
        };
        var plan = new DeathOutcomePlanner().PlanDeathSession(new[] { kept, dropped, deleted }, rules);

        var session = DeathSessionFactory.CreateFromDeathPlan("session-1", steamId: 76561198000000001UL, plan);

        var tracked = Assert.Single(session.Outcomes);
        Assert.Equal("kept-primary", tracked.Asset.Id);
        Assert.Equal(PlayerAssetOutcomeKind.Keep, tracked.Kind);
    }
}
