using CriticalCommonLib;
using CriticalCommonLib.Services;
using CriticalCommonLib.Services.Mediator;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.DalamudServices;
using InventorySorting.Mediator;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lumina.Excel;

namespace InventorySorting.Services
{
    public class ItemRequestService(MediatorService mediatorService, Configuration configuration, IInventoryScanner scanner, IChatGui chatGui, ExcelCache excelCache) : IHostedService, IMediatorSubscriber, IDisposable
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            mediatorService.Subscribe<ItemSearchRequestedMessage>(this, ItemSearchRequested);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            mediatorService.UnsubscribeAll(this);
            return Task.CompletedTask;
        }

        private void ItemSearchRequested(ItemSearchRequestedMessage searchRequest)
        {
            for (int i = 0; i < scanner.CharacterBag1.Length; i++)
            {
                var item = scanner.CharacterBag1[i];
                var itemName = excelCache.GetItemExSheet().GetRow(item.ItemId);
                if (item.ItemId > 0)
                {
                    chatGui.Print("Item id: " + item.ItemId);
                    chatGui.Print(itemName.NameString);
                }
            }
        }
        public void Dispose()
        {
            mediatorService.UnsubscribeAll(this);
        }

        public MediatorService MediatorService => mediatorService;
    }
}