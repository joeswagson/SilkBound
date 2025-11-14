using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SilkBound.Extensions {
    public static class VectorExtensions {

        #region Vector3

        public static Vector2 Transform(this Vector3 vec)
        {
            return new Vector2(vec.x, vec.y);
        }

        #endregion

        #region Vector2

        public static Vector3 Transform(this Vector2 vec, float layer = 0f)
        {
            return new Vector3(vec.x, vec.y, layer);
        }

        #endregion

    }
}
