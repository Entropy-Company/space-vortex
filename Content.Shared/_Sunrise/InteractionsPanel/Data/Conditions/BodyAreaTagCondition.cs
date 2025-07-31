using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Content.Shared.Tag;
using System.Linq;

namespace Content.Shared._Sunrise.InteractionsPanel.Data.Conditions;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class BodyAreaTagCondition : IAppearCondition
{
    [DataField]
    public bool CheckInitiator { get; private set; }

    [DataField]
    public bool CheckTarget { get; private set; } = true;

    [DataField]
    public bool RequireExposed { get; private set; } = true;

    [DataField(required: true)]
    public HashSet<string> Categories { get; private set; } = new();

    public bool IsMet(EntityUid initiator, EntityUid target, EntityManager entityManager)
    {
        if (CheckInitiator && !CheckEntity(initiator, entityManager))
            return false;

        if (CheckTarget && !CheckEntity(target, entityManager))
            return false;

        return true;
    }

    private bool CheckEntity(EntityUid entity, EntityManager entMan)
    {
        if (!entMan.TryGetComponent<ContainerManagerComponent>(entity, out var inventory))
            return RequireExposed;

        var restricted = GetCoveredCategories(entMan, inventory);
        foreach (var category in Categories)
        {
            var isCovered = restricted.Contains(category);

            if (RequireExposed && isCovered)
                return false;

            if (!RequireExposed && !isCovered)
                return false;
        }

        return true;
    }

    private HashSet<string> GetCoveredCategories(EntityManager entMan, ContainerManagerComponent inventory)
    {
        var result = new HashSet<string>();

        foreach (var (slot, container) in inventory.Containers)
        {
            if (container.ContainedEntities.Count == 0)
                continue;

            var ent = container.ContainedEntities[0];

            if (!entMan.TryGetComponent<TagComponent>(ent, out var tags))
                continue;

            result.UnionWith(GetCategoriesBySlotAndTags(slot, tags));
        }

        return result;
    }

    private HashSet<string> GetCategoriesBySlotAndTags(string slot, TagComponent tags)
    {
        var set = new HashSet<string>();
        var tagsSet = new HashSet<string>(tags.Tags.Select(t => t.ToString()));

        switch (slot)
        {
            case "jumpsuit":
                set.UnionWith(new[] { "грудь", "ляжки", "попа" });
                if (tagsSet.Contains("NudeBottom")) set = new() { "грудь" };
                if (tagsSet.Contains("NudeTop")) set = new() { "ляжки", "попа" };
                if (tagsSet.Contains("CommandSuit")) set = new() { "грудь", "ляжки", "попа" };
                break;

            case "outerClothing":
                set.UnionWith(new[] { "грудь", "ляжки", "попа" });
                if (tagsSet.Contains("NudeBottom")) set = new() { "грудь" };
                if (tagsSet.Contains("NudeFull")) set.Clear();
                if (tagsSet.Contains("FullCovered")) set = new() {
                    "щёки", "губы", "шея", "уши", "волосы",
                    "рот", "грудь", "ступни", "ляжки", "попа", "лицо", "хвост", "ладони", "гладкие перчатки"
                };
                if (tagsSet.Contains("FullBodyOuter")) set = new() {
                    "грудь", "ступни", "ляжки", "попа", "шея", "ладони", "гладкие перчатки"
                };
                break;

            case "head":
                set.UnionWith(new[] { "волосы" });
                if (tagsSet.Contains("TopCovered")) set = new() { "уши", "волосы" };
                if (tagsSet.Contains("FullCovered")) set = new() { "уши", "волосы", "рот", "лицо", "губы", "щёки" };
                break;

            case "gloves":
                set.UnionWith(new[] { "ладони", "гладкие перчатки" });
                if (tagsSet.Contains("SmoothGloves")) set = new() { "ладони" };
                if (tagsSet.Contains("Ring")) set.Clear();
                break;

            case "neck":
                set.UnionWith(new[] { "шея" });
                if (tagsSet.Contains("OpenNeck")) set.Clear();
                break;

            case "mask":
                set.UnionWith(new[] { "рот" });
                if (tagsSet.Contains("FaceCovered")) set = new() { "рот", "щёки", "лицо" };
                break;

            case "bra":
                set.UnionWith(new[] { "грудь" });
                break;

            case "socks":
                set.UnionWith(new[] { "ступни" });
                break;

            case "shoes":
                set.UnionWith(new[] { "носки", "ступни" });
                break;
        }

        return set;
    }
}
