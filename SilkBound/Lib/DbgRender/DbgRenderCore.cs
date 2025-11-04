using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Lib.DbgRender {
    public class DbgRenderCore {
        #region Visibility
        public static bool Enabled { get; private set; } = SilkConstants.DEBUG;

        public static void DebugEnable()
        {
            if (SilkConstants.DEBUG)
                Enable();
        }

        public static void Toggle()
        {
            Enabled = !Enabled;
        }

        public static void Enable()
        {
            Enabled = true;
        }

        public static void Disable()
        {
            Enabled = false;
        }
        #endregion

        private static List<Renderer> alive = [];
        public static void RegisterRenderer(Renderer renderer)
        {
            if (alive.Contains(renderer))
                return;

            renderer.Register();
            alive.Add(renderer);
        }
        public static void OnGUI()
        {
            if (!Enabled)
                return;

            alive.ForEach(r => {
                r.ResetCursor();
                r.ApplySettings();
                r.Draw(r.Origin);
            });
        }
        public static void Dispose()
        {
            alive.ForEach(r => r.Dispose());
        }
    }
}
