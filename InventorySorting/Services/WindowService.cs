using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CriticalCommonLib.Services.Mediator;
using DalaMock.Host.Factories;
using DalaMock.Shared.Interfaces;
using Dalamud.Interface.Windowing;
using InventorySorting.Mediator;
using InventorySorting.UI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Window = InventorySorting.UI.Window;

namespace InventorySorting.Services
{
    public class WindowService : DisposableMediatorSubscriberBase, IHostedService
    {
        private readonly IWindowSystem _windowSystem;

        private readonly Func<Type, GenericWindow> _genericWindowFactory;
        private readonly Configuration _configuration;
        private readonly MediatorService _mediatorService;

        public WindowService(ILogger<WindowService> logger, MediatorService mediatorService, IEnumerable<Window> windows, Func<Type, GenericWindow> genericWindowFactory, Configuration configuration, IWindowSystemFactory windowSystemFactory) : base(logger, mediatorService)
        {
            _windowSystem = windowSystemFactory.Create("AllaganTools");
            _windows = windows.ToDictionary(c => c.GetType(), c => c);
            _genericWindowFactory = genericWindowFactory;
            _configuration = configuration;
            _mediatorService = mediatorService;
        }

        private void GenericWindowMessage(OpenGenericWindowMessage obj)
        {
            OpenWindow(obj.windowType);
        }

        public void UpdateRespectCloseHotkey(Type windowType, bool newSetting)
        {
            foreach (var window in _allWindows)
            {
                if (window.GetType() == windowType)
                {
                    window.RespectCloseHotkey = newSetting;
                }
            }
        }

        private List<IWindow> _allWindows = new();
        private ConcurrentDictionary<Type, IWindow> _genericWindows = new();

        private MethodInfo? _openWindowMethod;
        private readonly Dictionary<Type, Window> _windows;


        private void RestoreSavedWindows()
        {
            var openWindows = _configuration.OpenWindows;
            _configuration.OpenWindows = new HashSet<string>();
            foreach (var openWindow in openWindows)
            {
                Assembly asm = typeof(WindowService).Assembly;
                Type? type = asm.GetType(openWindow);

                if (type != null)
                {
                    try
                    {
                        var newWindow = _genericWindowFactory.Invoke(type);
                        AddWindow(newWindow);
                        if (newWindow.SaveState)
                        {
                            newWindow.Open();
                        }

                    }
                    catch (Exception e)
                    {
                        Logger.LogError("Could not load saved window. Perhaps it was removed.");
                    }
                }
            }
        }

        public IWindowSystem WindowSystem => _windowSystem;

        public T GetWindow<T>() where T : GenericWindow
        {
            if (_genericWindows.ContainsKey(typeof(T)))
            {
                return (T)_genericWindows[typeof(T)];
            }
            ;
            var newWindow = _genericWindowFactory.Invoke(typeof(T));
            AddWindow(newWindow);
            return (T)newWindow;
        }

        public GenericWindow GetWindow(Type type)
        {
            if (_genericWindows.ContainsKey(type))
            {
                return (GenericWindow)_genericWindows[type];
            }
            ;
            var newWindow = _genericWindowFactory.Invoke(type);
            AddWindow(type, newWindow);
            return newWindow;
        }

        public bool ToggleWindow<T>() where T : GenericWindow
        {
            GetWindow<T>().Toggle();
            return true;
        }

        public bool ToggleWindow(Type window)
        {
            GetWindow(window).Toggle();
            return true;
        }

        public bool OpenWindow(Type type, bool refocus = true)
        {
            var window = GetWindow(type);
            if (window.IsOpen)
            {
                window.BringToFront();
            }
            else
            {
                window.Open();
            }
            return true;
        }
        public bool OpenWindow<T>(bool refocus = true) where T : GenericWindow
        {
            var window = GetWindow<T>();
            if (window.IsOpen)
            {
                window.BringToFront();
            }
            else
            {
                window.Open();
            }

            return true;
        }

