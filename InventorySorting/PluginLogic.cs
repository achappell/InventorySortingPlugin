using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CriticalCommonLib.Crafting;
using CriticalCommonLib.MarketBoard;
using CriticalCommonLib.Models;
using CriticalCommonLib.Services;
using CriticalCommonLib.Services.Mediator;
using Dalamud.Plugin.Services;
using InventorySorting.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static FFXIVClientStructs.FFXIV.Client.LayoutEngine.LayoutManager;

namespace InventorySorting
{
    public partial class PluginLogic : DisposableMediatorSubscriberBase, IHostedService
    {
        private readonly ConfigurationManagerService _configurationManagerService;
        private readonly IChatUtilities _chatUtilities;
        private readonly ILogger<PluginLogic> _logger;
        private readonly IFramework _framework;
        private readonly HostedInventoryHistory _hostedInventoryHistory;
        private readonly IInventoryMonitor _inventoryMonitor;
        private readonly IInventoryScanner _inventoryScanner;
        private readonly ICharacterMonitor _characterMonitor;
        private readonly Configuration _configuration;
        private readonly IMobTracker _mobTracker;
        private readonly ICraftMonitor _craftMonitor;
        private readonly IGameInterface _gameInterface;
        private readonly ITooltipService _tooltipService;
        private readonly IMarketCache _marketCache;
        private Dictionary<uint, InventoryMonitor.ItemChangesItem> _recentlyAddedSeen = new();

        public bool WasRecentlySeen(uint itemId)
        {
            if (_recentlyAddedSeen.ContainsKey(itemId))
            {
                return true;
            }
            return false;
        }

        public TimeSpan? GetLastSeenTime(uint itemId)
        {
            if (WasRecentlySeen(itemId))
            {
                return DateTime.Now - _recentlyAddedSeen[itemId].Date;
            }
            return null;
        }
        private DateTime? _nextSaveTime = null;

        public PluginLogic(ConfigurationManagerService configurationManagerService, IChatUtilities chatUtilities, ILogger<PluginLogic> logger, IFramework framework, MediatorService mediatorService, HostedInventoryHistory hostedInventoryHistory, IInventoryMonitor inventoryMonitor, IInventoryScanner inventoryScanner, ICharacterMonitor characterMonitor, Configuration configuration, IMobTracker mobTracker, ICraftMonitor craftMonitor, IGameInterface gameInterface, IMarketCache marketCache, ITooltipService tooltipService) : base(logger, mediatorService)
        {
            _configurationManagerService = configurationManagerService;
            _chatUtilities = chatUtilities;
            _logger = logger;
            _framework = framework;
            _hostedInventoryHistory = hostedInventoryHistory;
            _inventoryMonitor = inventoryMonitor;
            _inventoryScanner = inventoryScanner;
            _characterMonitor = characterMonitor;
            _configuration = configuration;
            _mobTracker = mobTracker;
            _craftMonitor = craftMonitor;
            _gameInterface = gameInterface;
            _tooltipService = tooltipService;
            _marketCache = marketCache;

        }

        private void CraftMonitorOnCraftCompleted(uint itemid, FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags flags, uint quantity)
        {
        }

        private void CraftMonitorOnCraftFailed(uint itemid)
        {
        }

        private void CraftMonitorOnCraftStarted(uint itemid)
        {
        }


        private void GameInterfaceOnAcquiredItemsUpdated()
        {
            var activeCharacter = _characterMonitor.ActiveCharacterId;
            if (activeCharacter != 0)
            {
                _configuration.AcquiredItems[activeCharacter] = _gameInterface.AcquiredItems;
            }
        }

        public void ClearOrphans()
        {
            var keys = _inventoryMonitor.Inventories.Keys;
            foreach (var key in keys)
            {
                var character = _characterMonitor.GetCharacterById(key);
                if (character == null)
                {
                    _logger.LogInformation("Removing inventories for " + key + " from inventory cache as there is no character associated with this inventory.");
                    _inventoryMonitor.ClearCharacterInventories(key);
                }
            }
        }



        private void FrameworkOnUpdate(IFramework framework)
        {
            if (_configuration.AutoSave)
            {
                if (NextSaveTime == null && _configuration.AutoSaveMinutes != 0)
                {
                    _nextSaveTime = DateTime.Now.AddMinutes(_configuration.AutoSaveMinutes);
                }
                else
                {
                    if (DateTime.Now >= NextSaveTime)
                    {
                        _nextSaveTime = null;
                        _configuration.IsDirty = true;
                    }
                }
            }
        }

