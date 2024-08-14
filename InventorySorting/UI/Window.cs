using System.Numerics;
using System.Windows.Forms.Design;
using CriticalCommonLib.Services.Mediator;
using ImGuiNET;
using Microsoft.Extensions.Logging;

// ReSharper disable VirtualMemberCallInConstructor

namespace InventorySorting.UI
{
    public abstract class Window : WindowMediatorSubscriberBase, IWindow
    {
        public Configuration Configuration { get; }

        public Window(ILogger logger, MediatorService mediator, Configuration configuration, string name = "") : base(logger, mediator, name)
        {
            Configuration = configuration;
            if (MinSize != null && MaxSize != null)
            {
                SizeConstraints = new WindowSizeConstraints()
                {
                    MinimumSize = MinSize.Value,
                    MaximumSize = MaxSize.Value
                };
            }

            SizeCondition = ImGuiCond.FirstUseEver;
            if (DefaultSize != null)
            {
                Size = DefaultSize.Value;
            }
        }

        public override void OnOpen()
        {
            Opened?.Invoke(this);
        }

        public override void OnClose()
        {
            Closed?.Invoke(this);
        }

        public void Close()
        {
            IsOpen = false;
        }

        public void Open()
        {
            IsOpen = true;
        }

        public abstract void Invalidate();
        public void SetPosition(Vector2 newPosition, bool isAppearing)
        {
            Position = newPosition;
            if (isAppearing)
            {
                PositionCondition = ImGuiCond.Appearing;
            }
        }

        public event IWindow.ClosedDelegate? Closed;
        public event IWindow.OpenedDelegate? Opened;

        public string Key { get; set; }
        public abstract string GenericKey { get; }
        public abstract string GenericName { get; }

        public abstract bool DestroyOnClose { get; }
        public virtual bool SavePosition { get; }
        public virtual Vector2 CurrentPosition { get; set; }

        public abstract bool SaveState { get; }

        public abstract Vector2? DefaultSize { get; }
        public abstract Vector2? MaxSize { get; }
        public abstract Vector2? MinSize { get; }
        public string OriginalWindowName;
    }
}
