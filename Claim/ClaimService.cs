using System;
using System.Collections.Generic;
using System.Linq;
using FFEmqo.ModifiedItemDrop.Configuration;
using FFEmqo.ModifiedItemDrop.Extensions;
using FFEmqo.ModifiedItemDrop.Models;
using FFEmqo.ModifiedItemDrop.Utilities;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;
using Item = SDG.Unturned.Item;

namespace FFEmqo.ModifiedItemDrop.Claim
{
    public sealed class ClaimService
    {
        private readonly ClaimStorage _storage;
        private readonly Func<ClaimSettings> _settingsProvider;

        public ClaimService(ClaimStorage storage, Func<ClaimSettings> settingsProvider)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
        }

        public void Initialize()
        {
            _storage.Load();
            CleanupExpired();
        }

        public ClaimSettings GetClaimSettings()
        {
            return _settingsProvider();
        }

        public ClaimRecord AddClaim(ulong steamId, Vector3 deathPosition, List<Item> inventoryItems, List<ClothingItemSnapshot> clothingItems)
        {
            var settings = _settingsProvider();
            if (!settings.EnableClaim)
            {
                return null;
            }

            var claim = new ClaimRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                SteamId = steamId,
                CreatedAt = DateTime.UtcNow,
                DeathX = deathPosition.x,
                DeathY = deathPosition.y,
                DeathZ = deathPosition.z,
                Items = new List<ClaimItem>(),
                Clothing = new List<ClaimClothing>()
            };

            if (settings.ExpirationMinutes > 0)
            {
                claim.ExpiresAt = claim.CreatedAt.AddMinutes(settings.ExpirationMinutes);
            }

            if (inventoryItems != null)
            {
                foreach (var item in inventoryItems)
                {
                    if (item == null || item.id == 0)
                    {
                        continue;
                    }

                    claim.Items.Add(new ClaimItem
                    {
                        ItemId = item.id,
                        Amount = item.amount,
                        Quality = item.quality,
                        State = item.state != null ? (byte[])item.state.Clone() : Array.Empty<byte>()
                    });
                }
            }

            if (clothingItems != null)
            {
                foreach (var clothing in clothingItems)
                {
                    if (clothing?.Item == null || clothing.Item.id == 0)
                    {
                        continue;
                    }

                    var claimClothing = new ClaimClothing
                    {
                        Slot = clothing.SlotType,
                        ItemId = clothing.Item.id,
                        Quality = clothing.Item.quality,
                        State = clothing.Item.state != null ? (byte[])clothing.Item.state.Clone() : Array.Empty<byte>()
                    };

                    // Save clothing contents
                    if (clothing.Contents != null && clothing.Contents.Count > 0)
                    {
                        foreach (var content in clothing.Contents)
                        {
                            if (content?.Item != null && content.Item.id != 0)
                            {
                                claimClothing.Contents.Add(new ClaimItem
                                {
                                    ItemId = content.Item.id,
                                    Amount = content.Item.amount,
                                    Quality = content.Item.quality,
                                    State = content.Item.state != null ? (byte[])content.Item.state.Clone() : Array.Empty<byte>()
                                });
                            }
                        }
                    }

                    claim.Clothing.Add(claimClothing);
                }
            }

            if (claim.IsEmpty)
            {
                return null;
            }

