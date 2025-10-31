using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Lib.DbgRender {
    public class DbgRenderCore {
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
            alive.ForEach(r => r.Draw(r.Origin));
        }
        public static void Dispose()
        {
            alive.ForEach(r=>r.Dispose());
        }
    }
}
