using System;
using System.Collections.Generic;
using FFEmqo.ModifiedItemDrop.Claim;
using FFEmqo.ModifiedItemDrop.Configuration;
using FFEmqo.ModifiedItemDrop.Models;
using FFEmqo.ModifiedItemDrop.Utilities;
using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace FFEmqo.ModifiedItemDrop.Drop
{
    /// <summary>
    /// Main service for managing item drops on player death.
    /// Orchestrates inventory processing, clothing processing, and item restoration.
    /// </summary>
    public sealed class DropService
    {
        private readonly ConfigurationLoader _configurationLoader;
        private readonly ChanceResolver _chanceResolver;
        private readonly System.Random _random;
        private readonly Dictionary<CSteamID, PendingRestore> _pendingRestores = new Dictionary<CSteamID, PendingRestore>();
        private readonly object _pendingRestoresLock = new object();

        private InventoryProcessor _inventoryProcessor;
        private ClothingProcessor _clothingProcessor;
        private RestoreManager _restoreManager;
        private ClaimService _claimService;

        public DropService(ConfigurationLoader configurationLoader)
        {
            _configurationLoader = configurationLoader ?? throw new ArgumentNullException(nameof(configurationLoader));
            _chanceResolver = new ChanceResolver(configurationLoader.CurrentRuleSet);
            _random = new System.Random();

            InitializeProcessors();
        }

        private void InitializeProcessors()
        {
            _inventoryProcessor = new InventoryProcessor(_chanceResolver, _configurationLoader, _random);
            _clothingProcessor = new ClothingProcessor(_configurationLoader, _random, _inventoryProcessor);
            _restoreManager = new RestoreManager(_inventoryProcessor, _clothingProcessor, _claimService);
        }

        public void SetClaimService(ClaimService claimService)
        {
            _claimService = claimService;
            // Reinitialize RestoreManager with the new claim service
            _restoreManager = new RestoreManager(_inventoryProcessor, _clothingProcessor, _claimService);
        }

        public void RefreshRules()
        {
            _chanceResolver.UpdateRuleSet(_configurationLoader.CurrentRuleSet);
            InitializeProcessors();
        }

        public double PeekChance(SlotType slotType, ushort itemId, out string source)
        {
            return _chanceResolver.GetChance(slotType, itemId, out source);
        }

        public ClothingSlotRule ResolveClothingRule(SlotType slotType)
        {
            return _configurationLoader.CurrentRuleSet.ResolveClothingRule(slotType);
        }

        public void SetRegionOverride(SlotType slotType, double chance)
        {
            _chanceResolver.SetRegionOverride(slotType.ToString(), UtilityHelper.ClampChance(chance));
        }

        public void SetItemOverride(ushort itemId, double chance)
        {
            _chanceResolver.SetItemOverride(itemId, UtilityHelper.ClampChance(chance));
        }

        public bool ClearRegionOverride(SlotType slotType)
        {
            return _chanceResolver.ClearRegionOverride(slotType.ToString());
        }

        public bool ClearItemOverride(ushort itemId)
        {
            return _chanceResolver.ClearItemOverride(itemId);
        }

        public void ClearAllOverrides()
        {
            _chanceResolver.ClearAllOverrides();
        }

        public void ClearAllRegionOverrides()
        {
            _chanceResolver.ClearAllRegionOverrides();
        }

        public void ClearAllItemOverrides()
        {
            _chanceResolver.ClearAllItemOverrides();
        }

        public IReadOnlyDictionary<string, double> RegionOverrides => _chanceResolver.RegionOverrides;

        public IReadOnlyDictionary<ushort, double> ItemOverrides => _chanceResolver.ItemOverrides;

        public void HandlePlayerDying(UnturnedPlayer player)
        {
            if (player == null || player.Player == null)
            {
                return;
            }

            DebugLog($"HandlePlayerDying: player={player.CharacterName} ({player.CSteamID}) position={player.Position}");

            var deathPosition = player.Position;
            var pending = new PendingRestore(player, deathPosition);

            try
            {
                ForceUnequipCurrentItem(player);
                _inventoryProcessor.ProcessInventory(player, pending, deathPosition);
                _clothingProcessor.ProcessClothing(player, pending, deathPosition);

                lock (_pendingRestoresLock)
                {
                    if (pending.IsEmpty)
                    {
                        _pendingRestores.Remove(player.CSteamID);
                    }
                    else
                    {
                        _pendingRestores[player.CSteamID] = pending;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogException(ex, "HandlePlayerDying");
                // In case of failure, ensure we do not lose items by restoring immediately.
                _restoreManager.RestoreImmediately(player, pending);
            }
        }

        public void HandlePlayerRevived(UnturnedPlayer player)
        {
            if (player == null)
            {
                return;
            }

            PendingRestore pending = null;
            lock (_pendingRestoresLock)
            {
                if (!_pendingRestores.TryGetValue(player.CSteamID, out pending))
                {
                    return;
                }
            }

            _restoreManager.RestorePendingItems(player, pending);

            lock (_pendingRestoresLock)
            {
                _pendingRestores.Remove(player.CSteamID);
            }
        }

        public void HandlePlayerDisconnected(UnturnedPlayer player)
        {
            if (player == null)
            {
                return;
            }

            PendingRestore pending = null;
            lock (_pendingRestoresLock)
            {
                if (!_pendingRestores.TryGetValue(player.CSteamID, out pending))
                {
                    return;
                }
            }

            // Save pending items to persistent claim storage instead of forcing restore
            if (_claimService != null && !pending.IsEmpty)
            {
                var steamId = (ulong)player.CSteamID;
                var remainingItems = pending.InventoryItems;
                var remainingClothing = pending.ClothingItems;
                _claimService.AddClaim(steamId, pending.DeathPosition, remainingItems, remainingClothing);
                DebugLog($"Player {player.CharacterName} disconnected, saved {remainingItems.Count} items and {remainingClothing.Count} clothing to claim storage.");
            }
            else
            {
                // Fallback to immediate restore if claim service not available
                _restoreManager.RestoreImmediately(player, pending);
            }

            lock (_pendingRestoresLock)
            {
                _pendingRestores.Remove(player.CSteamID);
            }
        }

        public void ClaimPending(UnturnedPlayer player)
        {
            if (player == null)
            {
                return;
            }

            _restoreManager.ClaimPending(player);
        }

        public void ClaimAllPending(UnturnedPlayer player)
        {
            if (player == null)
            {
                return;
            }

            _restoreManager.ClaimAllPending(player);
        }

        private void DebugLog(string message)
        {
            LoggingHelper.LogDebug(message, _configurationLoader.IsDebugLoggingEnabled);
        }

        /// <summary>
        /// Adjusts the hands slot size based on player permission.
        /// Permission format: ModifiedItemDrop.Hands.{PermissionName}
        /// </summary>
        public void ApplyHandsSlotSize(UnturnedPlayer player)
        {
            if (player?.Player?.inventory == null)
            {
                return;
            }

            var handsSettings = _configurationLoader.HandsSlotSettings;
            if (handsSettings?.Configurations == null || handsSettings.Configurations.Count == 0)
            {
                return;
            }

            // Find matching permission (check in reverse order so later configs have higher priority)
            HandsConfig matchedConfig = null;
            for (int i = handsSettings.Configurations.Count - 1; i >= 0; i--)
            {
                var config = handsSettings.Configurations[i];
                if (config == null || string.IsNullOrEmpty(config.PermissionName))
                {
                    continue;
                }

                var permission = $"ModifiedItemDrop.Hands.{config.PermissionName}";
                if (player.HasPermission(permission))
                {
                    matchedConfig = config;
                    break;
                }
            }

            if (matchedConfig == null)
            {
                return;
            }

            try
            {
                // Page 2 is the hands slot
                var handsPage = player.Player.inventory.items[2];
                if (handsPage != null)
                {
                    handsPage.resize(matchedConfig.Width, matchedConfig.Height);
                    player.Player.inventory.save();
                    DebugLog($"Applied hands slot size {matchedConfig.Width}x{matchedConfig.Height} for player {player.CharacterName} (permission: {matchedConfig.PermissionName})");
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogException(ex, "ApplyHandsSlotSize");
            }
        }

        private static void ForceUnequipCurrentItem(UnturnedPlayer player)
        {
            var equipment = player?.Player?.equipment;
            if (equipment == null)
            {
                return;
            }

            try
            {
                equipment.dequip();
            }
            catch (Exception)
            {
                // Ignore and continue; equipment might already be unequipped.
            }
        }

    }
}
