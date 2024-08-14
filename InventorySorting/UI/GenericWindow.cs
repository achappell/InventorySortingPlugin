using CriticalCommonLib.Services.Mediator;
using Microsoft.Extensions.Logging;
using System;

namespace InventorySorting.UI
{
    public abstract class GenericWindow : Window
    {
        public GenericWindow(ILogger logger, MediatorService mediator, Configuration configuration, string name = "") : base(logger, mediator, configuration, name)
        {
        }
        public abstract void Initialize();
    }
}
