// SPDX-FileCopyrightText: 2024 Julian Giebel <juliangiebel@live.de>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: MIT

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
