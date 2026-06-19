using System;
using System.IO;
using System.Reflection;
using FFEmqo.ModifiedItemDrop.Claim;
using FFEmqo.ModifiedItemDrop.Configuration;
using FFEmqo.ModifiedItemDrop.Drop;
using FFEmqo.ModifiedItemDrop.Domain;
using FFEmqo.ModifiedItemDrop.Utilities;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using SDG.Unturned;

namespace FFEmqo.ModifiedItemDrop.Plugin
{
    public sealed class ModifiedItemDropPlugin : RocketPlugin<ModifiedItemDropConfiguration>
    {
        private PlayerDeathHandler _deathHandler;
        private FileSystemWatcher _configWatcher;
        private DateTime _lastAutoReload = DateTime.MinValue;
        private ClaimStorage _claimStorage;
        private ClaimService _claimService;
        private DurableClaimStore _v2DurableClaimStore;
        private V2DurableClaimCreator _v2DurableClaimCreator;

        public static ModifiedItemDropPlugin Instance { get; private set; }

        public ConfigurationLoader ConfigurationLoader { get; private set; }

        public DropService DropService { get; private set; }

        public ClaimService ClaimService => _claimService;

        public V2DurableClaimCreator V2DurableClaimCreator => _v2DurableClaimCreator;

        protected override void Load()
        {
            if (Instance != null)
            {
                throw new InvalidOperationException("Attempted to load plugin twice.");
            }

            Instance = this;

            ConfigurationLoader = new ConfigurationLoader(this);
            DropService = new DropService(ConfigurationLoader);

            // Initialize claim persistence
            _claimStorage = new ClaimStorage(Directory);
            _claimService = new ClaimService(_claimStorage, () => Configuration?.Instance?.ClaimSettings ?? ClaimSettings.CreateDefault());
            _claimService.Initialize();

            _v2DurableClaimStore = new DurableClaimStore(V2ClaimStoragePaths.ForPluginDirectory(Directory));
            _v2DurableClaimCreator = new V2DurableClaimCreator(_v2DurableClaimStore);

            DropService.SetClaimService(_claimService);
            DropService.SetV2DurableClaimCreator(_v2DurableClaimCreator);

            _deathHandler = new PlayerDeathHandler(DropService, _claimService);
            _deathHandler.Enable();

            TryStartConfigWatcher();

            Logger.Log($"{Name} {Assembly.GetName().Version.ToString(3)} has been loaded!");
        }

        protected override void Unload()
        {
            _deathHandler?.Disable();
            _deathHandler = null;

            // Save any pending restores to claim storage before shutdown
            DropService?.FlushPendingRestores();

            _claimStorage?.ForceSave();

            DropService = null;
            ConfigurationLoader = null;
            _v2DurableClaimCreator = null;
            _v2DurableClaimStore = null;
            _claimService = null;
            _claimStorage = null;

            try
            {
                if (_configWatcher != null)
                {
                    _configWatcher.EnableRaisingEvents = false;
                    _configWatcher.Changed -= OnConfigFileChanged;
                    _configWatcher.Created -= OnConfigFileChanged;
                    _configWatcher.Renamed -= OnConfigFileRenamed;
                    _configWatcher.Error -= OnConfigWatcherError;
                    _configWatcher.Dispose();
                    _configWatcher = null;
                }
            }
            catch { }

            Instance = null;

            Logger.Log($"{Name} has been unloaded!");
        }

        public bool TryReloadConfiguration(out ConfigurationReloadSummary summary, out string error)
        {
            if (ConfigurationLoader == null)
            {
                summary = null;
                error = "Configuration loader not ready.";
                return false;
            }

            try
            {
                Configuration.Load();
            }
            catch (Exception ex)
            {
                summary = null;
                error = $"Failed to load configuration file: {ex.Message}";
                Logger.LogError($"[ModifiedItemDrop] Configuration reload failed: {ex.Message}");
                return false;
            }

            var result = ConfigurationLoader.TryReload(out summary, out error);
            if (result)
            {
                DropService?.RefreshRules();
            }

            return result;
        }

        private void TryStartConfigWatcher()
        {
            try
            {
                var folder = Directory;
                if (string.IsNullOrEmpty(folder) || !System.IO.Directory.Exists(folder))
                {
                    folder = AppDomain.CurrentDomain.BaseDirectory;
                }

                var configName = $"{Name}.configuration.xml";
                var configPath = Path.Combine(folder, configName);
                if (!File.Exists(configPath))
                {
                    LoggingHelper.LogWarning($"Auto-reload enabled but config file not found at: {configPath}");
                }

                _configWatcher = new FileSystemWatcher(folder, configName)
                {
                    IncludeSubdirectories = false,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName | NotifyFilters.CreationTime,
                    InternalBufferSize = 16384
                };
                _configWatcher.Changed += OnConfigFileChanged;
                _configWatcher.Created += OnConfigFileChanged;
                _configWatcher.Renamed += OnConfigFileRenamed;
                _configWatcher.Error += OnConfigWatcherError;
                _configWatcher.EnableRaisingEvents = true;

                LoggingHelper.LogInfo("Auto-reload enabled: watching configuration file for changes.");
            }
            catch (Exception ex)
            {
                LoggingHelper.LogException(ex, "TryStartConfigWatcher");
                LoggingHelper.LogWarning("Failed to start config file watcher; auto-reload disabled.");
            }
        }

        private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
        {
            TriggerAutoReload();
        }

        private void OnConfigFileRenamed(object sender, RenamedEventArgs e)
        {
            TriggerAutoReload();
        }

        private void OnConfigWatcherError(object sender, ErrorEventArgs e)
        {
            LoggingHelper.LogWarning($"Config watcher error: {e.GetException()?.Message}");
        }

        private void TriggerAutoReload()
        {
            // Debounce rapid successive events
            var now = DateTime.UtcNow;
            if ((now - _lastAutoReload).TotalMilliseconds < 800)
            {
                return;
            }
            _lastAutoReload = now;

            LoggingHelper.SafeExecute(
                () =>
                {
                    if (TryReloadConfiguration(out var summary, out var error))
                    {
                        var regions = summary?.RegionEntries ?? 0;
                        var items = summary?.CustomItemEntries ?? 0;
                        var cloth = summary?.ClothingEntries ?? 0;
                        LoggingHelper.LogInfo($"Auto-reloaded config. Regions={regions}, Items={items}, ClothingRules={cloth}.");
                    }
                    else
                    {
                        LoggingHelper.LogError($"Auto-reload failed: {error}");
                    }
                },
                "TriggerAutoReload"
            );
        }
    }
}

