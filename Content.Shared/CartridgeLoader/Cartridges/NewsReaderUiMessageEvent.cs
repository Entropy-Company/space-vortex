using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class NewsReaderUiMessageEvent : CartridgeMessageEvent
{
    public readonly NewsReaderUiAction Action;
    public readonly int? ArticleIndex;

    public NewsReaderUiMessageEvent(NewsReaderUiAction action, int? articleIndex = null)
    {
        Action = action;
        ArticleIndex = articleIndex;
    }
}

[Serializable, NetSerializable]
public enum NewsReaderUiAction
{
    Next,
    Prev,
    NotificationSwitch,
    ShowArticle,
    ShowList,
    BackToMain
}
