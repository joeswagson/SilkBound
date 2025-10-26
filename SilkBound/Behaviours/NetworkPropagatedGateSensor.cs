using SilkBound.Extensions;
using SilkBound.Network;
using SilkBound.Network.Packets.Impl.Sync.World;
using SilkBound.Types;
using SilkBound.Utils;
using System.Linq;
using UnityEngine;

namespace SilkBound.Behaviours
{
    /// <summary>
    /// Closes a boss area gate when a player walks near it.
    /// </summary>
    public class NetworkPropagatedGateSensor : MonoBehaviour
    {
        public const string FSM_GATE_OPEN = "BG OPEN";
        public const string FSM_GATE_CLOSE = "BG CLOSE";
        public const float ACTIVATION_DISTANCE = 5f;
        public const float PARTYSCAN_DISTANCE = 10f;
        public static NetworkPropagatedGateSensor? AddComponent(BattleScene battle, PlayMakerFSM gateFsm)
        {
            var inst = gateFsm.gameObject.AddComponentIfNotPresent<NetworkPropagatedGateSensor>();
            inst?.Init(battle, gateFsm);
            return inst;
        }

        bool hasInitialized = false;
        public PlayMakerFSM GateFsm { get; private set; } = null!;
        public BattleScene Battle { get; private set; } = null!;
        void Init(BattleScene battle, PlayMakerFSM gateFsm)
        {
            Battle = battle;
            GateFsm = gateFsm;
            hasInitialized = true;
        }

        public float? GetClientSqrDistance(Weaver client)
        {
            var mirror = client.Mirror;
            if (mirror == null)
                return null;

            return (mirror.Root.transform.position - transform.position).sqrMagnitude;
        }

        public Vector3 RoundVector(Vector3 vector)
        {
            return new Vector3(
                Mathf.Round(vector.x),
                Mathf.Round(vector.y),
                Mathf.Round(vector.z)
            );
        }
        float GetDistanceFromCol(BoxCollider2D col, Vector3 point)
        {
            point = RoundVector(point);

            var bounds = col.bounds;

            float clampedY = Mathf.Clamp(point.y, bounds.min.y - 1f, bounds.max.y + 1f);

            float distLeft = Mathf.Abs(point.x - bounds.min.x);
            float distRight = Mathf.Abs(point.x - bounds.max.x);
            float distX = Mathf.Min(distLeft, distRight);

            float distY = Mathf.Abs(point.y - clampedY);

            return Mathf.Sqrt(distX * distX + distY * distY);
        }

        float GetClientDistanceFromCol(BoxCollider2D col, Weaver client)
        {
            var mirror = client.Mirror;
            if (mirror == null)
                return PARTYSCAN_DISTANCE + 1; // me so smart

            return GetDistanceFromCol(col, mirror.Root.transform.position);
        }
        float GetLocalDistance()
        {
            if (CachedCollider == null)
                return ACTIVATION_DISTANCE + 1; // me so smart 2
            return GetDistanceFromCol(CachedCollider, HeroController.instance.transform.position);
        }

        public bool ClientInPartyscanRadius(Weaver client)
        {
            if (!CachedCollider) return false;

            return GetClientDistanceFromCol(CachedCollider, client) <= PARTYSCAN_DISTANCE;
        }
        public bool ClientInRadius(Weaver client)
        {
            if (!CachedCollider) return false;

            return GetClientDistanceFromCol(CachedCollider, client) <= ACTIVATION_DISTANCE;
        }
        public bool InRadius()
        {
            return GetLocalDistance() <= ACTIVATION_DISTANCE;
        }
        public bool InSharedRadius()
        {
            return GetLocalDistance() <= PARTYSCAN_DISTANCE;
        }

        public bool AllPlayersInRadius()
        {
            return Server.CurrentServer.Connections.All(ClientInPartyscanRadius);
        }

        BoxCollider2D? _cached;
        BoxCollider2D? CachedCollider
        {
            get
            {
                return _cached ??= GateFsm?.GetComponent<BoxCollider2D>();
            }
        }

        private readonly float debounce = 0.1f;
        private float lastCheck = 0f;
        public bool Closed => CachedCollider?.isActiveAndEnabled ?? false;

        public void UpdateSensor(bool activated)
        {
            if (activated)
                Close();
            else
                Open();
        }
        public void Open(bool sync = false)
        {
            if (!Closed || !GateFsm) return;

            GateFsm.SendEvent(FSM_GATE_OPEN);

            if (sync)
                NetworkUtils.SendPacket(new BossGateSensorPacket(GateFsm.transform.GetPath(), false));
        }

        public void Close(bool sync = false)
        {
            if (Closed || !GateFsm) return;

            GateFsm.SendEvent(FSM_GATE_CLOSE);

            if (sync)
                NetworkUtils.SendPacket(new BossGateSensorPacket(GateFsm.transform.GetPath(), true));
        }
        void Update()
        {
            if(!hasInitialized || !NetworkUtils.Connected || Time.time - lastCheck <= debounce) return;

            if (Battle.started)
            {
                if (!Closed)
                    Close(true);
                return;
            }

            lastCheck = Time.time;

            bool localInRadius = InRadius();
            bool localInShareRadius = InSharedRadius();
            bool anyClientInRadius = Server.CurrentServer.Connections.Any(ClientInRadius);

            bool anyPlayerInRadius = localInRadius || anyClientInRadius;
            bool allPlayersInRadius = AllPlayersInRadius() && localInShareRadius;

            //Logger.Msg(
            //    "gate:", GateFsm.name,
            //    "localInRadius:", localInRadius,
            //    "anyClientInRadius:", anyClientInRadius,
            //    "anyPlayerInRadius:", anyPlayerInRadius,
            //    "allPlayersInRadius:", allPlayersInRadius
            //);

            if (allPlayersInRadius)
            {
                //Logger.Msg("all players in rad");
                Open();
            }
            else if (anyPlayerInRadius)
            {
                //Logger.Msg("one player in rad");
                Close(true);
            }
            else
            {
                //Logger.Msg("no one in rad");
                Open();
            }
        }
    }
}
