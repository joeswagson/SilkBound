using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SilkBound.Utils {
    public static class Shallows {
        public static IEnumerator Coroutine()
        {
            yield break;
        }

        public static IEnumerator<T> Coroutine<T>()
        {
            yield break;
        }
    }
}
