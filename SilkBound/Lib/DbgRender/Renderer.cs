using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static UnityEngine.UI.Image;

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
        #region Corners
        public static readonly DrawAnchor TopLeft = new DrawAnchor(DrawAnchorY.Top, DrawAnchorX.Left);
        public static readonly DrawAnchor TopRight = new DrawAnchor(DrawAnchorY.Top, DrawAnchorX.Right);
        public static readonly DrawAnchor BottomLeft = new DrawAnchor(DrawAnchorY.Bottom, DrawAnchorX.Left);
        public static readonly DrawAnchor BottomRight = new DrawAnchor(DrawAnchorY.Bottom, DrawAnchorX.Right);
        #endregion

        #region Edges
        public static readonly DrawAnchor Top = new DrawAnchor(DrawAnchorY.Top, DrawAnchorX.Center);
        public static readonly DrawAnchor Bottom = new DrawAnchor(DrawAnchorY.Bottom, DrawAnchorX.Center);
        public static readonly DrawAnchor Left = new DrawAnchor(DrawAnchorY.Center, DrawAnchorX.Left);
        public static readonly DrawAnchor Right = new DrawAnchor(DrawAnchorY.Center, DrawAnchorX.Right);
        #endregion

        public DrawAnchorX AnchorX = anchorX;
        public DrawAnchorY AnchorY = anchorY;

        public TextAnchor? _text;
        public TextAnchor Text => _text ??= Enum.Parse<TextAnchor>(AnchorY switch {
            DrawAnchorY.Top => "Upper",
            DrawAnchorY.Center => "Middle",
            DrawAnchorY.Bottom => "Lower",
            _ => "Middle"
        } + AnchorX.ToString());

        public Vector2 normalized = new Vector2(
            (((float) anchorX) + 1) / 2,
            (((float) anchorY) + 1) / 2
        );

        public Vector2 screen => normalized.MultiplyElements(Screen.width, Screen.height);
    }

    /// <summary>
    /// Internal generic class for defining a custom renderer.
    /// </summary>
    public abstract class Renderer {
        public DrawAnchor Origin = DrawAnchor.TopLeft;
        /// <summary>
        /// Creates an uninitialized Renderer instance.
        /// </summary>
        /// <param name="anchor">The side of the screen to anchor to. Default is <see cref="DrawAnchor.TopLeft"/></param>
        public Renderer(DrawAnchor? anchor = null)
        {
            Origin = anchor ?? DrawAnchor.TopLeft;
            Created();
        }
        bool registered = false;

        /// <summary>
        /// Fired in the constructor after <see cref="Origin"/> is set.
        /// </summary>
        public virtual void Created() { }

        /// <summary>
        /// Registers a renderer and applies its settings.
        /// </summary>
        /// <returns><c>true</c> if the renderer hasn't been registered already.</returns>
        public bool Register()
        {
            if (registered) return false;

            registered = true;

            return true;
        }


        private Rect reference = Rect.zero;
        public Rect SetCursorReference(Rect rect) => reference = rect;
        public float ElementWidth => reference.width;
        public float ElementHeight => reference.height;
        public Rect ElementBuffer(float? w = null, float? h = null)
        {
            if (w.HasValue)
                reference.width = w.Value;
            if (h.HasValue)
                reference.height = h.Value;

            return reference;
        }

        private Rect _cursor = Rect.zero;
        public ref Rect Cursor => ref _cursor;
        /// <summary>
        /// Resets the sub-anchor to <see cref="Rect.zero"/>.
        /// </summary>
        /// <returns><see cref="Rect.zero"/></returns>
        public Rect ResetCursor(Rect? reference = null)
        {
            Cursor = Rect.zero;
            return reference ?? Rect.zero;
        }
        /// <summary>
        /// Moves the sub-anchor.
        /// </summary>
        /// <param name="stepX">Horizontal offset.</param>
        /// <param name="stepY">Vertical offset.</param>
        /// <returns>The offset <see cref="Cursor"/>.</returns>
        public Rect StepCursor(float stepX = 0, float stepY = 0)
        {
            Cursor.x += stepX;
            Cursor.y += stepY;
            return CursorToScreen();
        }

        /// <summary>
        /// Returns the absolute position of the current <see cref="Cursor"/> to a reference <see cref="Rect"/>
        /// </summary>
        /// <param name="reference"></param>
        /// <returns></returns>
        public Rect CursorToScreen() => new Rect(reference.x + Cursor.x, reference.y + Cursor.y, reference.width, reference.height);

        /// <summary>
        /// Transforms the sub-anchor vertically.
        /// </summary>
        /// <param name="scrollAmount">The magnitude of the vertical transformation.</param>
        /// <returns>The screen space <see cref="Cursor"/>.</returns>
        public Rect Scroll(float scrollAmount)
        {
            Cursor.y += scrollAmount;
            return CursorToScreen();
        }
        /// <summary>
        /// Transforms the sub-anchor horizonally.
        /// </summary>
        /// <param name="slideAmount">The magnitude of the horizonal transformation.</param>
        /// <returns>The screen space <see cref="Cursor"/>.</returns>
        public Rect Slide(float slideAmount)
        {
            Cursor.x += slideAmount;
            return CursorToScreen();
        }

        /// <summary>
        /// Explicitly sets the cursors Y position locally.
        /// </summary>
        /// <returns>The screen space <see cref="Cursor"/>.</returns>
        public Rect Y(float y)
        {
            Cursor.y = y;
            return CursorToScreen();
        }

        /// <summary>
        /// Explicitly sets the cursors Y position locally.
        /// </summary>
        /// <returns>The screen space <see cref="Cursor"/>.</returns>
        public Rect X(float x)
        {
            Cursor.x = x;
            return CursorToScreen();
        }

        /// <summary>
        /// Returns a region at the sub-anchor with a specified <see cref="float"/> width and height.
        /// </summary>
        public Rect ScaleCursor(float width, float height)
        {
            var screenCursor = CursorToScreen();
            return new Rect(screenCursor.x, screenCursor.y, width, height);
        }

        /// <summary>
        /// Returns a region at the sub-anchor with a specified <see cref="Vector2"/> size.
        /// </summary>
        public Rect ScaleCursor(Vector2 size) => ScaleCursor(size.x, size.y);

        /// <summary>
        /// Called before <see cref="Draw()"/>. Use this to change settings via <see cref="GUI"/>
        /// </summary>
        /// <param name="init">Initialization flag. This will only be <see langword="true"/> once, and it will fire before anything besides <see cref="Register"/></param>
        /// <example>
        /// public override void ApplySettings() { 
        ///     GUI.color = Color.red; // Make renderer text color red.
        /// }
        /// </example>
        protected virtual void ApplySettings(bool init)
        {
            GUI.backgroundColor = new Color(0, 0, 0, 0.5f);
            GUI.color = Color.white;

            GUI.skin.label.alignment = Origin.Text;
        }

        private bool init = true;
        public void ApplySettings()
        {
            ApplySettings(init);
            init = false;
        }

        /// <summary>
        /// Wrapper for the OnGUI MonoBehaviour message. Draw elements via <see cref="GUI"/>.
        /// </summary>
        /// <param name="origin">Equivalent to <see cref="Origin"/></param>
        public abstract void Draw();

        /// <summary>
        /// Optional method to dispose any resources used by the renderer. Called before application exit.
        /// </summary>
        public virtual void Dispose() { }

        #region Helper Methods
        static Color defaultBg = new Color(0, 0, 0, 0.75f);
        public Rect Box(float width, float height) => RenderUtils.GetWindowPosition(Origin, width, height);
        protected Rect DrawBox(float width, float height, Color? bgColor = null, float borderRadius = 0, float borderWidth = 0) => DrawBox(Box(width, height), bgColor, borderRadius, borderWidth);
        protected Rect DrawBox(Rect box, Color? bgColor = null, float borderRadius = 0, float borderWidth = 0)
        {
            GUI.DrawTexture(
                box,
                Texture2D.whiteTexture,
                ScaleMode.StretchToFill,
                true,
                1,
                bgColor ?? defaultBg,
                borderWidth,
                borderRadius

            );
            return box;
        }
        protected void DrawVoid(float? height = null, float margin = 5) => Scroll(height ?? ElementHeight + margin);
        #endregion
    }
}
