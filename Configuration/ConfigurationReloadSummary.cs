namespace FFEmqo.ModifiedItemDrop.Configuration
{
    public sealed class ConfigurationReloadSummary
    {
        public static ConfigurationReloadSummary Empty { get; } = new ConfigurationReloadSummary();

        public bool UsedDefaults { get; set; }

        public int RegionEntries { get; set; }

        public int RegionDiscardedEntries { get; set; }

        public int CustomItemEntries { get; set; }

        public int CustomItemDiscardedEntries { get; set; }

        public int ClothingEntries { get; set; }

        public int ClothingDiscardedEntries { get; set; }

        public bool DebugLoggingEnabled { get; set; }

        public bool ClothingContentsDebugEnabled { get; set; }

        public bool HasWarnings =>
            RegionDiscardedEntries > 0 ||
            CustomItemDiscardedEntries > 0 ||
            ClothingDiscardedEntries > 0;
    }
}

