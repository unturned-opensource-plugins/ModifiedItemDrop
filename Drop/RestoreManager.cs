using System;
using FFEmqo.ModifiedItemDrop.Claim;
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

        public RestoreManager(InventoryProcessor inventoryProcessor, ClothingProcessor clothingProcessor, ClaimService claimService)
        {
            _inventoryProcessor = inventoryProcessor ?? throw new ArgumentNullException(nameof(inventoryProcessor));
            _clothingProcessor = clothingProcessor ?? throw new ArgumentNullException(nameof(clothingProcessor));
            _claimService = claimService;
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

            var inventoryRestored = _inventoryProcessor.RestoreInventory(player, pending);
            var clothingRestored = _clothingProcessor.RestoreClothing(player, pending);

            // Clear any remaining clothing contents (should not happen since clothing contents drop with clothing)
            pending.ClothingContentsToRestore.Clear();

            if (pending.IsEmpty)
            {
                return;
            }

            // Save remaining items to persistent claim storage
            if (_claimService != null)
            {
                var steamId = (ulong)player.CSteamID;
                var remainingItems = pending.InventoryItems;
                var remainingClothing = pending.ClothingItems;
                var claim = _claimService.AddClaim(steamId, pending.DeathPosition, remainingItems, remainingClothing);

                if (claim != null)
                {
                    UtilityHelper.TryNotify(player, $"有 {pending.InventoryItems.Count + pending.ClothingItems.Count} 个物品已保存，使用 /mid claim 领取。");
                }
            }
            else
            {
                UtilityHelper.TryNotify(player, $"有 {pending.InventoryItems.Count} 个物品未能放入背包，使用 /mid claim 以稍后领取。");
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
            if (player == null || _claimService == null)
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
            if (player == null || _claimService == null)
            {
                return;
            }

            if (_claimService.ClaimAll(player, out var totalItems, out var totalClothing, out var claimCount))
            {
                if (totalItems > 0 || totalClothing > 0)
                {
                    UtilityHelper.TryNotify(player, $"已自动领取 {claimCount} 个包，共 {totalItems} 个物品和 {totalClothing} 件衣物。");
                }
            }
        }

    }
}
