using System.Threading;
using System.Threading.Tasks;
using CriticalCommonLib.Services;
using CriticalCommonLib.Services.Mediator;
using DalaMock.Shared.Interfaces;
using Dalamud.Interface.ImGuiFileDialog;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using InventorySorting.Mediator;
using InventorySorting.Services;

namespace InventorySorting.UI
{
    using Dalamud.Plugin;
    using InventorySorting.Windows;

    public partial class InventorySortingUI : DisposableMediatorSubscriberBase, IHostedService
    {
        private readonly IDalamudPluginInterface _pluginInterfaceService;
        private readonly ICharacterMonitor _characterMonitor;
        private readonly WindowService _windowService;
        private readonly FileDialogManager _fileDialogManager;
        private readonly Configuration _configuration;
        private bool _disposing = false;

        public InventorySortingUI(IDalamudPluginInterface pluginInterfaceService, ILogger<InventorySortingUI> logger, MediatorService mediatorService, ICharacterMonitor characterMonitor, WindowService windowService, FileDialogManager fileDialogManager, Configuration configuration) : base(logger, mediatorService)
        {
            _pluginInterfaceService = pluginInterfaceService;
            _characterMonitor = characterMonitor;
            _windowService = windowService;
            _fileDialogManager = fileDialogManager;
            _configuration = configuration;
        }

        private void InterfaceOnOpenMainUi()
        {
            BypassLoginStatus = true;
            MediatorService.Publish(new OpenGenericWindowMessage(typeof(MainWindow)));
        }

        private void UiBuilderOnOpenConfigUi()
        {
            BypassLoginStatus = true;
            MediatorService.Publish(new OpenGenericWindowMessage(typeof(ConfigWindow)));
        }

        public bool BypassLoginStatus { get; set; }

        public bool IsVisible
        {
            get => _configuration.IsVisible;
            set => _configuration.IsVisible = value;
        }

        public void Draw()
        {
            if (!_characterMonitor.IsLoggedIn && !BypassLoginStatus || _disposing)
                return;
            _windowService.WindowSystem.Draw();

            _fileDialogManager.Draw();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {

            }

            base.Dispose(disposing);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Logger.LogTrace("Starting service {type} ({this})", GetType().Name, this);
            _pluginInterfaceService.UiBuilder.Draw += Draw;
            _pluginInterfaceService.UiBuilder.OpenConfigUi += UiBuilderOnOpenConfigUi;
            _pluginInterfaceService.UiBuilder.OpenMainUi += InterfaceOnOpenMainUi;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogTrace("Stopping service {type} ({this})", GetType().Name, this);
            _pluginInterfaceService.UiBuilder.Draw -= Draw;
            _pluginInterfaceService.UiBuilder.OpenConfigUi -= UiBuilderOnOpenConfigUi;
            _pluginInterfaceService.UiBuilder.OpenMainUi -= InterfaceOnOpenMainUi;
            return Task.CompletedTask;
        }
    }
}
