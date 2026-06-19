using FFEmqo.ModifiedItemDrop.Domain;
using Xunit;

namespace FFEmqo.ModifiedItemDrop.Domain.Tests;

public sealed class DeathProcessingOrchestratorTests
{
    [Fact]
    public void PrimaryWeaponDropOutcomeProducesNoPendingDeathSessionResponsibility()
    {
        var asset = new PlayerAsset("inventory:0:0", PlayerAssetSlot.PrimaryWeapon, itemId: 363, amount: 1, quality: 100, state: new byte[] { 1 });
        var rules = new[]
        {
            OutcomeRule.Drop("primary drops", 100, OutcomeTarget.ForSlot(PlayerAssetSlot.PrimaryWeapon), chance: 1.0),
            OutcomeRule.Keep("default keep", 0, OutcomeTarget.Any(), chance: 1.0)
        };

        var result = new DeathProcessingOrchestrator().ProcessDeath(
            sessionId: "session-1",
            steamId: 76561198000000001UL,
            assets: new[] { asset },
            rules: rules);

        var outcome = Assert.Single(result.Plan.Outcomes);
        Assert.Equal(PlayerAssetOutcomeKind.Drop, outcome.Kind);
        Assert.False(result.HasPendingDeathSession);
    }


    [Fact]
    public void PrimaryWeaponKeepOutcomeStartsPendingDeathSessionResponsibility()
    {
        var asset = new PlayerAsset("inventory:0:0", PlayerAssetSlot.PrimaryWeapon, itemId: 363, amount: 1, quality: 100, state: new byte[] { 1 });
        var rules = new[]
        {
            OutcomeRule.Keep("primary kept", 100, OutcomeTarget.ForSlot(PlayerAssetSlot.PrimaryWeapon), chance: 1.0),
            OutcomeRule.Drop("default drop", 0, OutcomeTarget.Any(), chance: 1.0)
        };

        var result = new DeathProcessingOrchestrator().ProcessDeath(
            sessionId: "session-2",
            steamId: 76561198000000001UL,
            assets: new[] { asset },
            rules: rules);

        Assert.True(result.HasPendingDeathSession);
        Assert.Equal("session-2", result.DeathSession.Id);
        Assert.Equal(76561198000000001UL, result.DeathSession.SteamId);
        var kept = Assert.Single(result.DeathSession.KeptOutcomes);
        Assert.Equal("inventory:0:0", kept.Asset.Id);
        Assert.Equal(PlayerAssetOutcomeKind.Keep, kept.Kind);
    }
}