            HandleOverLimit(steamId, settings, deathPosition);
            _storage.Add(claim);
            LoggingHelper.LogInfo($"Created claim {claim.Id} for {steamId} with {claim.Items.Count} items and {claim.Clothing.Count} clothing.");
            return claim;
        }

        private void HandleOverLimit(ulong steamId, ClaimSettings settings, Vector3 deathPosition)
        {
            if (settings.MaxClaimsPerPlayer <= 0 || settings.OverLimitBehavior == OverLimitBehavior.IgnoreLimit)
            {
                return;
            }

            var currentCount = _storage.GetCountBySteamId(steamId);
            if (currentCount < settings.MaxClaimsPerPlayer)
            {
                return;
            }

            var excess = currentCount - settings.MaxClaimsPerPlayer + 1;
            var oldest = _storage.GetBySteamId(steamId).Take(excess).ToList();

            switch (settings.OverLimitBehavior)
            {
                case OverLimitBehavior.DeleteOldest:
                    _storage.RemoveRange(oldest);
                    LoggingHelper.LogInfo($"Deleted {oldest.Count} oldest claims for {steamId} due to limit.");
                    break;
                case OverLimitBehavior.DropToGround:
                    foreach (var claim in oldest)
                    {
                        var pos = new Vector3(claim.DeathX, claim.DeathY, claim.DeathZ);
                        DropClaimToGround(claim, pos);
                    }
                    _storage.RemoveRange(oldest);
                    LoggingHelper.LogInfo($"Dropped {oldest.Count} oldest claims to ground for {steamId} due to limit.");
                    break;
            }
        }

        public List<ClaimRecord> GetClaims(ulong steamId)
        {
            return _storage.GetBySteamId(steamId);
        }

        public int GetPendingCount(ulong steamId)
        {
            return _storage.GetCountBySteamId(steamId);
        }

        public bool ClaimOldest(UnturnedPlayer player, out int itemsRestored, out int clothingRestored, out bool hasMore)
        {
            itemsRestored = 0;
            clothingRestored = 0;
            hasMore = false;

            if (player == null)
            {
                return false;
            }

            var steamId = (ulong)player.CSteamID;
            var claim = _storage.GetOldest(steamId);
            if (claim == null)
            {
                return false;
            }

            EnsureClaimLists(claim);
            var restoredItems = RestoreItemsAndPrune(player, claim.Items);
            var restoredClothing = RestoreClothing(player, claim.Clothing, claim.Items);

            itemsRestored = restoredItems;
            clothingRestored = restoredClothing;

            if (claim.IsEmpty)
            {
                _storage.Remove(claim);
            }
            else
            {
                _storage.ForceSave();
            }

            hasMore = _storage.GetCountBySteamId(steamId) > 0;

            return restoredItems > 0 || restoredClothing > 0;
        }

        public bool ClaimAll(UnturnedPlayer player, out int totalItems, out int totalClothing, out int claimCount)
        {
            totalItems = 0;
            totalClothing = 0;
            claimCount = 0;

            if (player == null)
            {
                return false;
            }

            var steamId = (ulong)player.CSteamID;
            var claims = _storage.GetBySteamId(steamId);

            if (claims.Count == 0)
            {
                return false;
            }

            var claimsToRemove = new List<ClaimRecord>();

            foreach (var claim in claims)
            {
                if (claim == null)
                {
                    continue;
                }

                EnsureClaimLists(claim);
                var restoredItems = RestoreItemsAndPrune(player, claim.Items);
                var restoredClothing = RestoreClothing(player, claim.Clothing, claim.Items);

                if (restoredItems > 0 || restoredClothing > 0)
                {
                    claimCount++;
                    totalItems += restoredItems;
                    totalClothing += restoredClothing;
                }

                if (claim.IsEmpty)
                {
                    claimsToRemove.Add(claim);
                }
            }

            if (claimsToRemove.Count > 0)
            {
                _storage.RemoveRange(claimsToRemove);
            }

            // Force save if any claims were partially consumed
            if (claimCount > 0 && claimsToRemove.Count < claimCount)
            {
                _storage.ForceSave();
            }

            return claimCount > 0;
        }

        private static void EnsureClaimLists(ClaimRecord claim)
        {
            if (claim == null)
            {
                return;
            }

            if (claim.Items == null)
            {
                claim.Items = new List<ClaimItem>();
            }

            if (claim.Clothing == null)
            {
                claim.Clothing = new List<ClaimClothing>();
            }
        }

        public List<(ClaimRecord claim, int itemCount, int clothingCount)> GetClaimsSummary(ulong steamId)
        {
            var claims = _storage.GetBySteamId(steamId);
            return claims.Select(c => (c, c.Items?.Count ?? 0, c.Clothing?.Count ?? 0)).ToList();
        }

        public void CleanupExpired()
        {
            var settings = _settingsProvider();
            var expired = _storage.GetExpired();

            if (expired.Count == 0)
            {
                return;
            }

            switch (settings.ExpirationBehavior)
            {
                case ExpirationBehavior.Delete:
                    _storage.RemoveRange(expired);
                    LoggingHelper.LogInfo($"Deleted {expired.Count} expired claims.");
                    break;
                case ExpirationBehavior.DropAtDeathPosition:
                    foreach (var claim in expired)
                    {
                        var pos = new Vector3(claim.DeathX, claim.DeathY, claim.DeathZ);
                        DropClaimToGround(claim, pos);
                    }
                    _storage.RemoveRange(expired);
                    LoggingHelper.LogInfo($"Dropped {expired.Count} expired claims at death positions.");
                    break;
            }
        }

        private int RestoreItems(UnturnedPlayer player, List<ClaimItem> items)
        {
            if (items == null || items.Count == 0)
            {
                return 0;
            }

            var inventory = player.Player?.inventory;
            if (inventory == null)
            {
                return 0;
            }

            var restored = 0;
            foreach (var claimItem in items)
            {
                var item = new Item(claimItem.ItemId, claimItem.Amount, claimItem.Quality, claimItem.State ?? Array.Empty<byte>());
                if (inventory.tryAddItem(item, true))
                {
                    restored++;
                }
            }

            return restored;
        }

        /// <summary>
        /// Restores items and removes successfully restored ones from the list.
        /// Items that fail to restore (e.g. inventory full) remain in the list.
        /// </summary>
        private int RestoreItemsAndPrune(UnturnedPlayer player, List<ClaimItem> items)
        {
            if (items == null || items.Count == 0)
            {
                return 0;
            }

            var inventory = player.Player?.inventory;
            if (inventory == null)
            {
                return 0;
            }

            var restored = 0;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                var claimItem = items[i];
                if (claimItem == null || claimItem.ItemId == 0)
                {
                    items.RemoveAt(i);
                    continue;
                }

                var item = new Item(claimItem.ItemId, claimItem.Amount, claimItem.Quality, claimItem.State ?? Array.Empty<byte>());
                if (inventory.tryAddItem(item, true))
                {
                    items.RemoveAt(i);
                    restored++;
                }
            }

            return restored;
        }

        /// <summary>
        /// Restores clothing and removes successfully restored clothing records from the claim.
        /// If clothing contents cannot fit back into the clothing container, they are preserved as
        /// normal claim items so a later /mid claim can still recover them.
        /// </summary>
        private int RestoreClothing(UnturnedPlayer player, List<ClaimClothing> clothing, List<ClaimItem> fallbackItems)
        {
            if (clothing == null || clothing.Count == 0)
            {
                return 0;
            }

            var playerClothing = player.Player?.clothing;
            if (playerClothing == null)
            {
                return 0;
            }

            var restored = 0;
            for (int i = clothing.Count - 1; i >= 0; i--)
            {
                var claimClothing = clothing[i];
                if (claimClothing == null || claimClothing.ItemId == 0)
                {
                    clothing.RemoveAt(i);
                    continue;
                }

                var state = claimClothing.State ?? Array.Empty<byte>();
                var equipped = TryWearClothing(playerClothing, claimClothing.Slot, claimClothing.ItemId, claimClothing.Quality, state);
                if (equipped)
                {
                    restored++;

                    // Restore clothing contents
                    if (claimClothing.Contents != null && claimClothing.Contents.Count > 0)
                    {
                        var container = PlayerExtensions.GetClothingContainer(player.Player, claimClothing.Slot);
                        if (container != null)
                        {
                            foreach (var contentItem in claimClothing.Contents)
                            {
                                var item = new Item(contentItem.ItemId, contentItem.Amount, contentItem.Quality, contentItem.State ?? Array.Empty<byte>());
                                if (!container.tryAddItem(item, true))
                                {
                                    PreserveFailedClothingContent(fallbackItems, contentItem);
                                }
                            }
                        }
                        else
                        {
                            foreach (var contentItem in claimClothing.Contents)
                            {
                                PreserveFailedClothingContent(fallbackItems, contentItem);
                            }
                        }
                    }

                    clothing.RemoveAt(i);
                }
            }

            return restored;
        }

        private static void PreserveFailedClothingContent(List<ClaimItem> fallbackItems, ClaimItem contentItem)
        {
            if (fallbackItems == null || contentItem == null || contentItem.ItemId == 0)
            {
                return;
            }

            fallbackItems.Add(new ClaimItem
            {
                ItemId = contentItem.ItemId,
                Amount = contentItem.Amount,
                Quality = contentItem.Quality,
                State = contentItem.State != null ? (byte[])contentItem.State.Clone() : Array.Empty<byte>()
            });
        }

        private bool TryWearClothing(PlayerClothing clothing, SlotType slot, ushort itemId, byte quality, byte[] state)
        {
            return ClothingOperationHelper.TryWearClothing(clothing, slot, itemId, quality, state);
        }

        private void DropClaimToGround(ClaimRecord claim, Vector3 position)
        {
            if (claim == null)
            {
                return;
            }

            var dropPosition = position + Vector3.up * 0.5f;

            if (claim.Items != null)
            {
                foreach (var claimItem in claim.Items)
                {
                    var item = new Item(claimItem.ItemId, claimItem.Amount, claimItem.Quality, claimItem.State ?? Array.Empty<byte>());
                    ItemManager.dropItem(item, dropPosition, false, true, true);
                }
            }

            if (claim.Clothing != null)
            {
                foreach (var claimClothing in claim.Clothing)
                {
                    if (claimClothing == null || claimClothing.ItemId == 0)
                    {
                        continue;
                    }

                    var item = new Item(claimClothing.ItemId, 1, claimClothing.Quality, claimClothing.State ?? Array.Empty<byte>());
                    ItemManager.dropItem(item, dropPosition, false, true, true);

                    if (claimClothing.Contents == null)
                    {
                        continue;
                    }

                    foreach (var contentItem in claimClothing.Contents)
                    {
                        if (contentItem == null || contentItem.ItemId == 0)
                        {
                            continue;
                        }

                        var content = new Item(contentItem.ItemId, contentItem.Amount, contentItem.Quality, contentItem.State ?? Array.Empty<byte>());
                        ItemManager.dropItem(content, dropPosition, false, true, true);
                    }
                }
            }
        }
    }
}
