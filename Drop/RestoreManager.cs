using System;
using System.Collections.Generic;
using System.Linq;
using FFEmqo.ModifiedItemDrop.Claim;
using FFEmqo.ModifiedItemDrop.Configuration;
using FFEmqo.ModifiedItemDrop.Domain;
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
        private readonly InventoryProcessor _inventoryProcessor;
        private readonly ClothingProcessor _clothingProcessor;
        private readonly ClaimService _claimService;
        private readonly IDurableClaimCreator _v2ClaimCreator;
        private readonly V2ClaimRecoveryService _v2ClaimRecoveryService;
        private readonly ConfigurationLoader _configurationLoader;

        public RestoreManager(InventoryProcessor inventoryProcessor, ClothingProcessor clothingProcessor, ClaimService claimService, ConfigurationLoader configurationLoader)
            : this(inventoryProcessor, clothingProcessor, claimService, null, null, configurationLoader)
        {
        }

        public RestoreManager(
            InventoryProcessor inventoryProcessor,
            ClothingProcessor clothingProcessor,
            ClaimService claimService,
            IDurableClaimCreator v2ClaimCreator,
            V2ClaimRecoveryService v2ClaimRecoveryService,
            ConfigurationLoader configurationLoader)
        {
            _inventoryProcessor = inventoryProcessor ?? throw new ArgumentNullException(nameof(inventoryProcessor));
            _clothingProcessor = clothingProcessor ?? throw new ArgumentNullException(nameof(clothingProcessor));
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

            _inventoryProcessor.RestoreInventory(player, pending);
            _clothingProcessor.RestoreClothing(player, pending);

            GiveRespawnItems(player);

            if (pending.IsEmpty)
            {
                return;
            }

            var saved = SavePendingToClaimOrDrop((ulong)player.CSteamID, pending);
            if (saved)
            {
                UtilityHelper.TryNotify(player, $"有 {pending.InventoryItems.Count + pending.ClothingItems.Count} 个物品已保存，使用 /mid claim 领取。");
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
                    _inventoryProcessor.RestoreInventory(player, pending);
                    _clothingProcessor.RestoreClothing(player, pending);
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
                    msg += " 仍有更多待领取，使用 /mid claim 继续领取。";
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
                    msg += " 仍有更多待领取，使用 /mid claim 继续领取。";
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

        private bool HasV1Pending(UnturnedPlayer player)
        {
            return player != null && _claimService != null && _claimService.GetPendingCount((ulong)player.CSteamID) > 0;
        }

        public void GiveRespawnItems(UnturnedPlayer player)
        {
            var respawnItems = _configurationLoader?.DeathSettings?.RespawnItems;
            if (respawnItems == null || respawnItems.Count == 0)
            {
                return;
            }

            var inventory = player?.Player?.inventory;
            if (inventory == null)
            {
                return;
            }

            var failedItems = new List<Item>();

            foreach (var respawnItem in respawnItems)
            {
                if (respawnItem.ItemID == 0 || respawnItem.Amount == 0)
                {
                    continue;
                }

                for (int i = 0; i < respawnItem.Amount; i++)
                {
                    var item = new Item(respawnItem.ItemID, true) { quality = respawnItem.Quality };
                    if (!inventory.tryAddItem(item, true))
                    {
                        failedItems.Add(item);
                    }
                }

                LoggingHelper.LogDebug($"Gave respawn item: id={respawnItem.ItemID} x{respawnItem.Amount}", _configurationLoader?.IsDebugLoggingEnabled ?? false);
            }

            if (failedItems.Count > 0)
            {
                var claim = _claimService?.AddClaim((ulong)player.CSteamID, player.Position, failedItems, null);
                if (claim != null)
                {
                    UtilityHelper.TryNotify(player, $"有 {failedItems.Count} 个复活物品空间不足，已存入待领取，使用 /mid claim 领取。");
                }
                else
                {
                    foreach (var item in failedItems)
                    {
                        UtilityHelper.DropWorldItem(item, player.Position);
                    }
                    UtilityHelper.TryNotify(player, $"{failedItems.Count} 个复活物品空间不足且 Claim 不可用，已掉落在当前位置。");
                }
            }
        }

    }
}
