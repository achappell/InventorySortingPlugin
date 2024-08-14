using System;
using System.Linq;
using CriticalCommonLib;
using CriticalCommonLib.Extensions;
using CriticalCommonLib.Services;
using CriticalCommonLib.Services.Mediator;
using CriticalCommonLib.Sheets;
using InventorySorting.Attributes;
using InventorySorting.Mediator;
using InventorySorting.Windows;
using Microsoft.Extensions.Logging;

namespace InventorySorting.Commands
{
    public class PluginCommands
    {
        public ILogger<PluginCommands> Logger { get; }
        private readonly MediatorService _mediatorService;

        public PluginCommands(MediatorService mediatorService, ILogger<PluginCommands> logger)
        {
            Logger = logger;
            _mediatorService = mediatorService;
        }

        [Command("/invsort")]
        [HelpMessage("Some help")]
        public void ShowHideInventoryToolsCommand(string command, string args)
        {
            _mediatorService.Publish(new ToggleGenericWindowMessage(typeof(MainWindow)));
        }
    }
}