        private bool CloseWindow(Type windowType)
        {
            if (_genericWindows.ContainsKey(windowType))
            {
                _genericWindows[windowType].Close();
                return true;
            }
            return false;
        }

        private bool CloseWindows()
        {
            foreach (var window in _allWindows)
            {
                window.Close();
            }

            return true;
        }

        private bool CloseWindows(Type type)
        {
            foreach (var window in _allWindows)
            {
                if (type.IsInstanceOfType(window))
                {
                    window.Close();
                }
            }

            return true;
        }

        private bool AddWindow(Type windowType, GenericWindow window)
        {
            window.Logger = Logger;
            if (_genericWindows.TryAdd(windowType, window))
            {
                _allWindows.Add(window);
                _windowSystem.AddWindow(window);
                window.Closed += WindowOnClosed;
                window.Opened += WindowOnOpened;
                return true;
            }
            return false;
        }

        private bool AddWindow<T>(T window) where T : GenericWindow
        {
            window.Logger = Logger;
            if (_genericWindows.TryAdd(window.GetType(), window))
            {
                _allWindows.Add(window);
                _windowSystem.AddWindow(window);
                window.Closed += WindowOnClosed;
                window.Opened += WindowOnOpened;
                return true;
            }
            return false;
        }

        private void WindowOnOpened(IWindow window)
        {
            if (window.SaveState && !_configuration.OpenWindows.Contains(window.GetType().ToString()))
            {
                _configuration.OpenWindows.Add(window.GetType().ToString());
                _configuration.IsDirty = true;
            }
            if (window.SaveState && window.SavePosition)
            {
                if (_configuration.SavedWindowPositions.ContainsKey(window.GetType().ToString()))
                {
                    window.SetPosition(_configuration.SavedWindowPositions[window.GetType().ToString()], true);
                    _configuration.IsDirty = true;
                }
            }
        }

        private void WindowOnClosed(IWindow window)
        {
            if (window.SaveState && _configuration.OpenWindows.Contains(window.GetType().ToString()))
            {
                _configuration.OpenWindows.Remove(window.GetType().ToString());
                _configuration.IsDirty = true;
            }

            if (window.SaveState && window.SavePosition)
            {
                bool hasOtherWindowOpen = false;
                //Check to see if there are any other instances of the window open, if so don't save the one that was just closed's position
                foreach (var openWindow in _allWindows)
                {
                    if (window != openWindow && window.Key == openWindow.GenericKey &&
                        window.IsOpen)
                    {
                        hasOtherWindowOpen = true;
                    }
                }

                if (hasOtherWindowOpen == false)
                {
                    _configuration.SavedWindowPositions[window.GetType().ToString()] = window.CurrentPosition;
                    _configuration.IsDirty = true;
                }

            }

            if (window.DestroyOnClose)
            {
                RemoveWindow(window);
            }
        }

        public void RemoveWindow(IWindow window)
        {
            _allWindows.Remove(window);
            if (window is GenericWindow genericWindow)
            {
                _genericWindows.Remove(genericWindow.GetType(), out _);
            }

            if (window is Window actualWindow)
            {
                WindowSystem.RemoveWindow(actualWindow);
            }
            window.Dispose();
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            foreach (var window in _allWindows)
            {
                window.Opened -= WindowOnOpened;
                window.Closed -= WindowOnClosed;
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Logger.LogTrace("Starting service {type} ({this})", GetType().Name, this);
            _mediatorService.Subscribe(this, new Action<ToggleGenericWindowMessage>(ToggleGenericWindow));
            _mediatorService.Subscribe(this, new Action<OpenGenericWindowMessage>(GenericWindowMessage));

            return Task.CompletedTask;
        }

        private void ToggleGenericWindow(ToggleGenericWindowMessage obj)
        {
            ToggleWindow(obj.windowType);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogTrace("Stopping service {type} ({this})", GetType().Name, this);
            return Task.CompletedTask;
        }
    }
}
