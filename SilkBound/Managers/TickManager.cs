using System;
using UnityEngine;

namespace SilkBound.Managers
{
    public class TickManager
    {
        public const int TPS = 20;
        private const float TICK_INTERVAL = 1f / TPS;

        private static float _accumulator = 0f;
        private static float _lastTime;

        public static event Action<float>? OnTick;

        private void Awake()
        {
            _lastTime = Time.realtimeSinceStartup;
        }

        internal static void Update()
        {
            float now = Time.realtimeSinceStartup;
            float delta = now - _lastTime;
            _lastTime = now;

            _accumulator += delta;

            while (_accumulator >= TICK_INTERVAL)
            {
                _accumulator = 0; // do not spam ticks to make up for lost time 
                //_accumulator -= TICK_INTERVAL;
                OnTick?.Invoke(delta);
            }
        }
    }
}
