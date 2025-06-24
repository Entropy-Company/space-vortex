using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Numerics;
using Content.Shared.Paper;

namespace Content.Client.Paper.UI
{
    public sealed class SignTag : IMarkupTagHandler
    {
        public string Name => "sign";

        // Список подписей для текущей бумажки
        public static IReadOnlyList<StampDisplayInfo>? CurrentSignatures;

        public void PushDrawContext(MarkupNode node, MarkupDrawingContext context) { }
        public void PopDrawContext(MarkupNode node, MarkupDrawingContext context) { }
        public string TextBefore(MarkupNode node) {
            if (CurrentSignatures == null)
                return string.Empty;
            string strVal = node.Value.ToString();
            int idx = 0;
            if (!int.TryParse(strVal, out idx))
                return string.Empty;
            if (idx < 1 || idx > CurrentSignatures.Count)
                return string.Empty;
            var sig = CurrentSignatures[idx - 1];
            // Формируем цвет в hex
            var color = sig.StampedColor.ToHex();
            var name = sig.StampedName.Split('|')[0];
            return $"[color={color}][b]{name}[/b][/color]";
        }
        public string TextAfter(MarkupNode node) => string.Empty;
        public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Robust.Client.UserInterface.Control? control)
        {
            control = null;
            return false;
        }
    }
} 