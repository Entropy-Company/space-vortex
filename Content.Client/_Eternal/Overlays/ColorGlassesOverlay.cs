using Content.Shared._Eternal.Overlays;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using System.Numerics;
using Robust.Shared.Maths;

namespace Content.Client._Eternal.Overlays;

public class ColorGlassesOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEntityManager _entity = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    private readonly ShaderInstance _shader;

    public ColorGlassesOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototype.Index<ShaderPrototype>("ColorGlasses").Instance().Duplicate();
        ZIndex = 100;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture is null
            || _player.LocalEntity is null
            || !_entity.TryGetComponent<ColorGlassesComponent>(_player.LocalEntity.Value, out var component))
            return;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        var tintVec = component.TintVec;
        _shader.SetParameter("tint", tintVec);
        var worldHandle = args.WorldHandle;
        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(_shader);
        worldHandle.DrawRect(args.WorldBounds, Color.White);
        worldHandle.UseShader(null);
    }
}
