using System;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public static class PlayerAssetProjection
    {
        public static PlayerAsset InventoryItem(byte page, byte index, ushort itemId, byte amount, byte quality, byte[]? state)
        {
            return new PlayerAsset(
                "inventory:" + page + ":" + index,
                SlotFromInventoryPage(page),
                itemId,
                amount,
                quality,
                state)
                .WithInventoryLocation(page, index);
        }

        public static PlayerAsset Clothing(PlayerAssetSlot slot, ushort itemId, byte amount, byte quality, byte[]? state)
        {
            return new PlayerAsset(
                "clothing:" + slot,
                slot,
                itemId,
                amount,
                quality,
                state);
        }

        public static PlayerAsset ClothingContent(
            PlayerAssetSlot sourceSlot,
            string parentAssetId,
            byte contentIndex,
            ushort itemId,
            byte amount,
            byte quality,
            byte[]? state)
        {
            return PlayerAsset.ClothingContent(
                "clothing-content:" + sourceSlot + ":" + contentIndex,
                sourceSlot,
                parentAssetId,
                itemId,
                amount,
                quality,
                state);
        }

        private static PlayerAssetSlot SlotFromInventoryPage(byte page)
        {
            switch (page)
            {
                case 0:
                    return PlayerAssetSlot.PrimaryWeapon;
                case 1:
                    return PlayerAssetSlot.SecondaryWeapon;
                case 2:
                    return PlayerAssetSlot.Hands;
                default:
                    throw new ArgumentOutOfRangeException(nameof(page), page, "Only quick-slot inventory pages 0-2 are Player Asset death-processing inputs.");
            }
        }
    }
}
