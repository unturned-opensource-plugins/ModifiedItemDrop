using FFEmqo.ModifiedItemDrop.Domain;
using Xunit;

namespace FFEmqo.ModifiedItemDrop.Domain.Tests;

public sealed class CanonicalOutcomeModelTests
{
    [Fact]
    public void MixedTopLevelAndClothingContentAssetsReceiveExactlyOneOutcomeEach()
    {
        var primaryWeapon = new PlayerAsset("primary", PlayerAssetSlot.PrimaryWeapon, itemId: 363);
        var backpack = new PlayerAsset("backpack", PlayerAssetSlot.Backpack, itemId: 253);
        var backpackContent = PlayerAsset.ClothingContent(
            "backpack-content-1",
            sourceClothingSlot: PlayerAssetSlot.Backpack,
            parentAssetId: backpack.Id,
            itemId: 15);
        var bannedItem = new PlayerAsset("banned", PlayerAssetSlot.SecondaryWeapon, itemId: 95);

        var rules = new[]
        {
            OutcomeRule.Drop("primary weapons drop", 100, OutcomeTarget.ForSlot(PlayerAssetSlot.PrimaryWeapon), chance: 1.0),
            OutcomeRule.Keep("backpack content kept", 100, OutcomeTarget.ForClothingContent(PlayerAssetSlot.Backpack), chance: 1.0),
            OutcomeRule.Delete("delete banned item", 1000, OutcomeTarget.ForItem(95)),
            OutcomeRule.Keep("fallback keep", 0, OutcomeTarget.Any(), chance: 1.0)
        };

        var plan = new DeathOutcomePlanner().PlanDeathSession(
            new[] { primaryWeapon, backpack, backpackContent, bannedItem },
            rules);

        Assert.Equal(4, plan.Outcomes.Count);
        Assert.Equal(
            new[] { "backpack", "backpack-content-1", "banned", "primary" },
            plan.Outcomes.Select(outcome => outcome.Asset.Id).OrderBy(id => id));

        Assert.Equal(PlayerAssetOutcomeKind.Drop, plan.ForAsset("primary").Kind);
        Assert.Equal(PlayerAssetOutcomeKind.Keep, plan.ForAsset("backpack").Kind);
        Assert.Equal(PlayerAssetOutcomeKind.Keep, plan.ForAsset("backpack-content-1").Kind);
        Assert.Equal(PlayerAssetOutcomeKind.Delete, plan.ForAsset("banned").Kind);

        Assert.True(plan.ForAsset("backpack").RequiresRestoration);
        Assert.True(plan.ForAsset("backpack").IsDurableClaimEligible);
        Assert.False(plan.ForAsset("primary").RequiresRestoration);
        Assert.False(plan.ForAsset("banned").IsDurableClaimEligible);
    }
}
