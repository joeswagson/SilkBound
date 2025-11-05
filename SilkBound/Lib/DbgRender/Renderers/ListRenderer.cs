using HutongGames.Extensions;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SilkBound.Lib.DbgRender.Renderers {
    public class ListRenderer(Dictionary<string, object?> list, DrawAnchor? anchor=null) : Renderer(anchor) {
        private string EntryToString(KeyValuePair<string, object?> entry)
        {
            return $"{entry.Key}: {ChunkedTransfer.NormalizeObject(entry.Value)}";
        }

        private readonly float linePadding = 5f;
        public Color bgColor = new Color(0, 0, 0, 0.75f);
        public override void Draw()
        {
            if (list.Count == 0)
                return;

            var entries = list.Select(EntryToString).ToArray();

            float maxWidth = entries
                .Select(t => GUI.skin.label.CalcSize(new GUIContent(t)).x)
                .DefaultIfEmpty(0)
                .Max() + 5;

            float lineHeights = entries
                .Select(t => GUI.skin.label.CalcHeight(new GUIContent(t), maxWidth))
                .DefaultIfEmpty(0)
                .Sum();

            float paddedWidth = maxWidth + (linePadding * 2);
            float totalHeight = (lineHeights) + (linePadding * 2);

            var root = Box(paddedWidth, totalHeight);

            var bgRect = new Rect(root.x, root.y, root.width, root.height);
            //var prevColor = GUI.color;
            //GUI.color = new Color(1, 0, 0, 0.5f);
            //GUI.backgroundColor = new Color(1, 0, 0, 0.5f);
            GUI.DrawTexture(
                bgRect,
                Texture2D.whiteTexture,
                ScaleMode.StretchToFill,
                true,
                1,
                bgColor,
                0, 0
            );
            //GUI.color = prevColor;

            float y = root.y + linePadding;
            foreach (var line in entries)
            {
                var lineHeight = GUI.skin.label.CalcHeight(new GUIContent(line), maxWidth);
                GUI.Label(new Rect(root.x + linePadding, y, maxWidth, lineHeight), line);
                y += lineHeight;
            }
        }
    }
}
