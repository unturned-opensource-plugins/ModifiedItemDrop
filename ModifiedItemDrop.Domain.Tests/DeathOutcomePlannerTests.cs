using FFEmqo.ModifiedItemDrop.Domain;
using Xunit;

namespace FFEmqo.ModifiedItemDrop.Domain.Tests;

public sealed class DeathOutcomePlannerTests
{
    [Fact]
    public void PrimaryWeaponWithAlwaysDropRuleProducesDropOutcome()
    {
        var asset = new PlayerAsset("asset-1", PlayerAssetSlot.PrimaryWeapon, itemId: 363);
        var rule = OutcomeRule.Drop(
            "primary-weapon-always-drops",
            priority: 100,
            OutcomeTarget.ForSlot(PlayerAssetSlot.PrimaryWeapon),
            chance: 1.0);

        var outcome = new DeathOutcomePlanner().Plan(asset, new[] { rule });

        Assert.Equal(PlayerAssetOutcomeKind.Drop, outcome.Kind);
        Assert.Same(asset, outcome.Asset);
        Assert.Same(rule, outcome.Rule);
    }

    [Fact]
    public void RollBelowChanceUsesProbabilisticRuleOutcome()
    {
        var asset = new PlayerAsset("asset-1", PlayerAssetSlot.PrimaryWeapon, itemId: 363);
        var rules = new[]
        {
            OutcomeRule.Drop(
                "primary weapon sometimes drops",
                priority: 100,
                OutcomeTarget.ForSlot(PlayerAssetSlot.PrimaryWeapon),
                chance: 0.50),
            OutcomeRule.Keep(
                "fallback keep",
                priority: 0,
                OutcomeTarget.Any(),
                chance: 1.0)
        };

        var outcome = new DeathOutcomePlanner(new FixedRollProvider(0.49)).Plan(asset, rules);

        Assert.Equal(PlayerAssetOutcomeKind.Drop, outcome.Kind);
        Assert.Equal("primary weapon sometimes drops", outcome.Rule.Name);
    }

}


internal sealed class FixedRollProvider : IRollProvider
{
    private readonly double _roll;

    public FixedRollProvider(double roll)
    {
        _roll = roll;
    }

    public double NextRoll()
    {
        return _roll;
    }
}
