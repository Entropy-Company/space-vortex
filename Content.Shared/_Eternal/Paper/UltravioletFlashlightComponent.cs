using Robust.Shared.GameStates;

namespace Content.Shared._Eternal.Paper;

/// <summary>
/// Компонент для УФ фонарика. Используется как маркер в PaperSystem
/// для определения, что бумага должна открываться в УФ режиме.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class UltravioletFlashlightComponent : Component
{
}
