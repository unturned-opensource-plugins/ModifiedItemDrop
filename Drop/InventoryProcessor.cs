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
        private readonly System.Random _random;

        public InventoryProcessor(ChanceResolver chanceResolver, ConfigurationLoader configurationLoader, System.Random random)
        {
            _chanceResolver = chanceResolver ?? throw new ArgumentNullException(nameof(chanceResolver));
            _configurationLoader = configurationLoader ?? throw new ArgumentNullException(nameof(configurationLoader));
            _random = random ?? throw new ArgumentNullException(nameof(random));
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
            // Skip pages 3-6 (clothing storage) - those are handled by HandleClothingContents
            var groupedByPage = new Dictionary<byte, List<InventoryItemSnapshot>>();

            foreach (var snapshot in snapshots)
            {
                // Skip clothing storage pages (3=backpack, 4=vest, 5=shirt, 6=pants)
                // Page 2 is Hands slot, not clothing storage!
                // These are processed by HandleClothingContents using ClothingRules
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

                    // Check if item should be deleted on death
                    var deleteList = _configurationLoader.DeathSettings?.DeleteOnDeathItems;
                    if (deleteList != null && deleteList.Contains(jar.item.id))
                    {
                        inventory.removeItem(page, snapshot.Index);
                        DebugLog($"✗ [Delete] id={jar.item.id} (DeleteOnDeath)");
                        continue;
                    }

                    var slotType = UtilityHelper.GetSlotTypeForPage(page);
                    var chance = _chanceResolver.GetChance(slotType, jar.item.id, out var source);
                    var roll = _random.NextDouble();
                    var shouldDrop = roll <= chance;

                    inventory.removeItem(page, snapshot.Index);

                    if (shouldDrop)
                    {
                        UtilityHelper.DropWorldItem(jar.item, deathPosition);
                        var stateStr = jar.item.state != null ? $"state[{jar.item.state.Length}]" : "state[]";
                        DebugLog($"✗ [{slotType}] id={jar.item.id} ({source}) {roll:P0} ≤ {chance:P0} {stateStr}");
                    }
                    else
                    {
                        var cloned = UtilityHelper.CloneItem(jar.item);
                        var stateStr = cloned.state != null ? $"state[{cloned.state.Length}]" : "state[]";
                        pending.InventoryItems.Add(cloned);
                        DebugLog($"✓ [{slotType}] id={jar.item.id} ({source}) {roll:P0} > {chance:P0} {stateStr}");
                    }
                }
            }
        }

        /// <summary>
        /// Handles clothing contents based on the clothing slot rules.
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

                // Check if item should be deleted on death
                var deleteList = _configurationLoader.DeathSettings?.DeleteOnDeathItems;
                if (deleteList != null && deleteList.Contains(item.id))
                {
                    container?.removeItem(content.Index);
                    DebugClothingLog($"    ✗ id={item.id} (DeleteOnDeath)");
                    continue;
                }

                var effectiveChance = rule.ContentsDropChance;
                var roll = _random.NextDouble();
                var shouldDrop = effectiveChance > 0 && roll <= effectiveChance;

                container?.removeItem(content.Index);

                // If clothing will drop, contents must also drop (衣服掉了，里面的物品也要掉)
                if (clothingWillDrop || shouldDrop)
                {
                    UtilityHelper.DropWorldItem(item, deathPosition);
                    droppedCount++;
                    if (clothingWillDrop)
                    {
                        DebugClothingLog($"    ✗ id={item.id} (衣物掉落)");
                    }
                    else
                    {
                        DebugClothingLog($"    ✗ id={item.id} ({roll:P0} > {effectiveChance:P0})");
                    }
                    continue;
                }

                // Save to clothing contents restore map so it can be restored to the correct slot
                if (!pending.ClothingContentsToRestore.TryGetValue(snapshot.SlotType, out var contentsList))
                {
                    contentsList = new List<Item>();
                    pending.ClothingContentsToRestore[snapshot.SlotType] = contentsList;
                }
                contentsList.Add(UtilityHelper.CloneItem(item));
                keptCount++;
                DebugClothingLog($"    ✓ id={item.id} ({roll:P0} ≤ {effectiveChance:P0})");
            }

            if (droppedCount > 0 || keptCount > 0)
            {
                DebugClothingLog($"  {snapshot.SlotType} 内容物: ✓{keptCount} ✗{droppedCount}");
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

            // 直接迭代，避免创建副本
            for (int i = pending.InventoryItems.Count - 1; i >= 0; i--)
            {
                var item = pending.InventoryItems[i];
                var clone = UtilityHelper.CloneItem(item);
                var stateStr = clone.state != null ? $"state[{clone.state.Length}]" : "state[]";
                var ok = inventory.tryAddItem(clone, true);
                if (ok)
                {
                    pending.InventoryItems.RemoveAt(i);
                    restored++;
                    DebugLog($"Restored inventory item: id={item.id} {stateStr}");
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
