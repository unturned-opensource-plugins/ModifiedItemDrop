using FFEmqo.ModifiedItemDrop.Domain;
using Xunit;

namespace FFEmqo.ModifiedItemDrop.Domain.Tests;

public sealed class MidRulesExplainTargetParserTests
{
    [Fact]
    public void SlotTargetCreatesSyntheticPlayerAssetForExplanation()
    {
        var target = MidRulesExplainTargetParser.Parse(new[] { "slot", "PrimaryWeapon" });

        Assert.True(target.Accepted);
        Assert.NotNull(target.Asset);
        Assert.Equal(PlayerAssetSlot.PrimaryWeapon, target.Asset.Slot);
        Assert.Equal(0, target.Asset.ItemId);
    }

    [Fact]
    public void ItemTargetCreatesSyntheticPlayerAssetForExplanation()
    {
        var target = MidRulesExplainTargetParser.Parse(new[] { "item", "363" });

        Assert.True(target.Accepted);
        Assert.NotNull(target.Asset);
        Assert.Equal(363, target.Asset.ItemId);
    }

}
