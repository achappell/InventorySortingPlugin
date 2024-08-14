using System;
using System.Configuration;
using System.Linq;
using System.Numerics;
using CriticalCommonLib.Extensions;
using CriticalCommonLib.Services;
using CriticalCommonLib.Services.Mediator;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using ImGuiNET;
using InventorySorting.Mediator;
using InventorySorting.UI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InventorySorting.Windows;

public class MainWindow : GenericWindow
{
    private string GoatImagePath;
    private Plugin Plugin;
    public Configuration Configuration { get; }

    public override string GenericKey => "main";

    public override string GenericName { get; } = "Main";

    public override bool DestroyOnClose => true;

    public override bool SaveState => false;

    public override Vector2? DefaultSize { get; } = new Vector2(500, 800);
    public override Vector2? MaxSize => new(800, 1500);
    public override Vector2? MinSize => new(100, 100);

    // We give this window a hidden ID using ##
    // So that the user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(ILogger<MainWindow> logger, MediatorService mediator, Configuration configuration)
        : base(logger, mediator, configuration)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        Configuration = configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        ImGui.Spacing();

        ImGui.Text(InventoryUtils.GearSetName());

        ImGui.Spacing();

        if (ImGui.Button("What's in Bag 1"))
        {
            MediatorService.Publish(new ItemSearchRequestedMessage());
        }

        if (ImGui.Button("Open Retainer"))
        {
            MediatorService.Publish(new StoreOnRetainerMessage());
        }
    }

    public override void Initialize()
    {
        
    }

    public override void Invalidate()
    {
        
    }
}

internal sealed class InventoryUtils
{
    internal static unsafe String GearSetName()
    {
        var gearSetModule = RaptureGearsetModule.Instance();
        var gearset = gearSetModule->GetGearset(10);
        return gearset->GetItem(RaptureGearsetModule.GearsetItemIndex.MainHand).ToString();
    }
}

