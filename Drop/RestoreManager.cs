using System;
using System.Collections.Generic;
using System.Linq;
using FFEmqo.ModifiedItemDrop.Claim;
using FFEmqo.ModifiedItemDrop.Configuration;
using FFEmqo.ModifiedItemDrop.Domain;
using FFEmqo.ModifiedItemDrop.Extensions;
using FFEmqo.ModifiedItemDrop.Models;
using FFEmqo.ModifiedItemDrop.Utilities;
using Rocket.Unturned.Player;
using SDG.Unturned;

namespace FFEmqo.ModifiedItemDrop.Drop
{
    /// <summary>
    /// Manages item restoration logic for players.
    /// Handles both immediate restoration and persistent claim storage.
    /// </summary>
    public sealed class RestoreManager
    {
        private const byte AnyInventoryPage = byte.MaxValue;
        private readonly ClaimService _claimService;
        private readonly IDurableClaimCreator _v2ClaimCreator;
        private readonly V2ClaimRecoveryService _v2ClaimRecoveryService;
        private readonly ConfigurationLoader _configurationLoader;

        public RestoreManager(
            ClaimService claimService,
            IDurableClaimCreator v2ClaimCreator,
            V2ClaimRecoveryService v2ClaimRecoveryService,
            ConfigurationLoader configurationLoader)
        {
            _claimService = claimService;
            _v2ClaimCreator = v2ClaimCreator;
            _v2ClaimRecoveryService = v2ClaimRecoveryService;
            _configurationLoader = configurationLoader;
        }

        /// <summary>
        /// Restores items to a player from pending restore data.
        /// If items cannot be restored, they are saved to claim storage.
        /// </summary>
        public void RestorePendingItems(UnturnedPlayer player, PendingRestore pending)
        {
            if (player == null || pending == null || pending.IsEmpty)
            {
                return;
            }

            RestoreInventory(player, pending);
            RestoreClothing(player, pending);

            if (pending.IsEmpty)
            {
                return;
            }

            var saved = SavePendingToClaimOrDrop((ulong)player.CSteamID, pending);
            if (saved)
            {
                UtilityHelper.TryNotify(player, $"有 {pending.InventoryItems.Count + pending.ClothingItems.Count} 个物品已保存，使用 /mid claims recover oldest 领取。");
            }
            else
            {
                UtilityHelper.TryNotify(player, "部分物品无法放入背包且 Claim 不可用，已掉落到死亡位置。");
            }
        }

        public bool SavePendingToClaimOrDrop(ulong steamId, PendingRestore pending)
        {
            if (pending == null || pending.IsEmpty)
            {
                return false;
            }

            if (_v2ClaimCreator != null)
            {
                var claimId = Guid.NewGuid().ToString("N");
                var v2Claim = PendingRestoreV2ClaimAdapter.ToDurableClaimRecord(claimId, steamId, pending);
                var createResult = _v2ClaimCreator.TryCreate(v2Claim);
                if (createResult.Created)
                {
                    return true;
                }

                LoggingHelper.LogWarning("[ModifiedItemDrop] V2 Durable Claim creation failed; dropping pending Player Assets. " + createResult.ErrorMessage);
                DropPendingToGround(pending);
                return false;
            }

            if (_claimService != null)
            {
                var remainingClothingSlots = new HashSet<SlotType>(
                    pending.ClothingItems
                        .Where(x => x?.Item != null && x.Item.id != 0)
                        .Select(x => x.SlotType));
                var remainingItems = pending.InventoryItems
                    .Where(x => x?.Item != null && x.Item.id != 0)
                    .Select(x => x.Item)
                    .ToList();

                foreach (var pair in pending.ClothingContentsToRestore)
                {
                    if (remainingClothingSlots.Contains(pair.Key) || pair.Value == null)
                    {
                        continue;
                    }

                    foreach (var item in pair.Value)
                    {
                        if (item != null && item.id != 0)
                        {
                            remainingItems.Add(item);
                        }
                    }
                }

                var remainingClothing = pending.ClothingItems;
                var claim = _claimService.AddClaim(steamId, pending.DeathPosition, remainingItems, remainingClothing);
                if (claim != null)
                {
                    return true;
                }
            }

            DropPendingToGround(pending);
            return false;
        }

