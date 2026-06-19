namespace FFEmqo.ModifiedItemDrop.Configuration
{
    public sealed class ConfigurationReloadSummary
    {
        public static ConfigurationReloadSummary Empty { get; } = new ConfigurationReloadSummary();

        public bool UsedDefaults { get; set; }


        public bool DebugLoggingEnabled { get; set; }

        public bool ClothingContentsDebugEnabled { get; set; }

        public bool DeathProcessingEnabled { get; set; } = true;

        public string SafeModeReason { get; set; }

        public bool HasWarnings => !DeathProcessingEnabled;
    }
}
