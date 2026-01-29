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
        private readonly ConfigurationLoader _configurationLoader;
        private readonly System.Random _random;
        private readonly InventoryProcessor _inventoryProcessor;
        private readonly bool _debugLoggingEnabled;

        public ClothingProcessor(ConfigurationLoader configurationLoader, System.Random random, InventoryProcessor inventoryProcessor)
        {
            _configurationLoader = configurationLoader ?? throw new ArgumentNullException(nameof(configurationLoader));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _inventoryProcessor = inventoryProcessor ?? throw new ArgumentNullException(nameof(inventoryProcessor));
            _debugLoggingEnabled = configurationLoader.IsDebugLoggingEnabled;
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
                var roll = _random.NextDouble();
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
                    pending.ClothingItems.Add(snapshot);
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
            var listCopy = new List<ClothingItemSnapshot>(pending.ClothingItems);
            foreach (var snap in listCopy)
            {
                var state = snap.Item.state ?? Array.Empty<byte>();
                var stateStr = state.Length > 0 ? $"state[{state.Length}]" : "state[]";
                ClothingOperationHelper.WearClothingItem(clothing, snap.SlotType, snap.Item.id, snap.Item.quality, state);
                restored++;
                DebugLog($"Restored clothing {snap.SlotType}: id={snap.Item.id} {stateStr}");

                // Restore contents to the clothing slot if any
                if (pending.ClothingContentsToRestore.TryGetValue(snap.SlotType, out var contents))
                {
                    var container = PlayerExtensions.GetClothingContainer(clothing, snap.SlotType);
                    if (container != null)
                    {
                        var contentCount = 0;
                        foreach (var item in contents)
                        {
                            var clone = UtilityHelper.CloneItem(item);
                            container.tryAddItem(clone, true);
                            contentCount++;
                        }
                    }
                    pending.ClothingContentsToRestore.Remove(snap.SlotType);
                }
            }

            if (restored > 0)
            {
                pending.ClothingItems.Clear();
                DebugLog($"Restored {restored} clothing items.");
            }

            return restored > 0;
        }

        private void DebugLog(string message)
        {
            LoggingHelper.LogDebug(message, _debugLoggingEnabled);
        }
    }
}
