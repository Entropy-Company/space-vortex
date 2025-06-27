using Content.Shared.Light.Components;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Maths;
using Content.Shared._Eternal.Paper;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client._Eternal.Paper;

public sealed class UltravioletFlashlightSystem : EntitySystem, IUltravioletFlashlightSystem
{
    public override void Initialize()
    {
        base.Initialize();
    }

    /// <summary>
    /// Проверяет, работает ли УФ фонарик (есть ли компонент)
    /// </summary>
    public bool IsUltravioletFlashlightWorking(EntityUid flashlight)
    {
        return HasComp<UltravioletFlashlightComponent>(flashlight);
    }

    /// <summary>
    /// Клиентская заглушка: просто вызывает обычную проверку, сообщений не показывает
    /// </summary>
    public bool IsUltravioletFlashlightWorkingWithMessage(EntityUid flashlight, EntityUid user)
    {
        return IsUltravioletFlashlightWorking(flashlight);
    }
}
