using Rocket.API;

namespace FFEmqo.ModifiedItemDrop.Configuration
{
    public class ModifiedItemDropConfiguration : IRocketPluginConfiguration
    {
        public bool EnableDebugLogging { get; set; } = false;

        public bool EnableClothingContentsDebugLogging { get; set; } = false;

        public DropRuleSet RuleSet { get; set; } = DropRuleSet.CreateDefault();

        public ClaimSettings ClaimSettings { get; set; } = ClaimSettings.CreateDefault();

        public HandsSlotSettings HandsSlotSettings { get; set; } = HandsSlotSettings.CreateDefault();

        public void LoadDefaults()
        {
            EnableDebugLogging = false;
            EnableClothingContentsDebugLogging = false;
            RuleSet = DropRuleSet.CreateDefault();
            ClaimSettings = ClaimSettings.CreateDefault();
            HandsSlotSettings = HandsSlotSettings.CreateDefault();
        }
    }
}

