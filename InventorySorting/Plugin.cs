using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Util;
using CriticalCommonLib;
using CriticalCommonLib.Crafting;
using CriticalCommonLib.Interfaces;
using CriticalCommonLib.Ipc;
using CriticalCommonLib.MarketBoard;
using CriticalCommonLib.Services;
using CriticalCommonLib.Services.Mediator;
using CriticalCommonLib.Services.Ui;
using CriticalCommonLib.Time;
using DalaMock.Host.Factories;
using DalaMock.Host.Hosting;
using DalaMock.Shared.Classes;
using DalaMock.Shared.Interfaces;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using InventorySorting.UI;
using Window = InventorySorting.UI.Window;
using ECommons.Logging;
using InventorySorting.Commands;
using static FFXIVClientStructs.FFXIV.Client.LayoutEngine.LayoutManager;
using static Lumina.Data.BaseFileHandle;
using System.Windows.Forms.Design;
using InventorySorting.Services;
using InventorySorting.Windows;
using InventorySorting.Host;
using InventorySorting.Logic;

namespace InventorySorting;

public class Plugin : HostedPlugin
{
    private readonly IPluginLog _pluginLog;
    private Service? _service;
    private IDalamudPluginInterface PluginInterface { get; set; }

    private const string CommandName = "/pmycommand";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("InventorySorting");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    public Plugin(IDalamudPluginInterface pluginInterface, IPluginLog pluginLog,
            IAddonLifecycle addonLifecycle, IChatGui chatGui, IClientState clientState, ICommandManager commandManager,
            ICondition condition, IDataManager dataManager, IFramework framework, IGameGui gameGui,
            IGameInteropProvider gameInteropProvider, IKeyState keyState, IGameNetwork gameNetwork,
            IObjectTable objectTable, ITargetManager targetManager, ITextureProvider textureProvider,
            IToastGui toastGui, IContextMenu contextMenu, ITitleScreenMenu titleScreenMenu) : base(pluginInterface,
            pluginLog, addonLifecycle, chatGui, clientState, commandManager,
            condition, dataManager, framework, gameGui,
            gameInteropProvider, keyState, gameNetwork,
            objectTable, targetManager, textureProvider,
            toastGui, contextMenu, titleScreenMenu)
    {
        _pluginLog = pluginLog;
        PluginInterface = pluginInterface;
        _service = PluginInterface.Create<Service>()!;
        CreateHost();
        Start();
    }

