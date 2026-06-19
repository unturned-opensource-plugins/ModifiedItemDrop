using System;
using System.Collections.Generic;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class OutcomeRuleConfigurationState
    {
        private OutcomeRuleConfigurationState(bool deathProcessingEnabled, IReadOnlyList<OutcomeRule> rules, string diagnostic)
        {
            DeathProcessingEnabled = deathProcessingEnabled;
            Rules = rules ?? Array.Empty<OutcomeRule>();
            Diagnostic = diagnostic ?? string.Empty;
        }

        public bool DeathProcessingEnabled { get; }

        public IReadOnlyList<OutcomeRule> Rules { get; }

        public string Diagnostic { get; }

        public static OutcomeRuleConfigurationState FromXml(string xml)
        {
            try
            {
                var rules = OutcomeRuleXmlParser.Parse(xml);
                return new OutcomeRuleConfigurationState(
                    deathProcessingEnabled: true,
                    rules: rules,
                    diagnostic: string.Empty);
            }
            catch (Exception ex)
            {
                return new OutcomeRuleConfigurationState(
                    deathProcessingEnabled: false,
                    rules: Array.Empty<OutcomeRule>(),
                    diagnostic: ex.Message);
            }
        }
    }
}
