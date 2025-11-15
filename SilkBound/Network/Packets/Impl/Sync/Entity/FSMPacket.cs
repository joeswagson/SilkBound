using HutongGames.PlayMaker;
using Mono.Mozilla;
using SilkBound.Extensions;
using SilkBound.Managers;
using SilkBound.Network.NetworkLayers;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

// cant wait til someone abuses this and i have to make it lame and restricted to registered network objects under your name
namespace SilkBound.Network.Packets.Impl.Sync.Entity {
    //public struct FsmFingerprint
    //{
    //    public FsmFingerprint(string path, string name)
    //    {
    //        Path = path;
    //        Name = name;
    //    }

    //    public Guid Id;
    //    public string Path;
    //    public string Name;
    //}
    public abstract class FSMPacket : Packet {
        public Guid Id { get; private set; }

        protected FSMPacket(Guid target)
        {
            Id = target;
        }

        public Fsm? FSM => FindFSM(Id);
        public static Fsm? FindFSM(Guid target)
        {
            return ObjectManager.GetComponent<PlayMakerFSM>(target)?.Fsm;
        }
        public static bool FindFSM(Guid target, [NotNullWhen(true)] out Fsm? fsm)
        {
            return (fsm = FindFSM(target)) != null;
        }
        public override void Serialize(BinaryWriter writer)
        {
            Logger.Debug("Writing id:", Id);
            Write(Id);
        }

        public override Packet Deserialize(BinaryReader reader)
        {
            Id = Read<Guid>();
            Logger.Debug("Got id:", Id);

            return null!;
        }
    }
    public class FSMEventPacket(Guid id, string eventName) : FSMPacket(id) {
        public string EventName { get; } = eventName;

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(EventName);
        }

        public override Packet Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            string eventName = reader.ReadString();

            return new FSMEventPacket(
                Id,
                eventName
            );
        }

        public override void ClientHandler(NetworkConnection connection)
        {
            //Logger.Msg("Firing:", Fingerprint.Path, Fingerprint.Name, EventName, "on fsm:", FSM);
            if (FSM == null)
                Logger.Warn("Null FSM!", Id, EventName);
            FSM?.Event(EventName);
        }

        public override void ServerHandler(NetworkConnection connection)
        {
            FSM?.Event(EventName);
            NetworkUtils.LocalServer?.SendExcept(this, connection);
        }
    }
    public class FSMStatusPacket(Guid id, bool enabled) : FSMPacket(id) {
        public bool Enabled { get; } = enabled;

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(Enabled);
        }

        public override Packet Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            bool enabled = reader.ReadBoolean();

            return new FSMStatusPacket(
                id, 
                enabled
            );
        }

        public override void ClientHandler(NetworkConnection connection)
        {
            Logger.Msg("Starting:", id, "with", "on fsm:", FSM);
            if (FSM == null)
                Logger.Warn("Null FSM!", id);
            if (Enabled)
                FSM?.Start();
            else
                FSM?.Stop();
        }

        public override void ServerHandler(NetworkConnection connection)
        {
            if (Enabled)
                FSM?.Start();
            else
                FSM?.Stop();
            NetworkUtils.LocalServer?.SendExcept(this, connection);
        }
    }
    public enum FsmStateType {
        Enter,
        Exit,
        SilentEnter,
        SilentExit,
    }
    public class FSMStatePacket(Guid id, FsmStateType state, string stateName) : FSMPacket(id) {
        public FsmStateType StateType => state;
        public FsmState? State => FSM?.GetState(stateName);
        private static Dictionary<FsmStateType, Action<Fsm, FsmState>> Delegates = new() {

            { FsmStateType.Enter, (fsm, state) => fsm.EnterState(state) },
            { FsmStateType.Exit, (fsm, state) => fsm.ExitState(state) },
            { FsmStateType.SilentEnter, (fsm, state) => state.OnEnter() },
            { FsmStateType.SilentExit, (fsm, state) => state.OnExit() }
        };
        public Action<Fsm, FsmState>? Delegate => Delegates[state];
        public void Trigger()
        {
            Logger.Msg("Trigger", FSM?.Name ?? "nullfsm", State?.Name ?? "nullst8");
            if(FSM == null || State == null)
            {
                Logger.Msg("FSM or State was null:", FSM?.Name, State?.Name);
                return;
            }

            Delegate?.Invoke(FSM, State);
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)state);
            writer.Write(stateName);
        }

        public override Packet Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            FsmStateType stateType = (FsmStateType) reader.ReadInt32();
            string stateName = reader.ReadString();

            return new FSMStatePacket(
                Id,
                stateType,
                stateName
            );
        }

        public override void ClientHandler(NetworkConnection connection)
        {
            Trigger();
        }

        public override void ServerHandler(NetworkConnection connection)
        {
            Trigger();
            Relay(connection);
        }
    }
}
