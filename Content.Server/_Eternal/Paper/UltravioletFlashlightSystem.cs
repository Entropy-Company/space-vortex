using Content.Shared.Light.Components;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Maths;
using Content.Shared._Eternal.Paper;
using Robust.Server.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Server._Eternal.Paper;

public sealed class UltravioletFlashlightSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        // УФ фонарик работает только как маркер для PaperSystem
        // Логика обработки находится в PaperSystem
    }
}