        private void ConfigOnConfigurationChanged()
        {
            SyncConfigurationChanges();
        }

        private void SyncConfigurationChanges(bool save = true)
        {

            if (_hostedInventoryHistory.Enabled != _configuration.HistoryEnabled)
            {
                if (_configuration.HistoryEnabled)
                {
                    _hostedInventoryHistory.Enable();
                }
                else
                {
                    _hostedInventoryHistory.Disable();
                }
            }

            if (_configuration.HistoryTrackReasons != null)
            {
                if (_hostedInventoryHistory.ReasonsToLog.ToList() !=
                    _configuration.HistoryTrackReasons)
                {
                    _hostedInventoryHistory.SetChangeReasonsToLog(
                        _configuration.HistoryTrackReasons.Distinct().ToHashSet());
                }
            }
        }

        private void CharacterMonitorOnOnCharacterUpdated(Character? character)
        {
            if (character != null)
            {
                _configuration.IsDirty = true;
                if (_configuration.AcquiredItems.ContainsKey(character.CharacterId))
                {
                    _gameInterface.AcquiredItems = _configuration.AcquiredItems[character.CharacterId];
                }
            }
            else
            {
                _gameInterface.AcquiredItems = new HashSet<uint>();
            }
        }

        public DateTime? NextSaveTime => _nextSaveTime;

        public void ClearAutoSave()
        {
            _nextSaveTime = null;
        }

        private void InventoryMonitorOnOnInventoryChanged(List<InventoryChange> inventoryChanges, InventoryMonitor.ItemChanges? itemChanges)
        {
            _logger.LogTrace("PluginLogic: Inventory changed, saving to config.");
            var allItems = _inventoryMonitor.AllItems.ToList();
            _configurationManagerService.SaveInventories(allItems);
            if (_configuration.AutomaticallyDownloadMarketPrices)
            {
                var activeCharacter = _characterMonitor.ActiveCharacter;
                if (activeCharacter != null)
                {
                    foreach (var inventory in allItems)
                    {
                        _marketCache.RequestCheck(inventory.ItemId, activeCharacter.WorldId, false);
                    }
                }
            }

            if (itemChanges != null)
            {
                foreach (var item in itemChanges.NewItems)
                {
                    if (_recentlyAddedSeen.ContainsKey(item.ItemId))
                    {
                        _recentlyAddedSeen.Remove(item.ItemId);
                    }

                    _recentlyAddedSeen.Add(item.ItemId, item);
                }
            }
        }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            Logger.LogTrace("Starting service {type} ({this})", GetType().Name, this);
            _inventoryMonitor.Start();
            _inventoryScanner.Enable();
            _inventoryMonitor.OnInventoryChanged += InventoryMonitorOnOnInventoryChanged;
            _characterMonitor.OnCharacterUpdated += CharacterMonitorOnOnCharacterUpdated;
            _framework.Update += FrameworkOnUpdate;
            _configurationManagerService.ConfigurationChanged += ConfigOnConfigurationChanged;

            _craftMonitor.CraftStarted += CraftMonitorOnCraftStarted;
            _craftMonitor.CraftFailed += CraftMonitorOnCraftFailed;
            _craftMonitor.CraftCompleted += CraftMonitorOnCraftCompleted;
            _gameInterface.AcquiredItemsUpdated += GameInterfaceOnAcquiredItemsUpdated;

            SyncConfigurationChanges(false);
            ClearOrphans();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogTrace("Stopping service {type} ({this})", GetType().Name, this);
            _gameInterface.AcquiredItemsUpdated -= GameInterfaceOnAcquiredItemsUpdated;
            _configuration.SavedCharacters = _characterMonitor.Characters;
            _framework.Update -= FrameworkOnUpdate;
            _inventoryMonitor.OnInventoryChanged -= InventoryMonitorOnOnInventoryChanged;
            _characterMonitor.OnCharacterUpdated -= CharacterMonitorOnOnCharacterUpdated;
            _craftMonitor.CraftStarted -= CraftMonitorOnCraftStarted;
            _craftMonitor.CraftFailed -= CraftMonitorOnCraftFailed;
            _craftMonitor.CraftCompleted -= CraftMonitorOnCraftCompleted;
            _configurationManagerService.ConfigurationChanged -= ConfigOnConfigurationChanged;
            _configurationManagerService.Save();
            _configurationManagerService.SaveInventories(_inventoryMonitor.AllItems.ToList());
            _configurationManagerService.SaveHistory(_hostedInventoryHistory.GetHistory());
            return Task.CompletedTask;
        }
    }
}
