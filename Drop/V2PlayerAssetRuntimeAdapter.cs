using System;
using System.Collections.Generic;
using FFEmqo.ModifiedItemDrop.Domain;
using FFEmqo.ModifiedItemDrop.Models;
using SDG.Unturned;

namespace FFEmqo.ModifiedItemDrop.Drop
{
    public static class V2PlayerAssetRuntimeAdapter
    {
        public static PlayerAsset ProjectInventoryItem(InventoryItemSnapshot snapshot)
        {
            if (snapshot?.Jar?.item == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var item = snapshot.Jar.item;
            return PlayerAssetProjection.InventoryItem(
                snapshot.Page,
                snapshot.Index,
                item.id,
                item.amount,
                item.quality,
                item.state);
        }

        public static IReadOnlyList<PlayerAsset> ProjectClothingItem(ClothingItemSnapshot snapshot)
        {
            if (snapshot?.Item == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var assets = new List<PlayerAsset>();
            var slot = ToDomainSlot(snapshot.SlotType);
            var clothingAsset = PlayerAssetProjection.Clothing(
                slot,
                snapshot.Item.id,
                snapshot.Item.amount,
                snapshot.Item.quality,
                snapshot.Item.state);

            assets.Add(clothingAsset);

            if (snapshot.Contents == null)
            {
                return assets;
            }

            foreach (var content in snapshot.Contents)
            {
                if (content?.Item == null || content.Item.id == 0)
                {
                    continue;
                }

                assets.Add(PlayerAssetProjection.ClothingContent(
                    slot,
                    clothingAsset.Id,
                    content.Index,
                    content.Item.id,
                    content.Item.amount,
                    content.Item.quality,
                    content.Item.state));
            }

            return assets;
        }

        public static PlayerAssetSlot ToDomainSlot(SlotType slot)
        {
            switch (slot)
            {
                case SlotType.PrimaryWeapon:
                    return PlayerAssetSlot.PrimaryWeapon;
                case SlotType.SecondaryWeapon:
                    return PlayerAssetSlot.SecondaryWeapon;
                case SlotType.Hands:
                    return PlayerAssetSlot.Hands;
                case SlotType.Backpack:
                    return PlayerAssetSlot.Backpack;
                case SlotType.Vest:
                    return PlayerAssetSlot.Vest;
                case SlotType.Shirt:
                    return PlayerAssetSlot.Shirt;
                case SlotType.Pants:
                    return PlayerAssetSlot.Pants;
                case SlotType.Hat:
                    return PlayerAssetSlot.Hat;
                case SlotType.Mask:
                    return PlayerAssetSlot.Mask;
                case SlotType.Glasses:
                    return PlayerAssetSlot.Glasses;
                default:
                    throw new ArgumentOutOfRangeException(nameof(slot), slot, "Unsupported runtime Player Asset slot.");
            }
        }
    }
}
