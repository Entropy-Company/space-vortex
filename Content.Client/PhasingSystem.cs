using Content.Shared.Phasing;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using Robust.Shared.Maths;

namespace Content.Client;

public sealed class PhasingSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private ShaderInstance _shader = default!;

    public override void Initialize()
    {
        base.Initialize();
        _shader = _protoMan.Index<ShaderPrototype>("Phasing").InstanceUnique();
        SubscribeLocalEvent<PhasingComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PhasingComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<PhasingComponent, BeforePostShaderRenderEvent>(OnShaderRender);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        foreach (var (comp, sprite) in EntityManager.EntityQuery<PhasingComponent, SpriteComponent>())
        {
            if (sprite.PostShader == _shader)
                ApplyShaderParams();
        }
    }

    private void OnStartup(EntityUid uid, PhasingComponent component, ComponentStartup args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;
        ApplyShaderParams();
        sprite.PostShader = _shader;
        sprite.GetScreenTexture = false;
        sprite.RaiseShaderEvent = true;
    }

    private void OnShutdown(EntityUid uid, PhasingComponent component, ComponentShutdown args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;
        if (sprite.PostShader == _shader)
            sprite.PostShader = null;
    }

    private void OnShaderRender(EntityUid uid, PhasingComponent component, BeforePostShaderRenderEvent args)
    {
        ApplyShaderParams();
    }

    private void ApplyShaderParams()
    {
        _shader.SetParameter("bandMin", 3.0f);
        _shader.SetParameter("bandMax", 8.0f);
    }
}
