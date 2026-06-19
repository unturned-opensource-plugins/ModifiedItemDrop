using System;
using System.Collections.Generic;
using FFEmqo.ModifiedItemDrop.Domain;
using FFEmqo.ModifiedItemDrop.Models;
using SDG.Unturned;

namespace FFEmqo.ModifiedItemDrop.Drop
{
    public static class PendingRestoreV2ClaimAdapter
    {
        public static DurableClaimRecord ToDurableClaimRecord(string claimId, ulong steamId, PendingRestore pending)
        {
            if (pending == null)
            {
                throw new ArgumentNullException(nameof(pending));
            }

            var assets = new List<DurableClaimAsset>();
            var sequence = 0;

            foreach (var pendingItem in pending.InventoryItems)
            {
                if (pendingItem?.Item == null || pendingItem.Item.id == 0)
                {
                    continue;
                }

                assets.Add(ToAsset("inventory", sequence++, pendingItem.Item));
            }

            foreach (var clothing in pending.ClothingItems)
            {
                if (clothing?.Item == null || clothing.Item.id == 0)
                {
                    continue;
                }

                assets.Add(ToAsset("clothing:" + clothing.SlotType, sequence++, clothing.Item));

                if (clothing.Contents == null)
                {
                    continue;
                }

                foreach (var content in clothing.Contents)
                {
                    if (content?.Item == null || content.Item.id == 0)
                    {
                        continue;
                    }

                    assets.Add(ToAsset("clothing-content:" + clothing.SlotType, sequence++, content.Item));
                }
            }

            foreach (var pair in pending.ClothingContentsToRestore)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                foreach (var item in pair.Value)
                {
                    if (item == null || item.id == 0)
                    {
                        continue;
                    }

                    assets.Add(ToAsset("orphan-clothing-content:" + pair.Key, sequence++, item));
                }
            }

            return new DurableClaimRecord(claimId, steamId, assets);
        }

        private static DurableClaimAsset ToAsset(string source, int sequence, Item item)
        {
            return new DurableClaimAsset(
                source + ":" + sequence,
                item.id,
                item.amount,
                item.quality,
                item.state != null ? (byte[])item.state.Clone() : Array.Empty<byte>());
        }
    }
}
