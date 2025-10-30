using HutongGames.PlayMaker;
using Mono.Mozilla;
using SilkBound.Extensions;
using SilkBound.Types;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

// cant wait til someone abuses this and i have to make it lame and restricted to registered network objects under your name
namespace SilkBound.Network.Packets.Impl.Sync.Entity {
    public struct FsmFingerprint(string path, string name)
    {
        public string Path => path;
        public string Name => name;
    }
    public abstract class FSMPacket(FsmFingerprint target) : Packet {
        public FsmFingerprint Fingerprint = target;
        public Fsm? FSM => FindFSM(target.Path, target.Name);
        public static Fsm? FindFSM(string goPath, string fsmName)
        {
            return UnityObjectExtensions.FindComponents<PlayMakerFSM>(goPath)?.First(fsm => fsm.Fsm.Name == fsmName)?.Fsm;
        }
        public static bool FindFSM(string goPath, string fsmName, [NotNullWhen(true)] out Fsm? fsm)
        {
            fsm = UnityObjectExtensions.FindComponents<PlayMakerFSM>(goPath)?.First(fsm => fsm.Fsm.Name == fsmName)?.Fsm;
            return fsm != null;
        }
        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(target.Path);
            writer.Write(target.Name);
        }

        public override Packet Deserialize(BinaryReader reader)
        {
            string gameObjectPath = reader.ReadString();
            string fsmName = reader.ReadString();

            Fingerprint = new FsmFingerprint(gameObjectPath, fsmName);

            return null!;
        }
    }
    public class FSMEventPacket(FsmFingerprint print, string eventName) : FSMPacket(print) {
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
                Fingerprint,
                eventName
            );
        }

        public override void ClientHandler(NetworkConnection connection)
        {
            Logger.Msg("Firing:", Fingerprint.Path, Fingerprint.Name, EventName, "on fsm:", FSM);
            if (FSM == null)
                Logger.Warn("Null FSM!", Fingerprint.Path, Fingerprint.Name, EventName);
            FSM?.Event(EventName);
        }

        public override void ServerHandler(NetworkConnection connection)
        {
            FSM?.Event(EventName);
            NetworkUtils.LocalServer?.SendExcept(this, connection);
        }
    }
    public class FSMStatusPacket(FsmFingerprint print, bool enabled) : FSMPacket(print) {
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
                Fingerprint,
                enabled
            );
        }

        public override void ClientHandler(NetworkConnection connection)
        {
            Logger.Msg("Starting:", Fingerprint.Path, Fingerprint.Name, "with", "on fsm:", FSM);
            if (FSM == null)
                Logger.Warn("Null FSM!", Fingerprint.Path, Fingerprint.Name);
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
    public class FSMStatePacket(FsmFingerprint print, FsmStateType state, string stateName) : FSMPacket(print) {
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
                Fingerprint,
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
            NetworkUtils.LocalServer?.SendExcept(this, connection);
        }
    }
}
