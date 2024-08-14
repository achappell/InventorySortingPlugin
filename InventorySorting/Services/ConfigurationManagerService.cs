using CriticalCommonLib.Interfaces;
using Dalamud.Plugin.Services;
using Dalamud.Plugin;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CriticalCommonLib.Resolvers;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Hosting;
using System.Threading;
using LuminaSupplemental.Excel.Model;
using CriticalCommonLib.Models;

namespace InventorySorting.Services
{
    public class ConfigurationManagerService: BackgroundService
    {
        public ILogger<ConfigurationManagerService> Logger { get; }

        public delegate void ConfigurationChangedDelegate();
        private readonly IFramework _framework;
        private bool _configurationLoaded = false;

        public event ConfigurationChangedDelegate? ConfigurationChanged;

        public ConfigurationManagerService(IFramework framework, IDalamudPluginInterface pluginInterfaceService, ILogger<ConfigurationManagerService> logger, IBackgroundTaskQueue saveQueue)
        {
            Logger = logger;
            _pluginInterfaceService = pluginInterfaceService;
            _saveQueue = saveQueue;
            _framework = framework;
            _framework.Update += OnUpdate;
        }

        private void OnUpdate(IFramework framework)
        {
            if (_configurationLoaded)
            {
                if (Config.IsDirty)
                {
                    Config.IsDirty = false;
                    ConfigurationChanged?.Invoke();
                    SaveAsync();
                }
            }
        }

        public Configuration Config
        {
            get;
            set;
        } = null!;

        public string ConfigurationFile
        {
            get
            {
                return _pluginInterfaceService.ConfigFile.ToString();
            }
        }

        public Configuration GetConfig()
        {
            return Config;
        }

        public void Load(string? file = null)
        {
            Logger.LogTrace("Loading configuration");
            Stopwatch loadConfigStopwatch = new Stopwatch();
            loadConfigStopwatch.Start();
            if (!File.Exists(file ?? ConfigurationFile))
            {
                Config = new Configuration();
                Config.MarkReloaded();
                _configurationLoaded = true;
                return;
            }
            string jsonText = File.ReadAllText(file ?? ConfigurationFile);
            var inventoryToolsConfiguration = JsonConvert.DeserializeObject<Configuration>(jsonText, new JsonSerializerSettings()
            {
                Converters = new List<JsonConverter>()
                {
                },
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                TypeNameHandling = TypeNameHandling.None,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
            });
            if (inventoryToolsConfiguration == null)
            {
                Config = new Configuration();
                Config.MarkReloaded();
                _configurationLoaded = true;
                return;
            }
            loadConfigStopwatch.Stop();
            Logger.LogTrace("Took " + loadConfigStopwatch.Elapsed.TotalSeconds + " to load main configuration file.");
            Config = inventoryToolsConfiguration;
            Config.MarkReloaded();
            _configurationLoaded = true;
        }

        public void Save()
        {
            Stopwatch loadConfigStopwatch = new Stopwatch();
            loadConfigStopwatch.Start();
            Logger.LogTrace("Saving allagan tools configuration");
            try
            {
                File.WriteAllText(ConfigurationFile, JsonConvert.SerializeObject(Config, Formatting.None, new JsonSerializerSettings()
                {
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                    TypeNameHandling = TypeNameHandling.None,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
                }));
                loadConfigStopwatch.Stop();
                Logger.LogTrace("Took " + loadConfigStopwatch.Elapsed.TotalSeconds + " to save configuration.");
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to save allagan tools configuration due to {e.Message}");
            }
        }

        private void SaveAsync()
        {
            _saveQueue.QueueBackgroundWorkItemAsync(token => Task.Run(Save, token));
        }

        private readonly IDalamudPluginInterface _pluginInterfaceService;
        private readonly IBackgroundTaskQueue _saveQueue;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await BackgroundProcessing(stoppingToken);
        }

        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var workItem =
                    await _saveQueue.DequeueAsync(stoppingToken);

                try
                {
                    await workItem(stoppingToken);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex,
                        "Error occurred executing {WorkItem}.", nameof(workItem));
                }
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            Logger.LogTrace("Configuration manager save queue is stopping.");
            Save();
            await base.StopAsync(stoppingToken);
        }

        public override void Dispose()
        {
            _framework.Update -= OnUpdate;
        }

        public bool SaveInventories(List<InventoryItem> items)
        {
            return CsvLoader.ToCsvRaw<InventoryItem>(items, Path.Join(_pluginInterfaceService.ConfigDirectory.FullName, "inventories.csv"));
        }

        public bool SaveHistory(List<InventoryChange> changes)
        {
            return CsvLoader.ToCsvRaw<InventoryChange>(changes, Path.Join(_pluginInterfaceService.ConfigDirectory.FullName, "history.csv"));
        }
    }
}
