using System;
using System.Collections.Generic;
using FFEmqo.ModifiedItemDrop.Claim;
using FFEmqo.ModifiedItemDrop.Configuration;
using FFEmqo.ModifiedItemDrop.Domain;
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
        [ThreadStatic] private static System.Random _random;
        private static System.Random GetRandom() => _random ?? (_random = new System.Random(Environment.TickCount ^ System.Threading.Thread.CurrentThread.ManagedThreadId));
        private readonly Dictionary<CSteamID, PendingRestore> _pendingRestores = new Dictionary<CSteamID, PendingRestore>();
        private readonly object _pendingRestoresLock = new object();

        private volatile InventoryProcessor _inventoryProcessor;
        private volatile ClothingProcessor _clothingProcessor;
        private volatile RestoreManager _restoreManager;
        private ClaimService _claimService;
        private IDurableClaimCreator _v2ClaimCreator;
        private V2ClaimRecoveryService _v2ClaimRecoveryService;

        public DropService(ConfigurationLoader configurationLoader)
        {
            _configurationLoader = configurationLoader ?? throw new ArgumentNullException(nameof(configurationLoader));
            _chanceResolver = new ChanceResolver(configurationLoader.CurrentRuleSet);

            InitializeProcessors();
        }

        private void InitializeProcessors()
        {
            _inventoryProcessor = new InventoryProcessor(_chanceResolver, _configurationLoader, GetRandom);
            _clothingProcessor = new ClothingProcessor(_configurationLoader, GetRandom, _inventoryProcessor);
            _restoreManager = new RestoreManager(_inventoryProcessor, _clothingProcessor, _claimService, _v2ClaimCreator, _v2ClaimRecoveryService, _configurationLoader);
        }

        public void SetClaimService(ClaimService claimService)
        {
            _claimService = claimService;
            // Reinitialize RestoreManager with the new claim service
            _restoreManager = new RestoreManager(_inventoryProcessor, _clothingProcessor, _claimService, _v2ClaimCreator, _v2ClaimRecoveryService, _configurationLoader);
        }

        public void SetV2DurableClaimCreator(IDurableClaimCreator claimCreator)
        {
            _v2ClaimCreator = claimCreator;
            _restoreManager = new RestoreManager(_inventoryProcessor, _clothingProcessor, _claimService, _v2ClaimCreator, _v2ClaimRecoveryService, _configurationLoader);
        }

        public void SetV2ClaimRecoveryService(V2ClaimRecoveryService claimRecoveryService)
        {
            _v2ClaimRecoveryService = claimRecoveryService;
            _restoreManager = new RestoreManager(_inventoryProcessor, _clothingProcessor, _claimService, _v2ClaimCreator, _v2ClaimRecoveryService, _configurationLoader);
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
            var pending = new PendingRestore(deathPosition);

            try
            {
                // Capture local references to avoid stale processors during concurrent RefreshRules
                var invProc = _inventoryProcessor;
                var clothProc = _clothingProcessor;
                var restMgr = _restoreManager;

                ForceUnequipCurrentItem(player);
                invProc.ProcessInventory(player, pending, deathPosition);
                clothProc.ProcessClothing(player, pending, deathPosition);

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
                _pendingRestores.TryGetValue(player.CSteamID, out pending);
            }

            if (pending != null)
            {
                _restoreManager.RestorePendingItems(player, pending);
                lock (_pendingRestoresLock)
                {
                    _pendingRestores.Remove(player.CSteamID);
                }
            }
            else
            {
                _restoreManager.GiveRespawnItems(player);
            }
        }

        public void HandlePlayerDisconnected(UnturnedPlayer player)
        {
            if (player == null)
            {
                return;
            }

            PendingRestore pending;
            lock (_pendingRestoresLock)
            {
                if (!_pendingRestores.TryGetValue(player.CSteamID, out pending))
                {
                    return;
                }
                _pendingRestores.Remove(player.CSteamID);
            }

            if (!pending.IsEmpty)
            {
                var steamId = (ulong)player.CSteamID;
                var saved = _restoreManager.SavePendingToClaimOrDrop(steamId, pending);
                DebugLog(saved
                    ? $"Player {player.CharacterName} disconnected, saved {pending.InventoryItems.Count} items and {pending.ClothingItems.Count} clothing to claim storage."
                    : $"Player {player.CharacterName} disconnected, dropped pending items because claim storage is unavailable or disabled.");
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

        /// <summary>
        /// Flushes all pending restores to claim storage. Called during plugin unload.
        /// </summary>
        public void FlushPendingRestores()
        {
            List<KeyValuePair<CSteamID, PendingRestore>> snapshot;
            lock (_pendingRestoresLock)
            {
                if (_pendingRestores.Count == 0)
                {
                    return;
                }
                snapshot = new List<KeyValuePair<CSteamID, PendingRestore>>(_pendingRestores);
                _pendingRestores.Clear();
            }

            foreach (var kvp in snapshot)
            {
                var pending = kvp.Value;
                if (!pending.IsEmpty)
                {
                    _restoreManager.SavePendingToClaimOrDrop((ulong)kvp.Key, pending);
                }
            }

            DebugLog($"Flushed {snapshot.Count} pending restores to claim storage on shutdown.");
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
                    var width = ClampHandsSlotDimension(matchedConfig.Width);
                    var height = ClampHandsSlotDimension(matchedConfig.Height);
                    handsPage.resize(width, height);
                    player.Player.inventory.save();
                    DebugLog($"Applied hands slot size {width}x{height} for player {player.CharacterName} (permission: {matchedConfig.PermissionName})");
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

        private static byte ClampHandsSlotDimension(byte value)
        {
            if (value < 1)
            {
                return 1;
            }

            if (value > 12)
            {
                return 12;
            }

            return value;
        }

    }
}
