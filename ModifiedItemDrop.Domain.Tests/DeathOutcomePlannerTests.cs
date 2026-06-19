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
}
