using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SilkBound.Lib.DbgRender {
    public enum DrawAnchorX {
        Left = -1,
        Center = 0,
        Right = 1,
    }
    public enum DrawAnchorY {
        Top = -1,
        Center = 0,
        Bottom = 1,
    }

    public struct DrawAnchor(DrawAnchorY anchorY, DrawAnchorX anchorX) {
        public static DrawAnchor TopLeft = new DrawAnchor(DrawAnchorY.Top, DrawAnchorX.Left);

        public DrawAnchorX AnchorX = anchorX;
        public DrawAnchorY AnchorY = anchorY;

        public Vector2 normalized = new Vector2(
            (((float) anchorX) + 1) / 2,
            (((float) anchorY) + 1) / 2
        );
        public Vector2 screen => normalized.MultiplyElements(Screen.width, Screen.height);
    }

    public abstract class Renderer {
        public DrawAnchor Origin;
        public Renderer(DrawAnchor? anchor = null)
        {
            Origin = anchor ?? DrawAnchor.TopLeft;
        }
        bool registered = false;
        public void Register()
        {
            if (registered) return;

            registered = true;
            GUI.color = Color.white;
            ApplySettings();
        }
        public virtual void ApplySettings() { }
        public virtual void Dispose() { }
        public abstract void Draw(DrawAnchor origin);
    }
}
