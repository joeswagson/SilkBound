using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SilkBound.Extensions
{
    public static class TransformExtensions
    {
        public static string GetPath(this Transform transform)
        {
            if (transform.parent == null)
                return "/" + transform.name;
            return transform.parent.GetPath() + "/" + transform.name;
        }
    }
}