        public void DropPendingToGround(PendingRestore pending)
        {
            if (pending == null)
            {
                return;
            }

            var slotsWithClothing = new HashSet<SlotType>();

            foreach (var pendingItem in pending.InventoryItems)
            {
                if (pendingItem?.Item != null && pendingItem.Item.id != 0)
                {
                    UtilityHelper.DropWorldItem(pendingItem.Item, pending.DeathPosition);
                }
            }

            foreach (var clothing in pending.ClothingItems)
            {
                if (clothing?.Item == null || clothing.Item.id == 0)
                {
                    continue;
                }

                slotsWithClothing.Add(clothing.SlotType);
                UtilityHelper.DropWorldItem(clothing.Item, pending.DeathPosition);

                if (clothing.Contents == null)
                {
                    continue;
                }

                foreach (var content in clothing.Contents)
                {
                    if (content?.Item != null && content.Item.id != 0)
                    {
                        UtilityHelper.DropWorldItem(content.Item, pending.DeathPosition);
                    }
                }
            }

            foreach (var pair in pending.ClothingContentsToRestore)
            {
                if (slotsWithClothing.Contains(pair.Key) || pair.Value == null)
                {
                    continue;
                }

                foreach (var item in pair.Value)
                {
                    if (item != null && item.id != 0)
                    {
                        UtilityHelper.DropWorldItem(item, pending.DeathPosition);
                    }
                }
            }
        }

        /// <summary>
        /// Immediately restores all pending items to a player.
        /// Used as a fallback when normal restoration fails.
        /// </summary>
        public void RestoreImmediately(UnturnedPlayer player, PendingRestore pending)
        {
            LoggingHelper.SafeExecute(
                () =>
                {
                    RestoreInventory(player, pending);
                    RestoreClothing(player, pending);
                },
                "RestoreImmediately"
            );
        }

        /// <summary>
        /// Handles claim restoration from persistent storage.
        /// </summary>
        public void ClaimPending(UnturnedPlayer player)
        {
            if (player == null || (_v2ClaimRecoveryService == null && _claimService == null))
            {
                UtilityHelper.TryNotify(player, "没有可领取的待发放物品。");
                return;
            }

            if (_v2ClaimRecoveryService != null && !_v2ClaimRecoveryService.RecoveryEnabled)
            {
                UtilityHelper.TryNotify(player, "Claim 存储处于降级模式，领取已禁用。请联系管理员检查存储文件。");
                return;
            }

            if (_v2ClaimRecoveryService != null &&
                _v2ClaimRecoveryService.ClaimOldest(player, out var v2ItemsRestored, out var v2HasMore))
            {
                var msg = $"已领取 {v2ItemsRestored} 个物品和 0 件衣物。";
                if (v2HasMore || HasV1Pending(player))
                {
                    msg += " 仍有更多待领取，使用 /mid claims recover oldest 继续领取。";
                }

                UtilityHelper.TryNotify(player, msg);
                return;
            }

            if (_claimService == null)
            {
                UtilityHelper.TryNotify(player, "没有可领取的待发放物品。");
                return;
            }

            if (_claimService.ClaimOldest(player, out var itemsRestored, out var clothingRestored, out var hasMore))
            {
                var msg = $"已领取 {itemsRestored} 个物品和 {clothingRestored} 件衣物。";
                if (hasMore)
                {
                    msg += " 仍有更多待领取，使用 /mid claims recover oldest 继续领取。";
                }
                UtilityHelper.TryNotify(player, msg);
            }
            else
            {
                UtilityHelper.TryNotify(player, "没有可领取的待发放物品。");
            }
        }

        /// <summary>
        /// Claims all pending items from persistent storage.
        /// </summary>
        public void ClaimAllPending(UnturnedPlayer player)
        {
            if (player == null || (_v2ClaimRecoveryService == null && _claimService == null))
            {
                return;
            }

            if (_v2ClaimRecoveryService != null && !_v2ClaimRecoveryService.RecoveryEnabled)
            {
                UtilityHelper.TryNotify(player, "Claim 存储处于降级模式，领取已禁用。请联系管理员检查存储文件。");
                return;
            }

            var totalItems = 0;
            var totalClothing = 0;
            var claimCount = 0;

            if (_v2ClaimRecoveryService != null &&
                _v2ClaimRecoveryService.ClaimAll(player, out var v2Items, out var v2ClaimCount))
            {
                totalItems += v2Items;
                claimCount += v2ClaimCount;
            }

            if (_claimService != null &&
                _claimService.ClaimAll(player, out var v1Items, out var v1Clothing, out var v1ClaimCount))
            {
                totalItems += v1Items;
                totalClothing += v1Clothing;
                claimCount += v1ClaimCount;
            }

            if (totalItems > 0 || totalClothing > 0)
            {
                UtilityHelper.TryNotify(player, $"已自动领取 {claimCount} 个包，共 {totalItems} 个物品和 {totalClothing} 件衣物。");
            }
        }

