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
        public DrawAnchor Origin;
        /// <summary>
        /// Creates an uninitialized Renderer instance.
        /// </summary>
        /// <param name="anchor">The side of the screen to anchor to. Default is <see cref="DrawAnchor.TopLeft"/></param>
        public Renderer(DrawAnchor? anchor = null)
        {
            Origin = anchor ?? DrawAnchor.TopLeft;
        }
        bool registered = false;

        /// <summary>
        /// Registers a renderer and applies its settings.
        /// </summary>
        /// <returns><c>true</c> if the renderer hasn't been registered already.</returns>
        public bool Register()
        {
            if (registered) return false;

            registered = true;
            GUI.backgroundColor = new Color(1, 0, 0, 0.5f);
            GUI.color = Color.white;

            ApplySettings();

            return true;
        }

        /// <summary>
        /// Called after <see cref="Register"/>. Use this to change settings via <see cref="GUI"/>
        /// </summary>
        /// <example>
        /// public override void ApplySettings() { 
        ///     GUI.color = Color.red; // Make renderer text color red.
        /// }
        /// </example>
        public virtual void ApplySettings() { }

        /// <summary>
        /// Wrapper for the OnGUI MonoBehaviour message. Draw elements via <see cref="GUI"/>.
        /// </summary>
        /// <param name="origin">Equivalent to <see cref="Origin"/></param>
        public abstract void Draw(DrawAnchor origin);

        /// <summary>
        /// Optional method to dispose any resources used by the renderer. Called before application exit.
        /// </summary>
        public virtual void Dispose() { }
    }
}
