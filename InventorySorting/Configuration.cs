using Dalamud.Configuration;
using System;
using System.Collections.Generic;
using System.Numerics;
using CriticalCommonLib.Models;

namespace InventorySorting;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    public bool IsDirty { get; set; }
    public bool IsConfigWindowMovable { get; set; } = true;
    public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;
    public bool SaveBackgroundFilter { get; set; } = false;
    public string? ActiveBackgroundFilter { get; set; } = null;
    private bool _isVisible;
    private HashSet<string>? _openWindows = new();
    private Dictionary<string, Vector2>? _savedWindowPositions = new();
    private Dictionary<ulong, HashSet<uint>> _acquiredItems = new();
    private bool _historyEnabled = false;
    public Dictionary<ulong, Character> SavedCharacters = new();
    private List<InventoryChangeReason> _historyTrackReasons = new();
    public bool AutoSave { get; set; } = true;
    public int AutoSaveMinutes { get; set; } = 10;
    private bool _automaticallyDownloadMarketPrices = false;
    public void MarkReloaded()
    {
        if (!SaveBackgroundFilter)
        {
            ActiveBackgroundFilter = null;
        }
    }

    public void ClearDirtyFlags()
    {
        this.IsDirty = false;
    }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            _isVisible = value;
            IsDirty = true;
        }
    }

    public HashSet<string> OpenWindows
    {
        get
        {
            if (_openWindows == null)
            {
                _openWindows = new HashSet<string>();
            }
            return _openWindows;
        }
        set => _openWindows = value;
    }

    public Dictionary<string, Vector2> SavedWindowPositions
    {
        get
        {
            if (_savedWindowPositions == null)
            {
                _savedWindowPositions = new Dictionary<string, Vector2>();
            }
            return _savedWindowPositions;
        }
        set => _savedWindowPositions = value;
    }

    public Dictionary<ulong, HashSet<uint>> AcquiredItems
    {
        get => _acquiredItems ??= new Dictionary<ulong, HashSet<uint>>();
        set => _acquiredItems = value;
    }

    public bool HistoryEnabled
    {
        get => _historyEnabled;
        set
        {
            _historyEnabled = value;
            IsDirty = true;
        }
    }

    public List<InventoryChangeReason> HistoryTrackReasons
    {
        get
        {
            return _historyTrackReasons;
        }
        set
        {
            _historyTrackReasons = value;
            IsDirty = true;
        }
    }

    public bool AutomaticallyDownloadMarketPrices
    {
        get => _automaticallyDownloadMarketPrices;
        set
        {
            _automaticallyDownloadMarketPrices = value;
            IsDirty = true;
        }
    }
}
