namespace Content.Shared._Eternal.Paper;

/// <summary>
/// Интерфейс для системы УФ фонарика
/// </summary>
public interface IUltravioletFlashlightSystem
{
    /// <summary>
    /// Проверяет, работает ли УФ фонарик (есть ли батарея и заряд)
    /// </summary>
    bool IsUltravioletFlashlightWorking(EntityUid flashlight);

    /// <summary>
    /// Проверяет, работает ли УФ фонарик и показывает сообщение пользователю, если нет
    /// </summary>
    bool IsUltravioletFlashlightWorkingWithMessage(EntityUid flashlight, EntityUid user);
}
