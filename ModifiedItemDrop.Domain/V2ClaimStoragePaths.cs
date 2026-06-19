using System;
using System.IO;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class V2ClaimStoragePaths
    {
        private V2ClaimStoragePaths(string rootDirectory)
        {
            RootDirectory = rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory));
            PrimaryPath = Path.Combine(rootDirectory, "claims.json");
            BackupPath = Path.Combine(rootDirectory, "claims.json.bak");
            CorruptDirectory = Path.Combine(rootDirectory, "corrupt");
        }

        public string RootDirectory { get; }

        public string PrimaryPath { get; }

        public string BackupPath { get; }

        public string CorruptDirectory { get; }

        public static V2ClaimStoragePaths ForPluginDirectory(string pluginDirectory)
        {
            if (string.IsNullOrWhiteSpace(pluginDirectory))
            {
                throw new ArgumentException("Plugin directory must be provided.", nameof(pluginDirectory));
            }

            return new V2ClaimStoragePaths(Path.Combine(pluginDirectory, "claims", "v2"));
        }
    }
}
