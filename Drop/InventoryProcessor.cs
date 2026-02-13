using System;
using System.Collections.Generic;
using FFEmqo.ModifiedItemDrop.Configuration;
using FFEmqo.ModifiedItemDrop.Extensions;
using FFEmqo.ModifiedItemDrop.Models;
using FFEmqo.ModifiedItemDrop.Utilities;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;

namespace FFEmqo.ModifiedItemDrop.Drop
{
    /// <summary>
    /// Handles inventory processing logic during player death.
    /// Responsible for determining which items drop and which are kept.
    /// </summary>
    public sealed class InventoryProcessor
    {
        private readonly ChanceResolver _chanceResolver;
        private readonly ConfigurationLoader _configurationLoader;
        private readonly Func<System.Random> _randomProvider;

        public InventoryProcessor(ChanceResolver chanceResolver, ConfigurationLoader configurationLoader, Func<System.Random> randomProvider)
        {
            _chanceResolver = chanceResolver ?? throw new ArgumentNullException(nameof(chanceResolver));
            _configurationLoader = configurationLoader ?? throw new ArgumentNullException(nameof(configurationLoader));
            _randomProvider = randomProvider ?? throw new ArgumentNullException(nameof(randomProvider));
        }

        /// <summary>
        /// Processes inventory items on player death, determining which items drop and which are kept.
        /// </summary>
        public void ProcessInventory(UnturnedPlayer player, PendingRestore pending, Vector3 deathPosition)
        {
            var inventory = player.Player.inventory;
            if (inventory == null)
            {
                return;
            }

            var snapshots = player.CaptureInventory();
            if (snapshots.Count == 0)
            {
                return;
            }

            // Process per page in reverse order to keep indexes valid.
            // Skip pages 3-6 (clothing storage) because they are handled by HandleClothingContents.
            var groupedByPage = new Dictionary<byte, List<InventoryItemSnapshot>>();

            foreach (var snapshot in snapshots)
            {
                // Skip clothing storage pages (3=backpack, 4=vest, 5=shirt, 6=pants).
                if (snapshot.Page >= 3 && snapshot.Page <= 6)
                {
                    continue;
                }

                if (!groupedByPage.TryGetValue(snapshot.Page, out var list))
                {
                    list = new List<InventoryItemSnapshot>();
                    groupedByPage[snapshot.Page] = list;
                }

                list.Add(snapshot);
            }

            foreach (var pair in groupedByPage)
            {
                var page = pair.Key;
                var itemsInPage = pair.Value;
                itemsInPage.Sort((a, b) => b.Index.CompareTo(a.Index));

                foreach (var snapshot in itemsInPage)
                {
                    var jar = snapshot.Jar;
                    if (jar?.item == null || jar.item.id == 0)
                    {
                        continue;
                    }

                    // Check if item should be deleted on death.
                    var deleteList = _configurationLoader.DeathSettings?.DeleteOnDeathItems;
                    if (deleteList != null && deleteList.Contains(jar.item.id))
                    {
                        inventory.removeItem(page, snapshot.Index);
                        DebugLog($"[Delete] id={jar.item.id} (DeleteOnDeath)");
                        continue;
                    }

                    var slotType = UtilityHelper.GetSlotTypeForPage(page);
                    var chance = _chanceResolver.GetChance(slotType, jar.item.id, out var source);
                    var roll = _randomProvider().NextDouble();
                    var shouldDrop = roll <= chance;

                    inventory.removeItem(page, snapshot.Index);

                    if (shouldDrop)
                    {
                        UtilityHelper.DropWorldItem(jar.item, deathPosition);
                        var stateStr = jar.item.state != null ? $"state[{jar.item.state.Length}]" : "state[]";
                        DebugLog($"[Drop][{slotType}] id={jar.item.id} ({source}) {roll:P0} <= {chance:P0} {stateStr}");
                    }
                    else
                    {
                        var cloned = UtilityHelper.CloneItem(jar.item);
                        var stateStr = cloned.state != null ? $"state[{cloned.state.Length}]" : "state[]";
                        pending.InventoryItems.Add(new PendingInventoryItem(cloned, page));
                        DebugLog($"[Keep][{slotType}] id={jar.item.id} ({source}) {roll:P0} > {chance:P0} {stateStr}");
                    }
                }
            }
        }

