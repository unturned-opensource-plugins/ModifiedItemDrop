using System;
using System.Collections.Generic;
using System.Linq;
using FFEmqo.ModifiedItemDrop.Claim;
using FFEmqo.ModifiedItemDrop.Configuration;
using FFEmqo.ModifiedItemDrop.Domain;
using FFEmqo.ModifiedItemDrop.Extensions;
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
        private readonly V2DeathProcessingAdapter _v2DeathProcessingAdapter = new V2DeathProcessingAdapter();
        private readonly V2QuickSlotExecutionAdapter _v2QuickSlotExecutionAdapter = new V2QuickSlotExecutionAdapter();
        private readonly V2ClothingExecutionAdapter _v2ClothingExecutionAdapter = new V2ClothingExecutionAdapter();
        private readonly DeathSessionRespawnGrantPlanner _v2RespawnGrantPlanner = new DeathSessionRespawnGrantPlanner();
        private DeathSessionFinalizer _v2DeathSessionFinalizer;
        [ThreadStatic] private static System.Random _random;
        private static System.Random GetRandom() => _random ?? (_random = new System.Random(Environment.TickCount ^ System.Threading.Thread.CurrentThread.ManagedThreadId));
        private readonly Dictionary<CSteamID, PendingRestore> _pendingRestores = new Dictionary<CSteamID, PendingRestore>();
        private readonly Dictionary<CSteamID, DeathSession> _deathSessions = new Dictionary<CSteamID, DeathSession>();
        private readonly object _pendingRestoresLock = new object();

        private volatile InventoryProcessor _inventoryProcessor;
        private volatile ClothingProcessor _clothingProcessor;
        private volatile RestoreManager _restoreManager;
        private ClaimService _claimService;
        private IDurableClaimCreator _v2ClaimCreator;
        private V2ClaimRecoveryService _v2ClaimRecoveryService;
        private bool _claimStorageDeathProcessingEnabled = true;
        private string _claimStorageDisabledReason = string.Empty;

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
            _v2DeathSessionFinalizer = claimCreator != null ? new DeathSessionFinalizer(claimCreator) : null;
            _restoreManager = new RestoreManager(_inventoryProcessor, _clothingProcessor, _claimService, _v2ClaimCreator, _v2ClaimRecoveryService, _configurationLoader);
        }

        public void SetV2ClaimRecoveryService(V2ClaimRecoveryService claimRecoveryService)
        {
            _v2ClaimRecoveryService = claimRecoveryService;
            _restoreManager = new RestoreManager(_inventoryProcessor, _clothingProcessor, _claimService, _v2ClaimCreator, _v2ClaimRecoveryService, _configurationLoader);
        }

        public void SetClaimStorageHealth(bool deathProcessingEnabled, string disabledReason)
        {
            _claimStorageDeathProcessingEnabled = deathProcessingEnabled;
            _claimStorageDisabledReason = disabledReason ?? string.Empty;
        }

        public bool IsClaimStorageDeathProcessingEnabled => _claimStorageDeathProcessingEnabled;

        public string ClaimStorageDisabledReason => _claimStorageDisabledReason;

        public bool IsV2ClaimRecoveryEnabled => _v2ClaimRecoveryService == null || _v2ClaimRecoveryService.RecoveryEnabled;

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

            if (!_configurationLoader.IsDeathProcessingEnabled)
            {
                LoggingHelper.LogWarning("Death processing skipped in safe mode for " + player.CSteamID + ": " + _configurationLoader.SafeModeReason);
                return;
            }

            if (!_claimStorageDeathProcessingEnabled)
            {
                LoggingHelper.LogWarning("Death processing skipped because Claim storage is degraded for " + player.CSteamID + ": " + _claimStorageDisabledReason);
                return;
            }

            var deathPosition = player.Position;
            var pending = new PendingRestore(deathPosition);

            try
            {
                ForceUnequipCurrentItem(player);
                var quickSlotSnapshots = player.CaptureInventory()
                    .Where(snapshot => snapshot.Page <= 2)
                    .ToList();
                var clothingSnapshots = player.CaptureClothing();
                if (quickSlotSnapshots.Count > 0 || clothingSnapshots.Count > 0 || HasV2RespawnGrantRules())
                {
                    var deathResult = _v2DeathProcessingAdapter.ProcessDeath(
                        Guid.NewGuid().ToString("N"),
                        (ulong)player.CSteamID,
                        quickSlotSnapshots,
                        clothingSnapshots,
                        _configurationLoader.CurrentOutcomeRules);
                    ExecuteV2QuickSlotPlan(
                        player.Player.inventory,
                        quickSlotSnapshots,
                        deathResult.ExecutionPlan,
                        pending,
                        deathPosition);
                    ExecuteV2ClothingPlan(
                        player.Player,
                        clothingSnapshots,
                        deathResult.ExecutionPlan,
                        pending,
                        deathPosition);
                    lock (_pendingRestoresLock)
                    {
                        if (deathResult.HasPendingDeathSession || HasV2RespawnGrantRules())
                        {
                            _deathSessions[player.CSteamID] = deathResult.DeathSession;
                        }
                        else
                        {
                            _deathSessions.Remove(player.CSteamID);
                        }
                    }
                }

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
            DeathSession deathSession = null;
            lock (_pendingRestoresLock)
            {
                _pendingRestores.TryGetValue(player.CSteamID, out pending);
                _deathSessions.TryGetValue(player.CSteamID, out deathSession);
            }

            if (pending != null)
            {
                _restoreManager.RestorePendingItems(player, pending);
                GiveV2RespawnGrants(player, deathSession);
                lock (_pendingRestoresLock)
                {
                    _pendingRestores.Remove(player.CSteamID);
                    _deathSessions.Remove(player.CSteamID);
                }
            }
            else
            {
                GiveV2RespawnGrants(player, deathSession);
                lock (_pendingRestoresLock)
                {
                    _deathSessions.Remove(player.CSteamID);
                }
            }
        }

        private void GiveV2RespawnGrants(UnturnedPlayer player, DeathSession deathSession)
        {
            if (player?.Player?.inventory == null || deathSession == null)
            {
                return;
            }

            var result = _v2RespawnGrantPlanner.PlanAfterDeathRespawn(deathSession, _configurationLoader.CurrentOutcomeRules);
            if (result.Grants.Count == 0)
            {
                return;
            }

            var failed = new PendingRestore(player.Position);
            foreach (var grant in result.Grants)
            {
                if (grant.ItemId == 0 || grant.Amount == 0)
                {
                    continue;
                }

                var item = new Item(grant.ItemId, grant.Amount, grant.Quality, Array.Empty<byte>());
                if (!player.Player.inventory.tryAddItem(item, true))
                {
                    failed.InventoryItems.Add(new PendingInventoryItem(item, byte.MaxValue));
                }
            }

            if (!failed.IsEmpty)
            {
                var saved = _restoreManager.SavePendingToClaimOrDrop((ulong)player.CSteamID, failed);
                UtilityHelper.TryNotify(player, saved
                    ? $"有 {failed.InventoryItems.Count} 个复活 Grant 空间不足，已存入待领取。"
                    : $"{failed.InventoryItems.Count} 个复活 Grant 空间不足且 Claim 不可用，已掉落在当前位置。");
            }
        }

        private bool HasV2RespawnGrantRules()
        {
            return _configurationLoader.CurrentOutcomeRules.Any(rule => rule.TriggerKind == OutcomeRuleTriggerKind.AfterDeathRespawn);
        }

        public void HandlePlayerDisconnected(UnturnedPlayer player)
        {
            if (player == null)
            {
                return;
            }

            PendingRestore pending;
            DeathSession deathSession;
            lock (_pendingRestoresLock)
            {
                var hasPending = _pendingRestores.TryGetValue(player.CSteamID, out pending);
                var hasSession = _deathSessions.TryGetValue(player.CSteamID, out deathSession);
                if (!hasPending && !hasSession)
                {
                    return;
                }
                _pendingRestores.Remove(player.CSteamID);
                _deathSessions.Remove(player.CSteamID);
            }

            if (deathSession != null)
            {
                FinalizeDeathSessionOnDisconnect(deathSession, player.Position);
                return;
            }

            if (pending != null && !pending.IsEmpty)
            {
                var steamId = (ulong)player.CSteamID;
                var saved = _restoreManager.SavePendingToClaimOrDrop(steamId, pending);
                DebugLog(saved
                    ? $"Player {player.CharacterName} disconnected, saved {pending.InventoryItems.Count} items and {pending.ClothingItems.Count} clothing to claim storage."
                    : $"Player {player.CharacterName} disconnected, dropped pending items because claim storage is unavailable or disabled.");
            }
        }

        private void FinalizeDeathSessionOnDisconnect(DeathSession deathSession, Vector3 fallbackPosition)
        {
            if (deathSession == null || _v2DeathSessionFinalizer == null)
            {
                return;
            }

            var result = _v2DeathSessionFinalizer.FinalizeDisconnect(deathSession);
            ExecuteFallbackDecisions(result, fallbackPosition);
        }

        private void FinalizeDeathSessionOnPluginUnload(DeathSession deathSession)
        {
            if (deathSession == null || _v2DeathSessionFinalizer == null)
            {
                return;
            }

            var result = _v2DeathSessionFinalizer.FinalizePluginUnload(deathSession);
            ExecuteFallbackDecisions(result, Vector3.zero);
        }

        private static void ExecuteFallbackDecisions(DeathSessionFinalizationResult result, Vector3 fallbackPosition)
        {
            if (result?.FallbackDecisions == null)
            {
                return;
            }

            foreach (var decision in result.FallbackDecisions)
            {
                if (decision.Kind != DurableClaimFallbackKind.DropFallback || decision.Asset == null || decision.Asset.ItemId == 0)
                {
                    continue;
                }

                var item = new Item(decision.Asset.ItemId, decision.Asset.Amount, decision.Asset.Quality, decision.Asset.State ?? Array.Empty<byte>());
                UtilityHelper.DropWorldItem(item, fallbackPosition);
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
            List<DeathSession> sessionSnapshot;
            lock (_pendingRestoresLock)
            {
                if (_pendingRestores.Count == 0 && _deathSessions.Count == 0)
                {
                    return;
                }
                snapshot = new List<KeyValuePair<CSteamID, PendingRestore>>(_pendingRestores);
                sessionSnapshot = new List<DeathSession>(_deathSessions.Values);
                _pendingRestores.Clear();
                _deathSessions.Clear();
            }

            foreach (var kvp in snapshot)
            {
                if (sessionSnapshot.Any(session => session.SteamId == (ulong)kvp.Key))
                {
                    continue;
                }

                var pending = kvp.Value;
                if (!pending.IsEmpty)
                {
                    _restoreManager.SavePendingToClaimOrDrop((ulong)kvp.Key, pending);
                }
            }

            foreach (var deathSession in sessionSnapshot)
            {
                FinalizeDeathSessionOnPluginUnload(deathSession);
            }

            DebugLog($"Flushed {snapshot.Count} pending restores and {sessionSnapshot.Count} death sessions to claim storage on shutdown.");
        }

        private void DebugLog(string message)
        {
            LoggingHelper.LogDebug(message, _configurationLoader.IsDebugLoggingEnabled);
        }

        private void ExecuteV2QuickSlotPlan(
            PlayerInventory inventory,
            IEnumerable<InventoryItemSnapshot> snapshots,
            DeathOutcomeExecutionPlan executionPlan,
            PendingRestore pending,
            Vector3 deathPosition)
        {
            _v2QuickSlotExecutionAdapter.Execute(inventory, snapshots, executionPlan, pending, deathPosition);
        }

        private void ExecuteV2ClothingPlan(
            Player player,
            IEnumerable<ClothingItemSnapshot> snapshots,
            DeathOutcomeExecutionPlan executionPlan,
            PendingRestore pending,
            Vector3 deathPosition)
        {
            _v2ClothingExecutionAdapter.Execute(player, snapshots, executionPlan, pending, deathPosition);
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
