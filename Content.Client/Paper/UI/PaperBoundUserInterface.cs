using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using Content.Shared.Paper;
using static Content.Shared.Paper.PaperComponent;
using Robust.Client.GameObjects;
using Content.Shared._Eternal.Paper;
using Robust.Client.Player;
using Content.Shared.Hands.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Input;
using Robust.Client.Input;
using Content.Shared._Eternal.Paper;

namespace Content.Client.Paper.UI;

[UsedImplicitly]
public sealed class PaperBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private PaperWindow? _window;

    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IUltravioletFlashlightSystem _ultravioletFlashlight = default!;

    public PaperBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<PaperWindow>();
        _window.OnSaved += InputOnTextEntered;

        if (EntMan.TryGetComponent<PaperComponent>(Owner, out var paper))
        {
            _window.MaxInputLength = paper.ContentSize;
        }
        if (EntMan.TryGetComponent<PaperVisualsComponent>(Owner, out var visuals))
        {
            _window.InitVisuals(Owner, visuals);
        }

        // Проверяем УФ фонарик только при открытии меню
        CheckUltravioletFlashlight();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is PaperBoundUserInterfaceState paperState)
        {
            _window?.Populate(paperState);
            _window?.SetUltravioletMode(paperState.IsUltravioletMode);
        }
    }

    /// <summary>
    /// Проверяет, держит ли игрок УФ фонарик в руках
    /// </summary>
    private void CheckUltravioletFlashlight()
    {
        if (_window == null) return;

        var player = _playerManager.LocalPlayer?.ControlledEntity;
        if (player == null) return;

        var isUltravioletMode = false;
        if (_entityManager.TryGetComponent<HandsComponent>(player.Value, out var hands))
        {
            foreach (var hand in hands.Hands.Values)
            {
                if (hand.HeldEntity != null && _ultravioletFlashlight.IsUltravioletFlashlightWorking(hand.HeldEntity.Value))
                {
                    isUltravioletMode = true;
                    break;
                }
            }
        }

        // Устанавливаем УФ режим
        _window.SetUltravioletMode(isUltravioletMode);
    }

    private void InputOnTextEntered(string text)
    {
        SendMessage(new PaperInputTextMessage(text));

        if (_window != null)
        {
            _window.Input.TextRope = Rope.Leaf.Empty;
            _window.Input.CursorPosition = new TextEdit.CursorPos(0, TextEdit.LineBreakBias.Top);
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _window?.Dispose();
        }
    }
}
