using SilkBound.Types;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Network.Packets
{
    public enum AuthorityNode
    {
        Any = 0,
        Server = 1,
        Client = 2,
    }
    public enum Authority
    {
        C2C = 0x00, // Client to Client
        C2S = 0x01, // Client to Server
        S2C = 0x10, // Server to Client
        S2S = 0x11, // Server to Server (support)
    }
    //every packet needs an empty constructor for deserialization to work (i think you can have static abstracts in c# 11 but for compatibility ill keep it this way for now)
    public abstract class Packet
    {
        protected Packet() { }
        public static AuthorityNode GetSenderAuthority(Weaver sender)
        {
            return NetworkUtils.IsServer ? AuthorityNode.Server : AuthorityNode.Client;
        }
        public static Authority GetAuthority(Weaver sender)
        {
            AuthorityNode a = GetSenderAuthority(sender);
            AuthorityNode b = GetSenderAuthority(NetworkUtils.LocalClient);

            return (a, b) switch
            {
                (AuthorityNode.Client, AuthorityNode.Client) => Authority.C2C,
                (AuthorityNode.Client, AuthorityNode.Server) => Authority.C2S,
                (AuthorityNode.Server, AuthorityNode.Client) => Authority.S2C,
                (AuthorityNode.Server, AuthorityNode.Server) => Authority.S2S,
                _ => throw new InvalidOperationException($"Invalid authority combination: {a} -> {b}")
            };
        }
        public static AuthorityNode GetOutboundAuthority(Weaver sender)
        {
            if (NetworkUtils.LocalConnection is NetworkServer)
            {
                return AuthorityNode.Client;
            }
            else
            {
                return sender.ClientID != Server.CurrentServer!.Host?.ClientID ? AuthorityNode.Client : AuthorityNode.Server; // handle very rare chance for a c2c packet lol
            }
        }
        public bool HasAuthority(AuthorityNode required)
        {
            AuthorityNode senderAuthority = GetSenderAuthority(NetworkUtils.LocalClient);
            return required == AuthorityNode.Any || senderAuthority == required;
        }

        public virtual AuthorityNode ReadAuthority { get; } = AuthorityNode.Any;
        public virtual AuthorityNode SendAuthority { get; } = AuthorityNode.Any;
        public Authority PacketAuthority => GetAuthority(Sender);
        public Weaver Sender { get; protected set; } = null!;
        internal void SerializeInternal(BinaryWriter writer)
        {
            if(!HasAuthority(SendAuthority))
            {
                throw new UnauthorizedAccessException($"Packet of type {GetType().Name} cannot be sent by {PacketAuthority} authority.");
            }

            Sender = NetworkUtils.LocalClient!;

            Serialize(writer);
        }
        public abstract void Serialize(BinaryWriter writer);
        internal Packet Deserialize(Guid clientId, BinaryReader reader)
        {
            if (!HasAuthority(SendAuthority))
            {
                throw new UnauthorizedAccessException($"Packet of type {GetType().Name} cannot be sent by {PacketAuthority} authority.");
            }

            Sender = Server.CurrentServer?.GetWeaver(clientId) ?? new Weaver("Unnamed" + UnityEngine.Random.Range(1000, 10000).ToString(), null, clientId);
            Packet packet = Deserialize(reader);
            packet.Sender = Sender;

            return packet;
        }
        public abstract Packet Deserialize(BinaryReader reader);

        public void Send(NetworkConnection connection)
        {
            connection.Send(this);
        }
    }
}
