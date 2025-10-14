using Content.Shared.Phasing;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using Robust.Shared.Maths;
using Robust.Shared.GameStates;
using System.Collections.Generic;
using System.Linq;

namespace Content.Client;

public sealed class PhasingSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private ShaderPrototype _shaderPrototype = default!;

    // Словарь для хранения уникальных экземпляров шейдеров для каждой сущности
    private readonly Dictionary<EntityUid, ShaderInstance> _shaderInstances = new();

    // Кэш параметров для оптимизации - избегаем лишних вызовов SetParameter
    private readonly Dictionary<EntityUid, PhasingParameters> _parameterCache = new();

    // Счетчик кадров для оптимизации обновлений
    private int _frameCounter = 0;
    private const int UPDATE_FREQUENCY = 2; // Обновляем параметры каждые 2 кадра

    // Ограничения для предотвращения лагов
    private const int MAX_SHADER_INSTANCES = 1000; // Максимальное количество экземпляров шейдеров
    private const int MAX_UPDATES_PER_FRAME = 50; // Максимальное количество обновлений за кадр
    private const int WARNING_THRESHOLD = 500; // Порог для предупреждения о производительности

    public override void Initialize()
    {
        base.Initialize();
        _shaderPrototype = _protoMan.Index<ShaderPrototype>("Phasing");
        SubscribeLocalEvent<PhasingComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PhasingComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<PhasingComponent, BeforePostShaderRenderEvent>(OnShaderRender);
        SubscribeLocalEvent<PhasingComponent, ComponentHandleState>(OnHandleState);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Проверяем количество активных шейдеров
        if (_shaderInstances.Count > WARNING_THRESHOLD)
        {
            Logger.Warning($"PhasingSystem: Большое количество активных шейдеров ({_shaderInstances.Count}). Это может влиять на производительность.");
        }

        // Если слишком много шейдеров, переключаемся в режим экономии ресурсов
        if (_shaderInstances.Count > MAX_SHADER_INSTANCES)
        {
            Logger.Error($"PhasingSystem: Превышен лимит шейдеров ({_shaderInstances.Count}/{MAX_SHADER_INSTANCES}). Отключаем новые эффекты.");
            return;
        }

        // Оптимизация: обновляем параметры не каждый кадр, а с определенной частотой
        _frameCounter++;
        if (_frameCounter % UPDATE_FREQUENCY != 0)
            return;

        // Ограничиваем количество обновлений за один кадр для предотвращения лагов
        int updateCount = 0;

        // Оптимизация: обрабатываем только видимые сущности
        var visibleEntities = EntityManager.EntityQuery<PhasingComponent, SpriteComponent>()
            .Where(x => x.Item2.Visible)
            .Take(MAX_UPDATES_PER_FRAME);

        foreach (var (comp, sprite) in visibleEntities)
        {
            if (updateCount >= MAX_UPDATES_PER_FRAME)
                break;

            if (_shaderInstances.TryGetValue(comp.Owner, out var shaderInstance) && sprite.PostShader == shaderInstance)
            {
                // Проверяем, изменились ли параметры
                if (HasParametersChanged(comp.Owner, comp))
                {
                    ApplyShaderParams(comp, shaderInstance);
                    UpdateParameterCache(comp.Owner, comp);
                    updateCount++;
                }
            }
        }
    }

    private void OnStartup(EntityUid uid, PhasingComponent component, ComponentStartup args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        // Проверяем лимит шейдеров
        if (_shaderInstances.Count >= MAX_SHADER_INSTANCES)
        {
            Logger.Warning($"PhasingSystem: Не удалось создать шейдер для {uid} - превышен лимит.");
            return;
        }

        // Создаем уникальный экземпляр шейдера для этой сущности
        var shaderInstance = _shaderPrototype.InstanceUnique();
        _shaderInstances[uid] = shaderInstance;

        // Инициализируем кэш параметров
        UpdateParameterCache(uid, component);

        ApplyShaderParams(component, shaderInstance);
        sprite.PostShader = shaderInstance;
        sprite.GetScreenTexture = false;
        sprite.RaiseShaderEvent = true;
    }

    private void OnShutdown(EntityUid uid, PhasingComponent component, ComponentShutdown args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        // Убираем шейдер
        if (_shaderInstances.TryGetValue(uid, out var shaderInstance) && sprite.PostShader == shaderInstance)
            sprite.PostShader = null;

        // Освобождаем ресурсы шейдера и удаляем из словарей
        if (_shaderInstances.TryGetValue(uid, out var instance))
        {
            instance.Dispose();
            _shaderInstances.Remove(uid);
        }

        _parameterCache.Remove(uid);
    }

    private void OnShaderRender(EntityUid uid, PhasingComponent component, BeforePostShaderRenderEvent args)
    {
        // При рендеринге применяем параметры только если они изменились
        if (_shaderInstances.TryGetValue(uid, out var shaderInstance) && HasParametersChanged(uid, component))
        {
            ApplyShaderParams(component, shaderInstance);
            UpdateParameterCache(uid, component);
        }
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
            RestartShader(uid, sprite, component);
        }
    }

    private void ApplyShaderParams(PhasingComponent component, ShaderInstance shaderInstance)
    {
        shaderInstance.SetParameter("bandMin", component.BandMin);
        shaderInstance.SetParameter("bandMax", component.BandMax);
        shaderInstance.SetParameter("animationSpeed", component.AnimationSpeed);
        shaderInstance.SetParameter("distortionStrength", component.DistortionStrength);
        shaderInstance.SetParameter("glitchFrequency", component.GlitchFrequency);
        shaderInstance.SetParameter("bandSplitStrength", component.BandSplitStrength);
        shaderInstance.SetParameter("bandSplitFrequency", component.BandSplitFrequency);
    }

    /// <summary>
    /// Проверяет, изменились ли параметры компонента с момента последнего кэширования
    /// </summary>
    private bool HasParametersChanged(EntityUid uid, PhasingComponent component)
    {
        if (!_parameterCache.TryGetValue(uid, out var cached))
            return true;

        return cached.AnimationSpeed != component.AnimationSpeed ||
               cached.DistortionStrength != component.DistortionStrength ||
               cached.BandMin != component.BandMin ||
               cached.BandMax != component.BandMax ||
               cached.GlitchFrequency != component.GlitchFrequency ||
               cached.BandSplitStrength != component.BandSplitStrength ||
               cached.BandSplitFrequency != component.BandSplitFrequency;
    }

    /// <summary>
    /// Обновляет кэш параметров для указанной сущности
    /// </summary>
    private void UpdateParameterCache(EntityUid uid, PhasingComponent component)
    {
        _parameterCache[uid] = new PhasingParameters
        {
            AnimationSpeed = component.AnimationSpeed,
            DistortionStrength = component.DistortionStrength,
            BandMin = component.BandMin,
            BandMax = component.BandMax,
            GlitchFrequency = component.GlitchFrequency,
            BandSplitStrength = component.BandSplitStrength,
            BandSplitFrequency = component.BandSplitFrequency
        };
    }

    /// <summary>
    /// Перезапускает шейдер на указанной сущности
    /// </summary>
    public void RestartShader(EntityUid uid, SpriteComponent sprite, PhasingComponent component)
    {
        // Временно убираем шейдер
        sprite.PostShader = null;

        // Освобождаем старый экземпляр шейдера
        if (_shaderInstances.TryGetValue(uid, out var oldInstance))
        {
            oldInstance.Dispose();
        }

        // Создаем новый экземпляр шейдера
        var newInstance = _shaderPrototype.InstanceUnique();
        _shaderInstances[uid] = newInstance;

        // Обновляем кэш параметров
        UpdateParameterCache(uid, component);

        // Применяем параметры
        ApplyShaderParams(component, newInstance);

        // Возвращаем шейдер
        sprite.PostShader = newInstance;
    }

    /// <summary>
    /// Получает статистику использования шейдеров
    /// </summary>
    public (int activeShaders, int cachedParameters) GetShaderStats()
    {
        return (_shaderInstances.Count, _parameterCache.Count);
    }

    /// <summary>
    /// Очищает все шейдеры (для отладки или при критических проблемах с производительностью)
    /// </summary>
    public void ClearAllShaders()
    {
        foreach (var (uid, shaderInstance) in _shaderInstances)
        {
            if (TryComp(uid, out SpriteComponent? sprite))
            {
                sprite.PostShader = null;
            }
            shaderInstance.Dispose();
        }

        _shaderInstances.Clear();
        _parameterCache.Clear();

        Logger.Warning("PhasingSystem: Все шейдеры очищены.");
    }
}

/// <summary>
/// Структура для кэширования параметров шейдера
/// </summary>
public struct PhasingParameters
{
    public float AnimationSpeed;
    public float DistortionStrength;
    public float BandMin;
    public float BandMax;
    public float GlitchFrequency;
    public float BandSplitStrength;
    public float BandSplitFrequency;
}
