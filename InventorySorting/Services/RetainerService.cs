using CriticalCommonLib;
using CriticalCommonLib.Services.Mediator;
using ECommons.Automation.NeoTaskManager;
using ECommons.DalamudServices;
using InventorySorting.Logic;
using InventorySorting.Mediator;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InventorySorting.Services
{
    internal class RetainerService(MediatorService mediatorService, Configuration configuration) : IHostedService, IMediatorSubscriber, IDisposable
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            mediatorService.Subscribe<StoreOnRetainerMessage>(this, StoreOnRetainer);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            mediatorService.UnsubscribeAll(this);
            return Task.CompletedTask;
        }

        private void StoreOnRetainer(StoreOnRetainerMessage message)
        {
            var bell = RetainerLogic.GetReachableRetainerBell();
            if (bell != null)
            {
                Service.Targets.Target = bell;
                RetainerLogic.InteractWithTargetedBell();
            }
        }
        public void Dispose()
        {
            mediatorService.UnsubscribeAll(this);
        }

        public MediatorService MediatorService => mediatorService;
    }
}
