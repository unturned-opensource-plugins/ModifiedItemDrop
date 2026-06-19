using FFEmqo.ModifiedItemDrop.Domain;
using Xunit;

namespace FFEmqo.ModifiedItemDrop.Domain.Tests;

public sealed class DeathOutcomeExecutionPlannerTests
{
    [Fact]
    public void DropDeleteAndKeepOutcomesBecomeExplicitRuntimeActions()
    {
        var primary = new PlayerAsset("inventory:0:0", PlayerAssetSlot.PrimaryWeapon, itemId: 363);
        var banned = new PlayerAsset("inventory:1:0", PlayerAssetSlot.SecondaryWeapon, itemId: 95);
        var hands = new PlayerAsset("inventory:2:0", PlayerAssetSlot.Hands, itemId: 116);
        var plan = new DeathOutcomePlan(new[]
        {
            new PlayerAssetOutcome(primary, PlayerAssetOutcomeKind.Drop, OutcomeRule.Drop("drop primary", 100, OutcomeTarget.ForSlot(PlayerAssetSlot.PrimaryWeapon), chance: 1.0)),
            new PlayerAssetOutcome(banned, PlayerAssetOutcomeKind.Delete, OutcomeRule.Delete("delete banned", 100, OutcomeTarget.ForItem(95))),
            new PlayerAssetOutcome(hands, PlayerAssetOutcomeKind.Keep, OutcomeRule.Keep("keep hands", 100, OutcomeTarget.ForSlot(PlayerAssetSlot.Hands), chance: 1.0))
        });

        var execution = new DeathOutcomeExecutionPlanner().Plan(plan);

        Assert.Equal(3, execution.Actions.Count);
        Assert.Equal(DeathOutcomeExecutionActionKind.Drop, execution.ForAsset("inventory:0:0").Kind);
        Assert.Equal(DeathOutcomeExecutionActionKind.Delete, execution.ForAsset("inventory:1:0").Kind);
        Assert.Equal(DeathOutcomeExecutionActionKind.KeepForRestore, execution.ForAsset("inventory:2:0").Kind);
    }


    [Fact]
    public void ClothingParentAndContentsKeepIndependentRuntimeActions()
    {
        var backpack = new PlayerAsset("clothing:Backpack", PlayerAssetSlot.Backpack, itemId: 253);
        var content = PlayerAsset.ClothingContent(
            "clothing-content:Backpack:0",
            PlayerAssetSlot.Backpack,
            backpack.Id,
            itemId: 15);
        var plan = new DeathOutcomePlan(new[]
        {
            new PlayerAssetOutcome(backpack, PlayerAssetOutcomeKind.Keep, OutcomeRule.Keep("keep backpack", 100, OutcomeTarget.ForSlot(PlayerAssetSlot.Backpack), chance: 1.0)),
            new PlayerAssetOutcome(content, PlayerAssetOutcomeKind.Drop, OutcomeRule.Drop("drop backpack contents", 90, OutcomeTarget.ForClothingContent(PlayerAssetSlot.Backpack), chance: 1.0))
        });

        var execution = new DeathOutcomeExecutionPlanner().Plan(plan);

        Assert.Equal(DeathOutcomeExecutionActionKind.KeepForRestore, execution.ForAsset("clothing:Backpack").Kind);
        Assert.Equal(DeathOutcomeExecutionActionKind.Drop, execution.ForAsset("clothing-content:Backpack:0").Kind);
    }
}
