using Content.Shared._Eternal.Overlays;
using Content.Shared.GameTicking;
using Content.Shared.Inventory.Events;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._Eternal.Overlays;

public sealed class ColorGlassesSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IEntityManager _entity = default!;

    private ColorGlassesOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay = new ColorGlassesOverlay();

        SubscribeLocalEvent<ColorGlassesComponent, ComponentInit>(OnGlassesInit);
        SubscribeLocalEvent<ColorGlassesComponent, ComponentShutdown>(OnGlassesShutdown);
        SubscribeLocalEvent<ColorGlassesComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ColorGlassesComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnGlassesInit(EntityUid uid, ColorGlassesComponent component, ComponentInit args)
    {
        if (_player.LocalEntity == uid)
            _overlayMan.AddOverlay(_overlay);
    }

    private void OnGlassesShutdown(EntityUid uid, ColorGlassesComponent component, ComponentShutdown args)
    {
        if (_player.LocalEntity == uid)
            _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnPlayerAttached(EntityUid uid, ColorGlassesComponent component, LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, ColorGlassesComponent component, LocalPlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
    }
}
