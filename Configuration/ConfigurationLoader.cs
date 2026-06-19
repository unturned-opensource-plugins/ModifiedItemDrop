using System;
using System.Collections.Generic;
using FFEmqo.ModifiedItemDrop.Domain;
using FFEmqo.ModifiedItemDrop.Plugin;
using Rocket.Core.Logging;

namespace FFEmqo.ModifiedItemDrop.Configuration
{
    public sealed class ConfigurationLoader
    {
        private IReadOnlyList<OutcomeRule> _currentOutcomeRules = Array.Empty<OutcomeRule>();
        private OutcomeRuleConfigurationState _outcomeRuleState = OutcomeRuleConfigurationState.FromXml(DefaultOutcomeRules.Xml);
        private readonly ModifiedItemDropPlugin _plugin;

        public bool IsDebugLoggingEnabled { get; private set; }

        public bool IsClothingContentsDebugEnabled { get; private set; }

        public ConfigurationReloadSummary LastReloadSummary { get; private set; } = ConfigurationReloadSummary.Empty;

        public ConfigurationLoader(ModifiedItemDropPlugin plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            ReloadFromConfiguration();
        }

        public IReadOnlyList<OutcomeRule> CurrentOutcomeRules => _currentOutcomeRules;

        public bool IsDeathProcessingEnabled => _outcomeRuleState.DeathProcessingEnabled;

        public string SafeModeReason => _outcomeRuleState.Diagnostic;

        public HandsSlotSettings HandsSlotSettings => _plugin.Configuration?.Instance?.HandsSlotSettings ?? HandsSlotSettings.CreateDefault();

        public ConfigurationReloadSummary ReloadFromConfiguration()
        {
            var config = _plugin.Configuration?.Instance ?? new ModifiedItemDropConfiguration();
            var outcomeRulesXml = string.IsNullOrWhiteSpace(config.OutcomeRulesXml)
                ? DefaultOutcomeRules.Xml
                : config.OutcomeRulesXml;
            _outcomeRuleState = OutcomeRuleConfigurationState.FromXml(outcomeRulesXml);
            _currentOutcomeRules = _outcomeRuleState.Rules;
            IsDebugLoggingEnabled = config.EnableDebugLogging;
            IsClothingContentsDebugEnabled = config.EnableClothingContentsDebugLogging;

            var summary = new ConfigurationReloadSummary
            {
                UsedDefaults = string.IsNullOrWhiteSpace(config.OutcomeRulesXml),
                DebugLoggingEnabled = config.EnableDebugLogging,
                ClothingContentsDebugEnabled = config.EnableClothingContentsDebugLogging,
                DeathProcessingEnabled = _outcomeRuleState.DeathProcessingEnabled,
                SafeModeReason = _outcomeRuleState.Diagnostic
            };

            if (!_outcomeRuleState.DeathProcessingEnabled)
            {
                Logger.LogWarning("[ModifiedItemDrop] Entering safe mode: death processing is disabled because Outcome Rules are invalid. " + _outcomeRuleState.Diagnostic);
            }

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
