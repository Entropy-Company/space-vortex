using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace Content.Client.Research.UI
{
    public sealed class TechTreeAreaControl : Control
    {
        public event Action<GUIBoundKeyEventArgs>? OnMouseDown;
        public event Action<GUIBoundKeyEventArgs>? OnMouseUp;
        public event Action<GUIMouseMoveEventArgs>? OnMouseMove;

        public TechTreeAreaControl()
        {
            MouseFilter = MouseFilterMode.Stop;
        }

        protected override void MouseMove(GUIMouseMoveEventArgs args)
        {
            base.MouseMove(args);
            OnMouseMove?.Invoke(args);
        }

        protected override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            base.KeyBindDown(args);
            if (args.Function == EngineKeyFunctions.UIClick)
                OnMouseDown?.Invoke(args);
        }

        protected override void KeyBindUp(GUIBoundKeyEventArgs args)
        {
            base.KeyBindUp(args);
            if (args.Function == EngineKeyFunctions.UIClick)
                OnMouseUp?.Invoke(args);
        }
    }
}
