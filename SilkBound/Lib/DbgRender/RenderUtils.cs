using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SilkBound.Lib.DbgRender {
    public static class RenderUtils {
        public static Rect GetWindowPosition(DrawAnchor anchor, float width, float height)
        {
            Vector2 screen = anchor.screen;
            Vector2 size = new Vector2(width, height);

            float x;
            switch (anchor.AnchorX)
            {
                default:
                case DrawAnchorX.Left:
                    x = 0;
                    break;
                case DrawAnchorX.Center:
                    x = screen.x - (width / 2);
                    break;
                case DrawAnchorX.Right:
                    x = screen.x - width;
                    break;

            }

            float y;
            switch (anchor.AnchorY)
            {
                default:
                case DrawAnchorY.Top:
                    y = 0;
                    break;
                case DrawAnchorY.Center:
                    y = screen.y - (height / 2);
                    break;
                case DrawAnchorY.Bottom:
                    y = screen.y - height;
                    break;

            }

            Vector2 position = new Vector2(x, y);
            return new Rect(position, size);
        }
    }
}
