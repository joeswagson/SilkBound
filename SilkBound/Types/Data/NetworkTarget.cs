using GenericVariableExtension;
using SilkBound.Behaviours;
using SilkBound.Managers;
using SilkBound.Network;
using SilkBound.Network.Packets.Impl.Sync.Entity;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace SilkBound.Types.Data {
    public struct NativeNetworkTarget {
        public string NetworkId;
        public Guid TargetId;
    }
    public class NetworkTarget(EntityMirror host) {
        public EntityMirror Host => host;
        public HealthManager Health => Host.HealthManager;

        private HornetMirror? _target;
        public HornetMirror Target => _target ??= NetworkUtils.LocalClient.Mirror;

        public bool ApplyTarget(EntityTargetPacket packet)
        {
            if (packet.Target.Mirror == null)
                return false;

            _target = packet.Target.Mirror;
            Host.UpdateHero(packet.Target);

            return true;
        }

        public bool SetTarget(HornetMirror? target)
        {
            if (!(target?.IsInScene ?? false))
                return false;

            var old = _target;
            _target = target;

            if (Host.IsLocalOwned)
                NetworkUtils.SendPacket(new EntityTargetPacket(Host.NetworkId, target.Client.ClientID));

            return old != _target;
        }

        public bool SetTarget(Weaver? target) => SetTarget(target?.Mirror);

        #region Enumerable Selections
        public HornetMirror? Min(Func<HornetMirror, float?> selector)
        {
            var source = HornetMirror.Mirrors;
            if (source == null) return null;

            bool hasValue = false;
            HornetMirror? bestItem = null;
            float bestValue = float.MaxValue;

            foreach (var item in source)
            {
                float? val = selector(item.Value);
                if (val == null) continue;

                if (!hasValue || val.Value < bestValue)
                {
                    hasValue = true;
                    bestValue = val.Value;
                    bestItem = item.Value;
                }
            }

            return bestItem;
        }
        public HornetMirror? Max(Func<HornetMirror, float?> selector)
        {
            var source = HornetMirror.Mirrors;
            if (source == null) return null;

            bool hasValue = false;
            HornetMirror? bestItem = null;
            float bestValue = float.MinValue;

            foreach (var item in source)
            {
                float? val = selector(item.Value);
                if (val == null) continue;

                if (!hasValue || val.Value > bestValue)
                {
                    hasValue = true;
                    bestValue = val.Value;
                    bestItem = item.Value;
                }
            }

            return bestItem;
        }
        #endregion
        public HornetMirror Select(ServerSettings.BossTargetingMethod mode)
        {
            HornetMirror? selected = null;
            var origin = Host?.Root?.transform.position;
            switch (mode)
            {
                case ServerSettings.BossTargetingMethod.Nearest:
                    selected = Min(f => f.GetDistance(origin));
                    break;
                case ServerSettings.BossTargetingMethod.Furthest:
                    selected = Max(f => f.GetDistance(origin));
                    break;
                case ServerSettings.BossTargetingMethod.LowestHealth:
                    selected = Min(f => f.Health);
                    break;
                case ServerSettings.BossTargetingMethod.HighestHealth:
                    selected = Max(f => f.Health);
                    break;
                case ServerSettings.BossTargetingMethod.Random:
                    selected = HornetMirror.Mirrors.ElementAt(new Random().Next(HornetMirror.Mirrors.Count)).Value;
                    break;
            }

            if (selected == null)
                selected = NetworkUtils.LocalClient.Mirror;

            return selected;
        }

        public bool Update()
        {
            return SetTarget(Select(NetworkUtils.ServerSettings.BossTargeting));
        }
        public bool Update([NotNullWhen(true)] out HornetMirror? newTarget)
        {
            newTarget = Select(NetworkUtils.ServerSettings.BossTargeting);
            return SetTarget(newTarget);
        }
    }
}
