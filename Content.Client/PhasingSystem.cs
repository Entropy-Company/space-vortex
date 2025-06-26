using Content.Shared.Phasing;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using Robust.Shared.Maths;
using Robust.Shared.GameStates;

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
        SubscribeLocalEvent<PhasingComponent, ComponentHandleState>(OnHandleState);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        foreach (var (comp, sprite) in EntityManager.EntityQuery<PhasingComponent, SpriteComponent>())
        {
            if (sprite.PostShader == _shader)
                ApplyShaderParams(comp);
        }
    }

    private void OnStartup(EntityUid uid, PhasingComponent component, ComponentStartup args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        ApplyShaderParams(component);
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
        ApplyShaderParams(component);
    }

    private void OnHandleState(EntityUid uid, PhasingComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not PhasingComponentState state)
            return;

        // Проверяем, изменились ли параметры
        bool needsRestart = component.AnimationSpeed != state.AnimationSpeed ||
                           component.DistortionStrength != state.DistortionStrength ||
                           component.BandMin != state.BandMin ||
                           component.BandMax != state.BandMax ||
                           component.GlitchFrequency != state.GlitchFrequency ||
                           component.BandSplitStrength != state.BandSplitStrength ||
                           component.BandSplitFrequency != state.BandSplitFrequency;

        // Обновляем параметры
        component.AnimationSpeed = state.AnimationSpeed;
        component.DistortionStrength = state.DistortionStrength;
        component.BandMin = state.BandMin;
        component.BandMax = state.BandMax;
        component.GlitchFrequency = state.GlitchFrequency;
        component.BandSplitStrength = state.BandSplitStrength;
        component.BandSplitFrequency = state.BandSplitFrequency;

        // Если параметры изменились, перезапускаем шейдер
        if (needsRestart && TryComp(uid, out SpriteComponent? sprite))
        {
            RestartShader(uid, sprite);
        }
    }

    private void ApplyShaderParams(PhasingComponent component)
    {
        _shader.SetParameter("bandMin", component.BandMin);
        _shader.SetParameter("bandMax", component.BandMax);
        _shader.SetParameter("animationSpeed", component.AnimationSpeed);
        _shader.SetParameter("distortionStrength", component.DistortionStrength);
        _shader.SetParameter("glitchFrequency", component.GlitchFrequency);
        _shader.SetParameter("bandSplitStrength", component.BandSplitStrength);
        _shader.SetParameter("bandSplitFrequency", component.BandSplitFrequency);
    }

    /// <summary>
    /// Перезапускает шейдер на указанной сущности
    /// </summary>
    public void RestartShader(EntityUid uid, SpriteComponent sprite)
    {
        // Временно убираем шейдер
        sprite.PostShader = null;

        // Применяем параметры
        if (TryComp(uid, out PhasingComponent? component))
        {
            ApplyShaderParams(component);
        }

        // Возвращаем шейдер
        sprite.PostShader = _shader;
    }
}
