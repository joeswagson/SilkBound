using SilkBound.Types;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Network.Packets
{
    public enum Authority
    {
        Client=0,
        Server=1
    }
    //every packet needs an empty constructor for deserialization to work (i think you can have static abstracts in c# 11 but for compatibility ill keep it this way for now)
    public abstract class Packet
    {
        protected Packet() { }
        public static Authority GetAuthority(Weaver sender)
        {
            if (NetworkUtils.LocalConnection is NetworkServer)
            {
                return Authority.Client;
            }
            else
            {
                return sender.ClientID != Server.CurrentServer!.Host?.ClientID ? Authority.Client : Authority.Server; // handle very rare chance for a c2c packet lol
            }
        }

        public Authority PacketAuthority => GetAuthority(Sender);
        public Weaver Sender { get; protected set; } = null!;
        internal void SerializeInternal(BinaryWriter writer)
        {
            Sender = NetworkUtils.LocalClient!;

            Serialize(writer);
        }
        public abstract void Serialize(BinaryWriter writer);
        internal Packet Deserialize(Guid clientId, BinaryReader reader)
        {
            Packet packet = Deserialize(reader);
            packet.Sender = Server.CurrentServer?.GetWeaver(clientId) ?? new Weaver("Unnamed" + UnityEngine.Random.Range(1000, 10000).ToString(), null, clientId);

            return packet;
        }
        public abstract Packet Deserialize(BinaryReader reader);

        public void Send(NetworkConnection connection)
        {
            connection.Send(this);
        }
    }
}
