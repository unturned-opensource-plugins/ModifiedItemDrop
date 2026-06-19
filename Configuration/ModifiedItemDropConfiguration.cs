using Rocket.API;
using FFEmqo.ModifiedItemDrop.Domain;

namespace FFEmqo.ModifiedItemDrop.Configuration
{
    public class ModifiedItemDropConfiguration : IRocketPluginConfiguration
    {
        public bool EnableDebugLogging { get; set; } = false;

        public bool EnableClothingContentsDebugLogging { get; set; } = false;

        public string OutcomeRulesXml { get; set; } = DefaultOutcomeRules.Xml;

        public ClaimSettings ClaimSettings { get; set; } = ClaimSettings.CreateDefault();

        public HandsSlotSettings HandsSlotSettings { get; set; } = HandsSlotSettings.CreateDefault();

        public void LoadDefaults()
        {
            EnableDebugLogging = false;
            EnableClothingContentsDebugLogging = false;
            OutcomeRulesXml = DefaultOutcomeRules.Xml;
            ClaimSettings = ClaimSettings.CreateDefault();
            HandsSlotSettings = HandsSlotSettings.CreateDefault();
        }
    }
}
