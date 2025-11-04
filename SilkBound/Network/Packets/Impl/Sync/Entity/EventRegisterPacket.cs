using SilkBound.Extensions;
using SilkBound.Network.Packets;
using SilkBound.Types;
using System.IO;
using System.Linq;

namespace SilkBound.Network.Packets.Impl.Sync.Entity {
    internal class EventRegisterPacket(string registerPath, string eventName) : Packet {
        public EventRegister? Register => UnityObjectExtensions.FindComponents<EventRegister>(registerPath)?.First(reg => reg.SubscribedEvent == eventName);
        public override Packet Deserialize(BinaryReader reader) {
            string registerPath = reader.ReadString();
            string eventName = reader.ReadString();

            return new EventRegisterPacket(registerPath, eventName);
        }

        public override void Serialize(BinaryWriter writer) {
            writer.Write(registerPath);
            writer.Write(eventName);
        }

        public override void ClientHandler(NetworkConnection connection) {
            Register?.ReceiveEvent();
        }
        public override void ServerHandler(NetworkConnection connection) {
            Register?.ReceiveEvent();
            Relay(connection);
        }
    }
}