        /// <summary>
        /// Handles clothing contents based on clothing slot rules.
        /// </summary>
        public void HandleClothingContents(ClothingItemSnapshot snapshot, ClothingSlotRule rule, PendingRestore pending, Vector3 deathPosition, bool clothingWillDrop, double slotChance, Items container)
        {
            if (snapshot.Contents == null || snapshot.Contents.Count == 0)
            {
                return;
            }

            var contents = snapshot.Contents;
            if (contents.Count > 1)
            {
                contents.Sort((a, b) => b.Index.CompareTo(a.Index));
            }

            var droppedCount = 0;
            var keptCount = 0;

            foreach (var content in contents)
            {
                var item = content.Item;
                if (item == null || item.id == 0)
                {
                    continue;
                }

                // Check if item should be deleted on death.
                var deleteList = _configurationLoader.DeathSettings?.DeleteOnDeathItems;
                if (deleteList != null && deleteList.Contains(item.id))
                {
                    container?.removeItem(content.Index);
                    DebugClothingLog($"    [Delete] id={item.id} (DeleteOnDeath)");
                    continue;
                }

                var effectiveChance = rule.ContentsDropChance;
                var roll = _randomProvider().NextDouble();
                var shouldDrop = effectiveChance > 0 && roll <= effectiveChance;

                container?.removeItem(content.Index);

                // If clothing drops, its contents must drop too.
                if (clothingWillDrop || shouldDrop)
                {
                    UtilityHelper.DropWorldItem(item, deathPosition);
                    droppedCount++;
                    if (clothingWillDrop)
                    {
                        DebugClothingLog($"    [Drop] id={item.id} (clothing dropped)");
                    }
                    else
                    {
                        DebugClothingLog($"    [Drop] id={item.id} ({roll:P0} <= {effectiveChance:P0})");
                    }

                    continue;
                }

                // Save to clothing contents restore map so it can be restored to the correct slot.
                if (!pending.ClothingContentsToRestore.TryGetValue(snapshot.SlotType, out var contentsList))
                {
                    contentsList = new List<Item>();
                    pending.ClothingContentsToRestore[snapshot.SlotType] = contentsList;
                }

                contentsList.Add(UtilityHelper.CloneItem(item));
                keptCount++;
                DebugClothingLog($"    [Keep] id={item.id} ({roll:P0} > {effectiveChance:P0})");
            }

            if (droppedCount > 0 || keptCount > 0)
            {
                DebugClothingLog($"  {snapshot.SlotType} contents: keep={keptCount} drop={droppedCount}");
            }
        }

        /// <summary>
        /// Restores inventory items to the player.
        /// </summary>
        public bool RestoreInventory(UnturnedPlayer player, PendingRestore pending)
        {
            var inventory = player.Player?.inventory;
            if (inventory == null)
            {
                return false;
            }

            var restored = 0;
            var failed = 0;

            for (int i = pending.InventoryItems.Count - 1; i >= 0; i--)
            {
                var pendingItem = pending.InventoryItems[i];
                var item = pendingItem?.Item;
                if (item == null || item.id == 0)
                {
                    pending.InventoryItems.RemoveAt(i);
                    continue;
                }

                var stateStr = item.state != null ? $"state[{item.state.Length}]" : "state[]";

                // Restore to original page first (especially primary/secondary slots),
                // then fall back to default auto-placement.
                var ok = TryAddToPreferredPage(inventory, pendingItem);
                if (!ok)
                {
                    var fallbackClone = UtilityHelper.CloneItem(item);
                    ok = inventory.tryAddItem(fallbackClone, true);
                }

                if (ok)
                {
                    pending.InventoryItems.RemoveAt(i);
                    restored++;
                    DebugLog($"Restored inventory item: id={item.id} page={pendingItem.SourcePage} {stateStr}");
                }
                else
                {
                    failed++;
                }
            }

            if (restored > 0)
            {
                DebugLog($"Restored {restored} inventory items.");
            }

            if (failed > 0)
            {
                DebugLog($"Deferred {failed} inventory items due to no space.");
            }

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

            var preferredClone = UtilityHelper.CloneItem(pendingItem.Item);
            return pageContainer.tryAddItem(preferredClone, true);
        }

        private void DebugLog(string message)
        {
            LoggingHelper.LogDebug(message, _configurationLoader.IsDebugLoggingEnabled);
        }

        private void DebugClothingLog(string message)
        {
            LoggingHelper.LogDebugContents(message, _configurationLoader.IsDebugLoggingEnabled, _configurationLoader.IsClothingContentsDebugEnabled);
        }
    }
}

