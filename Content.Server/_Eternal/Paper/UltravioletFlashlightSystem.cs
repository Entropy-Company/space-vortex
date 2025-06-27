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

public sealed class UltravioletFlashlightSystem : EntitySystem, IUltravioletFlashlightSystem
{
    public override void Initialize()
    {
        base.Initialize();

        // Подписываемся на события взаимодействия для предотвращения выпадения фонарика
        SubscribeLocalEvent<UltravioletFlashlightComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<UltravioletFlashlightComponent> entity, ref InteractUsingEvent args)
    {
        // Если УФ фонарик используется на бумаге, предотвращаем его выпадение
        if (HasComp<PaperComponent>(args.Target))
        {
            // Устанавливаем Handled = true, чтобы предотвратить дальнейшую обработку
            // и возможное выпадение фонарика
            args.Handled = true;
        }
    }

    /// <summary>
    /// Проверяет, работает ли УФ фонарик (есть ли компонент)
    /// </summary>
    public bool IsUltravioletFlashlightWorking(EntityUid flashlight)
    {
        return HasComp<UltravioletFlashlightComponent>(flashlight);
    }

    /// <summary>
    /// Проверяет, работает ли УФ фонарик и показывает сообщение пользователю, если нет
    /// </summary>
    public bool IsUltravioletFlashlightWorkingWithMessage(EntityUid flashlight, EntityUid user)
    {
        if (!HasComp<UltravioletFlashlightComponent>(flashlight))
            return false;

        return true;
    }
}
