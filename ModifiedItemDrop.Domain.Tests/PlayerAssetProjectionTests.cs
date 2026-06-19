using FFEmqo.ModifiedItemDrop.Domain;
using Xunit;

namespace FFEmqo.ModifiedItemDrop.Domain.Tests;

public sealed class PlayerAssetProjectionTests
{
    [Fact]
    public void InventoryQuickSlotProjectionCreatesCanonicalPlayerAssetWithItemData()
    {
        var state = new byte[] { 9, 8, 7 };

        var asset = PlayerAssetProjection.InventoryItem(
            page: 0,
            index: 1,
            itemId: 363,
            amount: 2,
            quality: 75,
            state: state);

        state[0] = 1;

        Assert.Equal("inventory:0:1", asset.Id);
        Assert.Equal(PlayerAssetSlot.PrimaryWeapon, asset.Slot);
        Assert.Equal(363, asset.ItemId);
        Assert.Equal(2, asset.Amount);
        Assert.Equal(75, asset.Quality);
        Assert.Equal(new byte[] { 9, 8, 7 }, asset.State);
        Assert.False(asset.IsClothingContent);
    }


    [Fact]
    public void ClothingContentProjectionReferencesParentClothingAsset()
    {
        var content = PlayerAssetProjection.ClothingContent(
            sourceSlot: PlayerAssetSlot.Backpack,
            parentAssetId: "clothing:Backpack",
            contentIndex: 3,
            itemId: 15,
            amount: 4,
            quality: 90,
            state: new byte[] { 1, 2 });

        Assert.Equal("clothing-content:Backpack:3", content.Id);
        Assert.True(content.IsClothingContent);
        Assert.Equal(PlayerAssetSlot.Backpack, content.Slot);
        Assert.Equal(PlayerAssetSlot.Backpack, content.SourceClothingSlot);
        Assert.Equal("clothing:Backpack", content.ParentAssetId);
        Assert.Equal(15, content.ItemId);
        Assert.Equal(4, content.Amount);
        Assert.Equal(90, content.Quality);
        Assert.Equal(new byte[] { 1, 2 }, content.State);
    }
}
