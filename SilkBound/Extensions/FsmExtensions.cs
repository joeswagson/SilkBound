using HutongGames.PlayMaker;
using SilkBound.Behaviours;
using SilkBound.Network.Packets.Impl.Sync.Entity;
using SilkBound.Patches.Simple.Game;
using SilkBound.Sync;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SilkBound.Extensions {
    public static class FsmExtensions {
        public static bool GetMirror<T>(this Fsm? target, [NotNullWhen(true)] out T? mirror) where T : GenericSync
        {
            mirror = null;
            if (target?.FsmComponent == null)
                return false;

            mirror = target?.FsmComponent?.GetComponent<T>();
            return mirror != null;
        }
        public static bool IsActive([NotNullWhen(true)] this Fsm? target) => target?.FsmComponent?.Active ?? false;
        public static Guid? Identifier(this Fsm? target)
        {
            return target?.FsmComponent?.GetComponent<Tracker>()?.Id;// new FsmFingerprint(target.FsmComponent.transform.GetPath(), target.Name);
        }
        public static T? Construct<T>(this Fsm? target, Func<Guid, T> factory) where T : FSMPacket
        {
            var print = target.Identifier();
            if (print == null)
            {
                Logger.Msg("Failed to generate identifier for fsm", target?.Name);
                return null;
            }

            return factory.Invoke(print.Value);
        }
        public static void Send<T>(this Fsm? target, Func<Guid, T> factory) where T : FSMPacket
        {
            var print = target.Identifier();
            if (print == null)
            {
                Logger.Msg("Failed to generate identifier for fsm", target?.Name);
                return;
            }

            NetworkUtils.SendPacket(factory.Invoke(print.Value));
        }
    }
}
