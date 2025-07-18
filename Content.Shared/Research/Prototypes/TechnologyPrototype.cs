using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Maths;
using System.Numerics;

namespace Content.Shared.Research.Prototypes;

/// <summary>
/// This is a prototype for a technology that can be unlocked.
/// </summary>
[Prototype]
public sealed partial class TechnologyPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The name of the technology.
    /// Supports locale strings
    /// </summary>
    [DataField(required: true)]
    public LocId Name = string.Empty;

    /// <summary>
    /// An icon used to visually represent the technology in UI.
    /// </summary>
    [DataField(required: true)]
    public SpriteSpecifier Icon = default!;

    /// <summary>
    /// What research discipline this technology belongs to.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<TechDisciplinePrototype> Discipline;

    /// <summary>
    /// Hidden tech is not ever available at the research console.
    /// </summary>
    [DataField]
    public bool Hidden;

    /// <summary>
    /// How much research is needed to unlock.
    /// </summary>
    [DataField]
    public int Cost = 10000;

    /// <summary>
    /// A list of technologies that must be unlocked before this technology can be researched.
    /// </summary>
    [DataField]
    public List<ProtoId<TechnologyPrototype>> RequiredTech = new();

    /// <summary>
    /// A list of <see cref="LatheRecipePrototype"/>s that are unlocked by this technology
    /// </summary>
    [DataField]
    public List<ProtoId<LatheRecipePrototype>> RecipeUnlocks = new();

    /// <summary>
    /// A list of non-standard effects that are done when this technology is unlocked.
    /// </summary>
    [DataField]
    public IReadOnlyList<GenericUnlock> GenericUnlocks = new List<GenericUnlock>();

    /// <summary>
    /// Optional position in the tech tree (X, Y). If not set, will be auto-arranged.
    /// </summary>
    [DataField]
    public Vector2? Position { get; private set; }
}

[DataDefinition]
public partial record struct GenericUnlock()
{
    /// <summary>
    /// What event is raised when this is unlocked?
    /// Used for doing non-standard logic.
    /// </summary>
    [DataField]
    public object? PurchaseEvent = null;

    /// <summary>
    /// A player facing tooltip for what the unlock does.
    /// Supports locale strings.
    /// </summary>
    [DataField]
    public string UnlockDescription = string.Empty;
}