        private bool RestoreInventory(UnturnedPlayer player, PendingRestore pending)
        {
            var inventory = player.Player?.inventory;
            if (inventory == null)
            {
                return false;
            }

            var restored = 0;
            for (var i = pending.InventoryItems.Count - 1; i >= 0; i--)
            {
                var pendingItem = pending.InventoryItems[i];
                var item = pendingItem?.Item;
                if (item == null || item.id == 0)
                {
                    pending.InventoryItems.RemoveAt(i);
                    continue;
                }

                var ok = TryAddToPreferredPage(inventory, pendingItem);
                if (!ok)
                {
                    ok = inventory.tryAddItem(UtilityHelper.CloneItem(item), true);
                }

                if (ok)
                {
                    pending.InventoryItems.RemoveAt(i);
                    restored++;
                }
            }

            DebugLog("Restored " + restored + " inventory Player Asset(s).");
            return restored > 0;
        }

        private static bool TryAddToPreferredPage(PlayerInventory inventory, PendingInventoryItem pendingItem)
        {
            if (inventory?.items == null || pendingItem?.Item == null)
            {
                return false;
            }

            var sourcePage = pendingItem.SourcePage;
            if (sourcePage >= inventory.items.Length || sourcePage >= PlayerInventory.PAGES)
            {
                return false;
            }

            var pageContainer = inventory.items[sourcePage];
            if (pageContainer == null)
            {
                return false;
            }

            return pageContainer.tryAddItem(UtilityHelper.CloneItem(pendingItem.Item), true);
        }

        private bool RestoreClothing(UnturnedPlayer player, PendingRestore pending)
        {
            var clothing = player.Player?.clothing;
            if (clothing == null)
            {
                return false;
            }

            var restored = 0;
            for (var i = pending.ClothingItems.Count - 1; i >= 0; i--)
            {
                var snap = pending.ClothingItems[i];
                if (snap?.Item == null || snap.Item.id == 0)
                {
                    pending.ClothingItems.RemoveAt(i);
                    continue;
                }

                var state = snap.Item.state ?? Array.Empty<byte>();
                if (!ClothingOperationHelper.TryWearClothing(clothing, snap.SlotType, snap.Item.id, snap.Item.quality, state))
                {
                    continue;
                }

                restored++;
                RestoreClothingContentsToSlot(clothing, pending, snap.SlotType);
                pending.ClothingItems.RemoveAt(i);
            }

            DebugLog("Restored " + restored + " clothing Player Asset(s).");
            return restored > 0;
        }

        private static void RestoreClothingContentsToSlot(SDG.Unturned.PlayerClothing clothing, PendingRestore pending, SlotType slotType)
        {
            if (!pending.ClothingContentsToRestore.TryGetValue(slotType, out var contents))
            {
                return;
            }

            var container = PlayerExtensions.GetClothingContainer(clothing, slotType);
            foreach (var item in contents)
            {
                if (item == null || item.id == 0)
                {
                    continue;
                }

                if (container == null || !container.tryAddItem(UtilityHelper.CloneItem(item), true))
                {
                    pending.InventoryItems.Add(new PendingInventoryItem(UtilityHelper.CloneItem(item), AnyInventoryPage));
                }
            }

            pending.ClothingContentsToRestore.Remove(slotType);
        }

        private void DebugLog(string message)
        {
            LoggingHelper.LogDebug(message, _configurationLoader?.IsDebugLoggingEnabled ?? false);
        }

        private bool HasV1Pending(UnturnedPlayer player)
        {
            return player != null && _claimService != null && _claimService.GetPendingCount((ulong)player.CSteamID) > 0;
        }

   }
}
