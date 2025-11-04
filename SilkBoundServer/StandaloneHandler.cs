using SilkBound.Managers;
using SilkBound.Network;
using SilkBound.Network.Packets;
using SilkBound.Network.Packets.Impl.Communication;
using SilkBound.Types;
using SilkBound.Types.Transfers;
using SilkBound.Utils;
using Logger = SilkBound.Utils.Logger;

namespace SilkBoundServer
{

    public class StandaloneHandler : PacketHandler
    {
        public override void Initialize()
        {
        }

        [PacketHandler(typeof(HandshakePacket))]
        public void OnHandshakePacket(HandshakePacket packet, NetworkConnection connection)
        {
            Logger.Msg("Handshake Recieved (Server):", packet.ClientId, packet.ClientName, packet.HandshakeId);
            connection.Send(new HandshakePacket(packet.ClientId, packet.ClientName, packet.HandshakeId,
                Program.Guid)); // reply with same handshake id so the client can acknowledge handshake completion
            //now that we have the client id, we can create a client object for them
            Weaver client = new Weaver(packet.ClientName, connection, packet.ClientId);
            Server.CurrentServer.Connections.Add(client);
            TransferManager.Send(transfer: new ServerInformationTransfer(ServerState.GetCurrent()),
                connections: [connection]);
            NetworkUtils.LocalServer.SendExcept(new ClientConnectionPacket(client.ClientID, client.ClientName),
                connection);
        }

        [PacketHandler(typeof(Packet))]
        public void OnPacket(Packet packet, NetworkConnection connection)
        {
            packet.Relay(connection);
        }
    }
}