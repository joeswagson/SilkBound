using SilkBound.Types;
using SilkBound.Utils;
using System;
using System.IO;

namespace SilkBound.Network.Packets {
    public enum AuthorityNode {
        Any = 0,
        Server = 1,
        Client = 2,
    }
    public enum Authority {
        C2C = 0x00, // Client to Client
        C2S = 0x01, // Client to Server
        S2C = 0x10, // Server to Client
        S2S = 0x11, // Server to Server (support)
    }
    public abstract class Packet {
        protected Packet() { }

        public static AuthorityNode GetSenderAuthority(Weaver sender)
        {
            return (Server.CurrentServer?.Host == sender || NetworkUtils.IsServer) ? AuthorityNode.Server : AuthorityNode.Client;
        }
        public static Authority GetAuthority(Weaver sender)
        {
            AuthorityNode a = GetSenderAuthority(sender);
            AuthorityNode b = GetSenderAuthority(NetworkUtils.LocalClient);

            return (a, b) switch {
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
            } else
            {
                return sender.ClientID != Server.CurrentServer!.Host?.ClientID ? AuthorityNode.Client : AuthorityNode.Server; // handle very rare chance for a c2c packet lol
            }
        }
        public bool HasAuthority(AuthorityNode required)
        {
            AuthorityNode senderAuthority = GetSenderAuthority(Sender ?? NetworkUtils.LocalClient);
            return required == AuthorityNode.Any || senderAuthority == required;
        }

        public virtual AuthorityNode ReadAuthority { get; } = AuthorityNode.Any;
        public virtual AuthorityNode SendAuthority { get; } = AuthorityNode.Any;
        public Authority PacketAuthority => GetAuthority(Sender);
        public Weaver Sender { get; protected set; } = null!;
        internal void SerializeInternal(BinaryWriter writer)
        {
            if (!HasAuthority(SendAuthority))
            {
                throw new UnauthorizedAccessException($"Packet of type {GetType().Name} cannot be sent by {PacketAuthority} authority.");
            }

            Sender ??= NetworkUtils.LocalClient;

            Serialize(writer);
        }
        public abstract void Serialize(BinaryWriter writer);
        internal Packet Deserialize(Guid clientId, BinaryReader reader)
        {
            if (!HasAuthority(ReadAuthority))
            {
                throw new UnauthorizedAccessException($"Packet of type {GetType().Name} cannot be sent by {PacketAuthority} authority.");
            }

            Sender ??= Server.CurrentServer?.GetWeaver(clientId);
            
            Packet packet = Deserialize(reader);
            packet.Sender = Sender;

            return packet;
        }

        public virtual void ServerHandler(NetworkConnection connection) { }
        public virtual void ClientHandler(NetworkConnection connection) { }
        protected virtual void Relay(NetworkConnection connection) { }
        public void RelayInternal(NetworkConnection connection)
        {
            if (!NetworkUtils.IsServer)
            {
                Logger.Warn("Client attempted to relay packet", GetType(), "- preventing");
                return;
            }
            Relay(connection);
        }

        public abstract Packet Deserialize(BinaryReader reader);

        public void Send(NetworkConnection connection)
        {
            connection.Send(this);
        }
        public void SendExcept(NetworkConnection connection)
        {
            if (NetworkUtils.IsServer)
                NetworkUtils.LocalServer.SendExcept(this, connection);
        }
    }
}
