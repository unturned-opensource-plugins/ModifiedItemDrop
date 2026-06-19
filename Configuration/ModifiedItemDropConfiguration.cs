using Rocket.API;
using FFEmqo.ModifiedItemDrop.Domain;

namespace FFEmqo.ModifiedItemDrop.Configuration
{
    public class ModifiedItemDropConfiguration : IRocketPluginConfiguration
    {
        public bool EnableDebugLogging { get; set; } = false;

        public bool EnableClothingContentsDebugLogging { get; set; } = false;

        public string OutcomeRulesXml { get; set; } = DefaultOutcomeRules.Xml;

        public DropRuleSet RuleSet { get; set; } = DropRuleSet.CreateDefault();

        public ClaimSettings ClaimSettings { get; set; } = ClaimSettings.CreateDefault();

        public HandsSlotSettings HandsSlotSettings { get; set; } = HandsSlotSettings.CreateDefault();

        public DeathSettings DeathSettings { get; set; } = DeathSettings.CreateDefault();

        public void LoadDefaults()
        {
            EnableDebugLogging = false;
            EnableClothingContentsDebugLogging = false;
            OutcomeRulesXml = DefaultOutcomeRules.Xml;
            RuleSet = DropRuleSet.CreateDefault();
            ClaimSettings = ClaimSettings.CreateDefault();
            HandsSlotSettings = HandsSlotSettings.CreateDefault();
            DeathSettings = DeathSettings.CreateDefault();
        }
    }
}
