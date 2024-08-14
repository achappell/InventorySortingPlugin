using CriticalCommonLib.Crafting;
using CriticalCommonLib.Services.Ui;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InventorySorting.Services
{
    public class HostedCraftMonitor : CraftMonitor, IHostedService
    {
        public HostedCraftMonitor(IGameUiManager gameUiManager) : base(gameUiManager)
        {
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
