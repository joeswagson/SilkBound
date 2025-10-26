using SilkBound.Managers;
using SilkBound.Utils;
using UnityEngine;

namespace SilkBound.Sync
{
    public abstract class GenericSync : MonoBehaviour
    {
        private void Awake()
        {
            if (!NetworkUtils.Connected)
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
            if (NetworkUtils.Connected)
                Tick(dt);
            else
                TickDisconnected(dt);
        }
        protected virtual void TickDisconnected(float dt) { }
        protected abstract void Tick(float dt);
        protected abstract void Reset();
    }
}
