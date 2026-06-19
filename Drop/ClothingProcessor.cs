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
    /// Handles clothing processing logic during player death.
    /// Responsible for determining which clothing items drop and which are kept.
    /// </summary>
    public sealed class ClothingProcessor
    {
        private const byte AnyInventoryPage = byte.MaxValue;
        private readonly ConfigurationLoader _configurationLoader;
        private readonly Func<System.Random> _randomProvider;
        private readonly InventoryProcessor _inventoryProcessor;

        public ClothingProcessor(ConfigurationLoader configurationLoader, Func<System.Random> randomProvider, InventoryProcessor inventoryProcessor)
        {
            _configurationLoader = configurationLoader ?? throw new ArgumentNullException(nameof(configurationLoader));
            _randomProvider = randomProvider ?? throw new ArgumentNullException(nameof(randomProvider));
            _inventoryProcessor = inventoryProcessor ?? throw new ArgumentNullException(nameof(inventoryProcessor));
        }

        /// <summary>
        /// Processes clothing items on player death, determining which clothing drops and which is kept.
        /// </summary>
        public void ProcessClothing(UnturnedPlayer player, PendingRestore pending, Vector3 deathPosition)
        {
            var snapshots = player.CaptureClothing();
            if (snapshots.Count == 0)
            {
                return;
            }

            var clothing = player.Player.clothing;
            foreach (var snapshot in snapshots)
            {
                var rule = _configurationLoader.CurrentRuleSet.ResolveClothingRule(snapshot.SlotType);
                var chance = rule.SlotDropChance;
                var roll = _randomProvider().NextDouble();
                var shouldDrop = roll <= chance;

                var container = PlayerExtensions.GetClothingContainer(clothing, snapshot.SlotType);
                _inventoryProcessor.HandleClothingContents(snapshot, rule, pending, deathPosition, shouldDrop, chance, container);

                ClothingOperationHelper.ClearClothingSlot(clothing, snapshot.SlotType);

                if (shouldDrop)
                {
                    UtilityHelper.DropWorldItem(snapshot.Item, deathPosition);
                    DebugLog($"✗ {snapshot.SlotType}: id={snapshot.Item.id} ({roll:P0} ≤ {chance:P0})");
                }
                else
                {
                    pending.ClothingItems.Add(BuildKeptClothingSnapshot(snapshot, pending));
                    DebugLog($"✓ {snapshot.SlotType}: id={snapshot.Item.id} ({roll:P0} > {chance:P0})");
                }
            }
        }

        /// <summary>
        /// Restores clothing items to the player.
        /// </summary>
        public bool RestoreClothing(UnturnedPlayer player, PendingRestore pending)
        {
            var clothing = player.Player?.clothing;
            if (clothing == null)
            {
                return false;
            }

            var restored = 0;
            for (int i = pending.ClothingItems.Count - 1; i >= 0; i--)
            {
                var snap = pending.ClothingItems[i];
                if (snap?.Item == null || snap.Item.id == 0)
                {
                    pending.ClothingItems.RemoveAt(i);
                    continue;
                }

                var state = snap.Item.state ?? Array.Empty<byte>();
                var stateStr = state.Length > 0 ? $"state[{state.Length}]" : "state[]";
                if (!ClothingOperationHelper.TryWearClothing(clothing, snap.SlotType, snap.Item.id, snap.Item.quality, state))
                {
                    DebugLog($"Deferred clothing {snap.SlotType}: id={snap.Item.id} because slot is occupied.");
                    continue;
                }

                restored++;
                DebugLog($"Restored clothing {snap.SlotType}: id={snap.Item.id} {stateStr}");

                // Restore contents to the clothing slot if any
                if (pending.ClothingContentsToRestore.TryGetValue(snap.SlotType, out var contents))
                {
                    var container = PlayerExtensions.GetClothingContainer(clothing, snap.SlotType);
                    if (container != null)
                    {
                        foreach (var item in contents)
                        {
                            if (item == null || item.id == 0)
                            {
                                continue;
                            }

                            var clone = UtilityHelper.CloneItem(item);
                            if (!container.tryAddItem(clone, true))
                            {
                                pending.InventoryItems.Add(new PendingInventoryItem(UtilityHelper.CloneItem(item), AnyInventoryPage));
                            }
                        }
                    }
                    else
                    {
                        foreach (var item in contents)
                        {
                            if (item == null || item.id == 0)
                            {
                                continue;
                            }

                            pending.InventoryItems.Add(new PendingInventoryItem(UtilityHelper.CloneItem(item), AnyInventoryPage));
                        }
                    }

                    pending.ClothingContentsToRestore.Remove(snap.SlotType);
                }

                pending.ClothingItems.RemoveAt(i);
            }

            if (restored > 0)
            {
                DebugLog($"Restored {restored} clothing items.");
            }

            return restored > 0;
        }

        private static ClothingItemSnapshot BuildKeptClothingSnapshot(ClothingItemSnapshot original, PendingRestore pending)
        {
            var keptContents = new List<ClothingContentSnapshot>();
            if (pending.ClothingContentsToRestore.TryGetValue(original.SlotType, out var contents))
            {
                for (var i = 0; i < contents.Count; i++)
                {
                    keptContents.Add(new ClothingContentSnapshot((byte)i, UtilityHelper.CloneItem(contents[i])));
                }
            }

            return new ClothingItemSnapshot(original.SlotType, UtilityHelper.CloneItem(original.Item), keptContents);
        }

        private void DebugLog(string message)
        {
            LoggingHelper.LogDebug(message, _configurationLoader.IsDebugLoggingEnabled);
        }
    }
}