    public override void PreBuild(IHostBuilder hostBuilder)
    {
        hostBuilder.UseContentRoot(PluginInterface.ConfigDirectory.FullName)
            .ConfigureLogging(lb =>
            {
                lb.ClearProviders();
                lb.AddDalamudLogging();
                lb.SetMinimumLevel(LogLevel.Trace);
            });
        hostBuilder.UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureContainer<ContainerBuilder>(builder =>
            {
                Dictionary<Type, Type> transientExternallyOwnedPairs = new Dictionary<Type, Type>()
                    {
                        { typeof(GenericWindow), typeof(Window) }
                    };

                var loadableTypes = Assembly.GetExecutingAssembly().GetLoadableTypes().Where(c =>
                        c is { IsInterface: false, IsAbstract: false } &&
                        (!c.ContainsGenericParameters || c.IsConstructedGenericType)).ToList();
                foreach (var type in loadableTypes)
                {
                    foreach (var pair in transientExternallyOwnedPairs)
                    {
                        if (pair.Key.IsAssignableFrom(type))
                        {
                            builder.RegisterType(type).As(pair.Value).As(pair.Key).As(type).ExternallyOwned();
                        }
                    }
                }
            }
            );

        hostBuilder.ConfigureContainer<ContainerBuilder>(builder =>
        {
            builder.RegisterType<ExcelCache>().SingleInstance().ExternallyOwned();
            builder.RegisterType<HostedUniversalis>().AsSelf().As<IUniversalis>().SingleInstance()
                .ExternallyOwned();
            builder.RegisterType<MediatorService>().SingleInstance().ExternallyOwned();
            builder.RegisterType<PluginCommandManager<PluginCommands>>().SingleInstance().ExternallyOwned();
            builder.RegisterType<HostedInventoryHistory>().SingleInstance().ExternallyOwned();
            builder.RegisterType<InventorySortingUI>().SingleInstance().ExternallyOwned();
            builder.RegisterType<WindowService>().SingleInstance().ExternallyOwned();
            builder.RegisterType<ConfigurationManagerService>().SingleInstance().ExternallyOwned();
            builder.RegisterType<ItemRequestService>().SingleInstance().ExternallyOwned();
            builder.RegisterType<PluginLogic>().SingleInstance().ExternallyOwned();
        });

        hostBuilder.ConfigureContainer<ContainerBuilder>(builder =>
        {
            builder.Register(c => new HttpClient()).As<HttpClient>();
            builder.RegisterType<SeTime>().As<ISeTime>().SingleInstance();
            builder.RegisterType<FileDialogManager>().SingleInstance();
            builder.RegisterType<BackgroundTaskQueue>().As<IBackgroundTaskQueue>();
            builder.RegisterType<CharacterMonitor>().As<ICharacterMonitor>().SingleInstance();
            builder.RegisterType<ChatUtilities>().As<IChatUtilities>().SingleInstance();
            builder.RegisterType<Font>().As<IFont>().SingleInstance();
            builder.RegisterType<GameInterface>().As<IGameInterface>().SingleInstance();
            builder.RegisterType<GameUiManager>().As<IGameUiManager>().SingleInstance();
            builder.RegisterType<InventoryMonitor>().As<IInventoryMonitor>().SingleInstance();
            builder.RegisterType<InventoryScanner>().As<IInventoryScanner>().SingleInstance();
            builder.RegisterType<MarketCache>().As<IMarketCache>().SingleInstance();
            builder.RegisterType<MobTracker>().As<IMobTracker>().SingleInstance();
            builder.RegisterType<TeleporterIpc>().As<ITeleporterIpc>().SingleInstance();
            builder.RegisterType<TooltipService>().As<ITooltipService>().SingleInstance();
            builder.RegisterType<MarketboardTaskQueue>().SingleInstance();
            builder.RegisterType<InventoryHistory>().SingleInstance();
            builder.RegisterType<DalamudLogger>().SingleInstance();
            builder.RegisterType<OdrScanner>().SingleInstance();
            builder.RegisterType<PluginCommands>().SingleInstance();
            builder.RegisterType<TryOn>().SingleInstance();
            builder.RegisterType<CraftPricer>().SingleInstance();
            builder.RegisterType<WindowSystemFactory>().As<IWindowSystemFactory>().SingleInstance();
            builder.RegisterType<DalamudWindowSystem>().As<IWindowSystem>();
            builder.RegisterType<HostedUniversalisConfiguration>().AsSelf().As<IHostedUniversalisConfiguration>()
                    .SingleInstance();
            builder.RegisterType<HostedCraftMonitor>().AsSelf().As<ICraftMonitor>().SingleInstance();

            builder.Register(provider =>
            {
                var configurationManager = provider.Resolve<ConfigurationManagerService>();
                configurationManager.Load();
                var configuration = configurationManager.GetConfig();
                configuration.ClearDirtyFlags();
                return configuration;
            }).As<Configuration>().SingleInstance();

            builder.Register<Func<Type, GenericWindow>>(c =>
            {
                var context = c.Resolve<IComponentContext>();
                return type =>
                {
                    var genericWindow = (GenericWindow)context.Resolve(type);
                    genericWindow.Initialize();
                    return genericWindow;
                };
            });
            builder.Register<Func<int, IBackgroundTaskQueue>>(c =>
            {
                return capacity =>
                {
                    var filter = new BackgroundTaskQueue(capacity);
                    return filter;
                };
            });
        });
        hostBuilder
            .ConfigureServices(collection =>
            {
                collection.AddHostedService(p => p.GetRequiredService<ExcelCache>());
                collection.AddHostedService(p => p.GetRequiredService<MediatorService>());
                collection.AddHostedService(p => p.GetRequiredService<PluginCommandManager<PluginCommands>>());
                collection.AddHostedService(p => p.GetRequiredService<HostedUniversalis>());
                collection.AddHostedService(p => p.GetRequiredService<HostedInventoryHistory>());
                collection.AddHostedService(p => p.GetRequiredService<InventorySortingUI>());
                collection.AddHostedService(p => p.GetRequiredService<WindowService>());
                collection.AddHostedService(p => p.GetRequiredService<ConfigurationManagerService>());
                collection.AddHostedService(p => p.GetRequiredService<ItemRequestService>());
                collection.AddHostedService(p => p.GetRequiredService<PluginLogic>());
            });
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Service.Log.Debug("Starting dispose of InventoryToolsPlugin");
            _service?.Dispose();
            _service = null;
            PluginInterface = null;
        }
    }

    public new void Dispose()
    {
        Dispose(true);
        base.Dispose();
        GC.SuppressFinalize(this);
    }

    public override void ConfigureContainer(ContainerBuilder containerBuilder)
    {
    }

    public override void ConfigureServices(IServiceCollection serviceCollection)
    {
    }
}
