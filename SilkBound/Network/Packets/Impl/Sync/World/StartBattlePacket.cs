using SilkBound.Extensions;
using SilkBound.Types;
using SilkBound.Utils;
using System.IO;
using System.Net.Sockets;

namespace SilkBound.Network.Packets.Impl.Sync.World
{
    public class StartBattlePacket(string battlePath) : Packet
    {
        public BattleScene? Battle => UnityObjectExtensions.FindComponent<BattleScene>(battlePath);
        public override Packet Deserialize(BinaryReader reader)
        {
            string battlePath = reader.ReadString();

            return new StartBattlePacket(battlePath);
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(battlePath);
        }

        public override void ClientHandler(NetworkConnection connection)
        {
            if (Sender.InScene(Battle?.gameObject.scene.name))
                Battle?.StartBattle();
        }

        public override void ServerHandler(NetworkConnection connection)
        {
            if (Sender.InScene(Battle?.gameObject.scene.name))
                Battle?.StartBattle();

            Relay(connection);
        }

        protected override void Tunnel(NetworkConnection connection) => this.Send(connection); // we dont let clients start battles so we'll have to send this to them aswell
    }
}
