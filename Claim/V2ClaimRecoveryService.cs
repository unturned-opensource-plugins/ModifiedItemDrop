using System;
using System.Collections.Generic;
using System.Linq;
using FFEmqo.ModifiedItemDrop.Domain;
using FFEmqo.ModifiedItemDrop.Utilities;
using Rocket.Unturned.Player;
using SDG.Unturned;

namespace FFEmqo.ModifiedItemDrop.Claim
{
    public sealed class V2ClaimRecoveryService
    {
        private readonly DurableClaimStore _store;

        public V2ClaimRecoveryService(DurableClaimStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public bool ClaimOldest(UnturnedPlayer player, out int itemsRestored, out bool hasMore)
        {
            itemsRestored = 0;
            hasMore = false;

            if (player?.Player?.inventory == null)
            {
                return false;
            }

            var steamId = (ulong)player.CSteamID;
            var loadResult = _store.Load();
            LogLoadWarnings(loadResult);

            var claim = loadResult.Claims.FirstOrDefault(record => record.SteamId == steamId);
            if (claim == null)
            {
                return false;
            }

            itemsRestored = RestoreClaimAssets(player, claim);
            hasMore = _store.Load().Claims.Any(record => record.SteamId == steamId);
            return itemsRestored > 0;
        }

        public bool ClaimAll(UnturnedPlayer player, out int totalItems, out int claimCount)
        {
            totalItems = 0;
            claimCount = 0;

            if (player?.Player?.inventory == null)
            {
                return false;
            }

            var steamId = (ulong)player.CSteamID;
            while (true)
            {
                var loadResult = _store.Load();
                LogLoadWarnings(loadResult);

                var claim = loadResult.Claims.FirstOrDefault(record => record.SteamId == steamId);
                if (claim == null)
                {
                    break;
                }

                var restored = RestoreClaimAssets(player, claim);
                if (restored == 0)
                {
                    break;
                }

                claimCount++;
                totalItems += restored;
            }

            return claimCount > 0;
        }

        private int RestoreClaimAssets(UnturnedPlayer player, DurableClaimRecord claim)
        {
            var inventory = player.Player?.inventory;
            if (inventory == null)
            {
                return 0;
            }

            var prunedAssetIds = new List<string>();
            var restoredCount = 0;
            foreach (var asset in claim.Assets)
            {
                if (asset == null)
                {
                    continue;
                }

                if (asset.ItemId == 0)
                {
                    prunedAssetIds.Add(asset.AssetId);
                    continue;
                }

                var item = new Item(asset.ItemId, asset.Amount, asset.Quality, asset.State ?? Array.Empty<byte>());
                if (inventory.tryAddItem(item, true))
                {
                    prunedAssetIds.Add(asset.AssetId);
                    restoredCount++;
                }
            }

            if (prunedAssetIds.Count == 0)
            {
                return 0;
            }

            var pruneResult = _store.TryPruneAssets(claim.Id, prunedAssetIds);
            if (!pruneResult.Removed)
            {
                LoggingHelper.LogWarning("[ModifiedItemDrop] V2 Durable Claim prune failed after restoring assets from claim " + claim.Id + ": " + pruneResult.ErrorMessage);
            }

            return restoredCount;
        }

        private static void LogLoadWarnings(DurableClaimLoadResult loadResult)
        {
            if (loadResult?.Warnings == null)
            {
                return;
            }

            foreach (var warning in loadResult.Warnings)
            {
                LoggingHelper.LogWarning("[ModifiedItemDrop] " + warning);
            }
        }
    }
}
