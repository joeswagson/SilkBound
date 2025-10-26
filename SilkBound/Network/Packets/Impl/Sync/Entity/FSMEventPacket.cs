using HutongGames.PlayMaker;
using SilkBound.Extensions;
using SilkBound.Types;
using SilkBound.Utils;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

// cant wait til someone abuses this and i have to make it lame and restricted to registered network objects under your name
namespace SilkBound.Network.Packets.Impl.Sync.Entity {
    public abstract class FSMPacket(string goPath, string fsmName) : Packet {
#pragma warning disable CS9124
        public string GameObjectPath = goPath;
        public string FSMName = fsmName;
#pragma warning restore CS9124
        public Fsm? FSM => FindFSM(goPath, fsmName);
        public static Fsm? FindFSM(string goPath, string fsmName) {
            return UnityObjectExtensions.FindComponents<PlayMakerFSM>(goPath)?.First(fsm => fsm.Fsm.Name == fsmName)?.Fsm;
        }
        public static bool FindFSM(string goPath, string fsmName, [NotNullWhen(true)] out Fsm? fsm) {
            fsm = UnityObjectExtensions.FindComponents<PlayMakerFSM>(goPath)?.First(fsm => fsm.Fsm.Name == fsmName)?.Fsm;
            return fsm != null;
        }
        public override void Serialize(BinaryWriter writer) {
            writer.Write(goPath);
            writer.Write(fsmName);
        }

        public override Packet Deserialize(BinaryReader reader) {
            GameObjectPath = reader.ReadString();
            FSMName = reader.ReadString();

            return null!;
        }
    }
    public class FSMEventPacket(string goPath, string fsmName, string eventName) : FSMPacket(goPath, fsmName) {
        public string EventName { get; } = eventName;

        public override void Serialize(BinaryWriter writer) {
            base.Serialize(writer);
            writer.Write(EventName);
        }

        public override Packet Deserialize(BinaryReader reader) {
            base.Deserialize(reader);
            string eventName = reader.ReadString();

            return new FSMEventPacket(
                GameObjectPath,
                FSMName,
                eventName
            );
        }

        public override void ClientHandler(NetworkConnection connection) {
            Logger.Msg("Firing:", GameObjectPath, FSMName, EventName, "on fsm:", FSM);
            if (FSM == null)
                Logger.Warn("Null FSM!", GameObjectPath, FSMName, EventName);
            FSM?.Event(EventName);
        }

        public override void ServerHandler(NetworkConnection connection) {
            FSM?.Event(EventName);
            NetworkUtils.LocalServer?.SendExcept(this, connection);
        }
    }
    public class FSMStatusPacket(string goPath, string fsmName, bool enabled) : FSMPacket(goPath, fsmName) {
        public bool Enabled { get; } = enabled;

        public override void Serialize(BinaryWriter writer) {
            base.Serialize(writer);
            writer.Write(Enabled);
        }

        public override Packet Deserialize(BinaryReader reader) {
            base.Deserialize(reader);
            bool enabled = reader.ReadBoolean();

            return new FSMStatusPacket(
                GameObjectPath,
                FSMName,
                enabled
            );
        }

        public override void ClientHandler(NetworkConnection connection) {
            Logger.Msg("Starting:", GameObjectPath, FSMName, "with", "on fsm:", FSM);
            if (FSM == null)
                Logger.Warn("Null FSM!", GameObjectPath, FSMName);
            if(Enabled)
                FSM?.Start();
            else
                FSM?.Stop();
        }

        public override void ServerHandler(NetworkConnection connection) {
            if (Enabled)
                FSM?.Start();
            else
                FSM?.Stop();
            NetworkUtils.LocalServer?.SendExcept(this, connection);
        }
    }
}
