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
    public sealed class SignTag : IMarkupTag
    {
        public static List<StampDisplayInfo>? CurrentSignatures;

        public string Name => "sign";

        public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
        {
            if (CurrentSignatures == null || !int.TryParse(node.Value.StringValue, out int index))
            {
                return;
            }

            index--; // Convert to 0-based index
            if (index < 0 || index >= CurrentSignatures.Count)
            {
                return;
            }

            var signature = CurrentSignatures[index];
            // The color is handled by the markup text itself, not by the context
        }

        public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
        {
            // Nothing to do here
        }

        public string TextBefore(MarkupNode node)
        {
            if (CurrentSignatures == null || !int.TryParse(node.Value.StringValue, out int index))
            {
                return "[color=gray][bold]___________[/bold]";
            }

            index--; // Convert to 0-based index
            if (index < 0 || index >= CurrentSignatures.Count)
            {
                return "[color=gray][bold]___________[/bold]";
            }

            var signature = CurrentSignatures[index];
            var colorHex = signature.StampedColor.ToHexNoAlpha();
            return $"[color={colorHex}][italic]{signature.StampedName}[/italic][/color]";
        }

        public string TextAfter(MarkupNode node)
        {
            return "";
        }

        public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Robust.Client.UserInterface.Control? control)
        {
            control = null;
            return false;
        }

        private static string GetSignatureName(string stampedName)
        {
            if (string.IsNullOrEmpty(stampedName))
                return string.Empty;

            var parts = stampedName.Split('|');
            return parts.Length > 0 ? parts[0] : stampedName;
        }
    }
}
