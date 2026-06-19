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
        var fallback = OutcomeRule.Keep(
            "fallback keep",
            priority: 0,
            OutcomeTarget.Any(),
            chance: 1.0);

        var outcome = new DeathOutcomePlanner().Plan(asset, new[] { rule, fallback });

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


    [Fact]
    public void EqualPriorityMatchingRulesAreInvalidConfiguration()
    {
        var asset = new PlayerAsset("asset-1", PlayerAssetSlot.PrimaryWeapon, itemId: 363);
        var rules = new[]
        {
            OutcomeRule.Drop("drop primary", 100, OutcomeTarget.ForSlot(PlayerAssetSlot.PrimaryWeapon), chance: 1.0),
            OutcomeRule.Keep("keep primary", 100, OutcomeTarget.ForSlot(PlayerAssetSlot.PrimaryWeapon), chance: 1.0),
            OutcomeRule.Keep("fallback keep", 0, OutcomeTarget.Any(), chance: 1.0)
        };

        var exception = Assert.Throws<InvalidOutcomeRuleConfigurationException>(
            () => new DeathOutcomePlanner().Plan(asset, rules));

        Assert.Contains("priority 100", exception.Message);
        Assert.Contains("drop primary", exception.Message);
        Assert.Contains("keep primary", exception.Message);
    }


    [Fact]
    public void MissingCatchAllRuleIsInvalidConfiguration()
    {
        var asset = new PlayerAsset("backpack", PlayerAssetSlot.Backpack, itemId: 253);
        var rules = new[]
        {
            OutcomeRule.Drop(
                "primary weapon drops",
                priority: 100,
                OutcomeTarget.ForSlot(PlayerAssetSlot.PrimaryWeapon),
                chance: 1.0)
        };

        var exception = Assert.Throws<InvalidOutcomeRuleConfigurationException>(
            () => new DeathOutcomePlanner().Plan(asset, rules));

        Assert.Contains("catch-all", exception.Message);
    }


    [Fact]
    public void HandsSlotRuleCanResolveTopLevelAssetOutcome()
    {
        var asset = new PlayerAsset("hands", PlayerAssetSlot.Hands, itemId: 116);
        var rules = new[]
        {
            OutcomeRule.Keep("keep hands item", 100, OutcomeTarget.ForSlot(PlayerAssetSlot.Hands), chance: 1.0),
            OutcomeRule.Drop("fallback drop", 0, OutcomeTarget.Any(), chance: 1.0)
        };

        var outcome = new DeathOutcomePlanner().Plan(asset, rules);

        Assert.Equal(PlayerAssetOutcomeKind.Keep, outcome.Kind);
        Assert.Equal("keep hands item", outcome.Rule.Name);
    }


    [Fact]
    public void ProbabilisticOutcomeRecordsSampledRollForExplanation()
    {
        var asset = new PlayerAsset("asset-1", PlayerAssetSlot.PrimaryWeapon, itemId: 363);
        var rules = new[]
        {
            OutcomeRule.Drop("primary weapon sometimes drops", 100, OutcomeTarget.ForSlot(PlayerAssetSlot.PrimaryWeapon), chance: 0.50),
            OutcomeRule.Keep("fallback keep", 0, OutcomeTarget.Any(), chance: 1.0)
        };

        var outcome = new DeathOutcomePlanner(new FixedRollProvider(0.49)).Plan(asset, rules);

        Assert.Equal(0.49, outcome.SampledRoll);
        Assert.Equal(0.50, outcome.Rule.Chance);
    }


    [Fact]
    public void ChanceZeroNeverUsesProbabilisticRuleAndFallsBack()
    {
        var asset = new PlayerAsset("asset-1", PlayerAssetSlot.PrimaryWeapon, itemId: 363);
        var rules = new[]
        {
            OutcomeRule.Drop("primary weapon never drops", 100, OutcomeTarget.ForSlot(PlayerAssetSlot.PrimaryWeapon), chance: 0.0),
            OutcomeRule.Keep("fallback keep", 0, OutcomeTarget.Any(), chance: 1.0)
        };

        var outcome = new DeathOutcomePlanner(new FixedRollProvider(0.0)).Plan(asset, rules);

        Assert.Equal(PlayerAssetOutcomeKind.Keep, outcome.Kind);
        Assert.Equal("fallback keep", outcome.Rule.Name);
        Assert.Null(outcome.SampledRoll);
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
