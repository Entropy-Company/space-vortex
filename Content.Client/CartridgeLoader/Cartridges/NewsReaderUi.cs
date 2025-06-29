// SPDX-FileCopyrightText: 2024 Julian Giebel <juliangiebel@live.de>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: MIT

using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.CartridgeLoader;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed partial class NewsReaderUi : UIFragment
{
    private NewsReaderUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new NewsReaderUiFragment();

        _fragment.OnNextButtonPressed += () =>
        {
            SendNewsReaderMessage(NewsReaderUiAction.Next, userInterface);
        };
        _fragment.OnPrevButtonPressed += () =>
        {
            SendNewsReaderMessage(NewsReaderUiAction.Prev, userInterface);
        };
        _fragment.OnNotificationSwithPressed += () =>
        {
            SendNewsReaderMessage(NewsReaderUiAction.NotificationSwitch, userInterface);
        };
        _fragment.OnArticleSelected += (index) =>
        {
            SendNewsReaderMessage(NewsReaderUiAction.ShowArticle, userInterface, index);
        };
        _fragment.OnBackButtonPressed += () =>
        {
            SendNewsReaderMessage(NewsReaderUiAction.BackToMain, userInterface);
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        switch (state)
        {
            case NewsReaderBoundUserInterfaceState cast:
                _fragment?.UpdateArticleState(cast.Article, cast.TargetNum, cast.TotalNum, cast.NotificationOn);
                break;
            case NewsReaderEmptyBoundUserInterfaceState empty:
                _fragment?.UpdateEmptyState(empty.NotificationOn);
                break;
            case NewsReaderListBoundUserInterfaceState list:
                _fragment?.UpdateListState(list.Articles, list.NotificationOn);
                break;
        }
    }

    private void SendNewsReaderMessage(NewsReaderUiAction action, BoundUserInterface userInterface, int? articleIndex = null)
    {
        var newsMessage = new NewsReaderUiMessageEvent(action, articleIndex);
        var message = new CartridgeUiMessage(newsMessage);
        userInterface.SendMessage(message);
    }
}