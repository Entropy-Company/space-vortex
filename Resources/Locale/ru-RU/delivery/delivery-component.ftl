delivery-recipient-examine = Адресовано: { $recipient }, { $job }.
delivery-already-opened-examine = Уже вскрыто.
delivery-earnings-examine = Delivering this will earn the station [color=yellow]{ $spesos }[/color] spesos.
delivery-recipient-no-name = Безымянный
delivery-recipient-no-job = Неизвестно
delivery-unlocked-self = Вы разблокировали { $delivery } отпечатком пальца.
delivery-opened-self = Вы вскрываете { $delivery }.
delivery-unlocked-others =
    { CAPITALIZE($recipient) } { GENDER($recipient) ->
        [male] разблокировал
        [female] разблокировала
        [epicene] разблокировали
       *[neuter] разблокировало
    } { $delivery } используя свой отпечаток пальца.
delivery-opened-others =
    { CAPITALIZE($recipient) } { GENDER($recipient) ->
        [male] вскрыл
        [female] вскрыл
        [epicene] вскрыл
       *[neuter] вскрыл
    } { $delivery }.
delivery-unlock-verb = Разблокировать
delivery-open-verb = Вскрыть
delivery-slice-verb = Slice open
delivery-teleporter-amount-examine =
    { $amount ->
        [one] It contains [color=yellow]{ $amount }[/color] delivery.
       *[other] It contains [color=yellow]{ $amount }[/color] deliveries.
    }
delivery-teleporter-empty = The { $entity } is empty.
delivery-teleporter-empty-verb = Take mail
# modifiers
delivery-priority-examine = Это [color=orange]приоритетная { $type }[/color]. У вас осталось [color=orange]{ $time }[/color], чтобы доставить её и получить бонус.
delivery-priority-delivered-examine = Это [color=orange]приоритетная { $type }[/color]. Она была доставлена вовремя.
delivery-priority-expired-examine = Это [color=orange]приоритетная { $type }[/color]. Время на её доставку истекло.
delivery-fragile-examine = Эта [color=red]{ $type } хрупкое[/color]. Доставьте её в целости, чтобы получить бонус.
delivery-fragile-broken-examine = Это [color=red]хрупкая { $type }[/color]. Она выглядит сильно повреждённой.
delivery-bomb-examine = Это [color=purple]бомба { $type }[/color]. О нет.
delivery-bomb-primed-examine = Это [color=purple]бомба { $type }[/color]. Чтение этого текста — не лучшее использование вашего времени.
