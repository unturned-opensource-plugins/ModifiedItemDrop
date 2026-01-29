using System;
using FFEmqo.ModifiedItemDrop.Plugin;
using Rocket.Core.Logging;

namespace FFEmqo.ModifiedItemDrop.Configuration
{
    public sealed class ConfigurationLoader
    {
        private DropRuleSet _currentRuleSet;
        private readonly ModifiedItemDropPlugin _plugin;

        public bool IsDebugLoggingEnabled { get; private set; }

        public bool IsClothingContentsDebugEnabled { get; private set; }

        public ConfigurationReloadSummary LastReloadSummary { get; private set; } = ConfigurationReloadSummary.Empty;

        public ConfigurationLoader(ModifiedItemDropPlugin plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            ReloadFromConfiguration();
        }

        public DropRuleSet CurrentRuleSet => _currentRuleSet;

        public HandsSlotSettings HandsSlotSettings => _plugin.Configuration?.Instance?.HandsSlotSettings ?? HandsSlotSettings.CreateDefault();

        public ConfigurationReloadSummary ReloadFromConfiguration()
        {
            var config = _plugin.Configuration?.Instance ?? new ModifiedItemDropConfiguration();
            var rawRuleSet = config.RuleSet ?? DropRuleSet.CreateDefault();
            var normalizedRuleSet = rawRuleSet.NormalizedCopy();

            _currentRuleSet = normalizedRuleSet;
            IsDebugLoggingEnabled = config.EnableDebugLogging;
            IsClothingContentsDebugEnabled = config.EnableClothingContentsDebugLogging;

            var summary = new ConfigurationReloadSummary
            {
                UsedDefaults = config.RuleSet == null,
                RegionEntries = normalizedRuleSet.RegionChances?.Count ?? 0,
                RegionDiscardedEntries = Math.Max(0, (rawRuleSet.RegionChances?.Count ?? 0) - (normalizedRuleSet.RegionChances?.Count ?? 0)),
                CustomItemEntries = normalizedRuleSet.CustomItemChances?.Count ?? 0,
                CustomItemDiscardedEntries = Math.Max(0, (rawRuleSet.CustomItemChances?.Count ?? 0) - (normalizedRuleSet.CustomItemChances?.Count ?? 0)),
                ClothingEntries = normalizedRuleSet.ClothingRules?.Count ?? 0,
                ClothingDiscardedEntries = Math.Max(0, (rawRuleSet.ClothingRules?.Count ?? 0) - (normalizedRuleSet.ClothingRules?.Count ?? 0)),
                DebugLoggingEnabled = config.EnableDebugLogging,
                ClothingContentsDebugEnabled = config.EnableClothingContentsDebugLogging
            };

            LastReloadSummary = summary;
            return summary;
        }

        public bool TryReload(out ConfigurationReloadSummary summary, out string error)
        {
            try
            {
                summary = ReloadFromConfiguration();
                Logger.Log("ModifiedItemDrop configuration reloaded successfully.");
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                error = ex.Message;
                summary = null;
                return false;
            }
        }
    }
}

