using FFEmqo.ModifiedItemDrop.Domain;
using Xunit;

namespace FFEmqo.ModifiedItemDrop.Domain.Tests;

public sealed class DurableClaimFallbackPlannerTests
{
    [Fact]
    public void FailedDurableClaimCreationDropsEachUnresolvedKeptAssetExactlyOnceWhenImmediateRestoreIsUnavailable()
    {
        var backpack = new PlayerAsset("backpack", PlayerAssetSlot.Backpack, itemId: 253);
        var content = PlayerAsset.ClothingContent(
            "content-1",
            sourceClothingSlot: PlayerAssetSlot.Backpack,
            parentAssetId: backpack.Id,
            itemId: 15);
        var keptOutcomes = new[]
        {
            new PlayerAssetOutcome(backpack, PlayerAssetOutcomeKind.Keep, OutcomeRule.Keep("keep backpack", 100, OutcomeTarget.Any(), chance: 1.0)),
            new PlayerAssetOutcome(content, PlayerAssetOutcomeKind.Keep, OutcomeRule.Keep("keep content", 100, OutcomeTarget.Any(), chance: 1.0))
        };
        var failedClaim = DurableClaimCreateResult.Failure("disk full");

        var decisions = new DurableClaimFallbackPlanner().PlanAfterCreateFailure(
            keptOutcomes,
            failedClaim,
            immediateRestoreAvailable: false);

        Assert.Equal(new[] { "backpack", "content-1" }, decisions.Select(decision => decision.Asset.Id).OrderBy(id => id));
        Assert.All(decisions, decision => Assert.Equal(DurableClaimFallbackKind.DropFallback, decision.Kind));
        Assert.All(decisions, decision => Assert.Contains("disk full", decision.Reason));
    }

    [Fact]
    public void FailedDurableClaimCreationUsesImmediateRestoreWhenAvailable()
    {
        var asset = new PlayerAsset("primary", PlayerAssetSlot.PrimaryWeapon, itemId: 363);
        var keptOutcomes = new[]
        {
            new PlayerAssetOutcome(asset, PlayerAssetOutcomeKind.Keep, OutcomeRule.Keep("keep primary", 100, OutcomeTarget.Any(), chance: 1.0))
        };

        var decisions = new DurableClaimFallbackPlanner().PlanAfterCreateFailure(
            keptOutcomes,
            DurableClaimCreateResult.Failure("disk full"),
            immediateRestoreAvailable: true);

        var decision = Assert.Single(decisions);
        Assert.Equal(DurableClaimFallbackKind.ImmediateRestore, decision.Kind);
        Assert.Equal("primary", decision.Asset.Id);
    }

}
