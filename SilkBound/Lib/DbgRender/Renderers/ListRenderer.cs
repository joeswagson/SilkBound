using HutongGames.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SilkBound.Lib.DbgRender.Renderers {
    public class ListRenderer(Dictionary<string, object?> list) : Renderer {
        private static string NormalizeObject(object? input) =>
            input switch {
                null => "null",
                object?[] array => $"{{{string.Join(",", array.Select(NormalizeObject))}}}",
                _ => input.ToString() ?? "[object->string failure]"
            };

        //private static string[] NormalizeArray(object?[] input) => [.. input.Select(NormalizeObject)];

        private string EntryToString(KeyValuePair<string, object?> entry)
        {
            return $"{entry.Key}: {NormalizeObject(entry.Value)}";
        }

        private readonly float lineHeight = 30f;
        private readonly float linePadding = 5f;

        public override void Draw(DrawAnchor origin)
        {
            if (list.Count == 0)
                return;

            var entries = list.Select(EntryToString).ToArray();

            float maxWidth = entries
                .Select(t => GUI.skin.label.CalcSize(new GUIContent(t)).x)
                .DefaultIfEmpty(0)
                .Max() + 5;

            float paddedWidth = maxWidth + (linePadding * 2);
            float totalHeight = (lineHeight * entries.Length) + (linePadding * 2);

            var root = RenderUtils.GetWindowPosition(origin, paddedWidth, totalHeight);

            var bgRect = new Rect(root.x, root.y, paddedWidth, totalHeight);
            var prevColor = GUI.color;
            GUI.color = new Color(0, 0, 0, 0.5f);
            GUI.Box(bgRect, GUIContent.none);
            GUI.color = prevColor;

            float y = root.y + linePadding;
            foreach (var line in entries)
            {
                GUI.Label(new Rect(root.x + linePadding, y, maxWidth, lineHeight), line);
                y += lineHeight;
            }
        }
    }
}
