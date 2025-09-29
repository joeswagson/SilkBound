using SilkBound.Addons.Events;
using SilkBound.Addons.Events.Handlers;
using SilkBound.Managers;
using SilkBound.Network;
using SilkBound.Types;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace SilkBound.Sync
{
    public abstract class GenericSync : MonoBehaviour
    {
        private float _tickTimeout;

        private void Awake()
        {
            if (!NetworkUtils.IsConnected)
            {
                Destroy(this);
                return;
            }

            Start();
            TickManager.OnTick += _tick;
        }

        private void OnDestroy()
        {
            TickManager.OnTick -= _tick;
            Reset();
        }

        protected abstract void Start();
        protected void _tick(float dt)
        {
            if (NetworkUtils.IsConnected)
                Tick(dt);
            else
                TickDisconnected(dt);
        }
        protected virtual void TickDisconnected(float dt) { }
        protected abstract void Tick(float dt);
        protected abstract void Reset();
    }
}
