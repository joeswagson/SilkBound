using UnityEngine;
using SilkBound.Utils;
using SilkBound.Behaviours;
using SilkBound.Network.Packets.Impl.Sync.Entity;

namespace SilkBound.Sync {
    public abstract class NetworkEntity : NetworkObject {
        public override bool Active => NetworkUtils.IsNullPtr(Root) || Root.activeInHierarchy;

        public virtual GameObject Root => gameObject;
        private Vector3 lastPosition;
        public Vector3 Position => Root.transform.position;
        public SimpleInterpolator? _interpolator;
        public SimpleInterpolator Interpolator => _interpolator ?? Root.AddComponentIfNotPresent<SimpleInterpolator>();
        private Rigidbody2D? _body;
        public Rigidbody2D Body => _body ??= Root.GetComponent<Rigidbody2D>();

        public void UpdatePosition(Vector3 position, Vector2 velocity, float scaleX)
        {
            //Logger.Msg("Updating", NetworkId, "at", position);
            Root.transform.position = position;
            Interpolator.velocity = velocity;
            Vector3 scaleRef = Root.transform.localScale;
            scaleRef.x = scaleX;
            Root.transform.localScale = scaleRef;
        }
        protected override void Tick(float dt)
        {
            PreTickNoObjectRequired(dt);

            if (Root == null)
                return;

            PreTick(dt);

            if (IsLocalOwned)
            {
                if (Body)
                    Body.constraints = RigidbodyConstraints2D.FreezeRotation;

                //var deltaPos = Root.transform.localPosition - Position;
                ////Logger.Msg("syncing position of entity", NetworkId);
                if ((lastPosition - Position).sqrMagnitude > 0.01f)
                    NetworkUtils.SendPacket(
                        new SyncEntityPositionPacket(
                            NetworkId,
                            Root.scene.name,
                            Position,
                            Body?.linearVelocity ?? Vector2.zero,
                            Root.transform.localScale.x
                        )
                    );
            } else
            {
                if (Body)
                    Body.constraints = RigidbodyConstraints2D.FreezeAll;
            }

            lastPosition = Position;

            AdditionalTick(dt);
        }

        protected virtual void PreTickNoObjectRequired(float dt) { }
        protected virtual void PreTick(float dt) { }
        protected virtual void AdditionalTick(float dt) { }
    }
}
