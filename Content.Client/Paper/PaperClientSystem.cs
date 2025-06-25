using Content.Shared.Paper;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.Paper;

public sealed class PaperClientSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PaperComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, PaperComponent component, ref AfterAutoHandleStateEvent args)
    {
        // Update visual state based on paper content
        if (TryComp<SpriteComponent>(uid, out var sprite))
        {
            if (!string.IsNullOrEmpty(component.Content))
            {
                sprite.LayerSetState(0, "paper");
            }
            else
            {
                sprite.LayerSetState(0, "paper_blank");
            }
        }
    }
}
