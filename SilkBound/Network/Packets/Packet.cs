using JetBrains.Annotations;
using Newtonsoft.Json;
using SilkBound.Types;
using SilkBound.Types.Data;
using SilkBound.Types.Language;
using SilkBound.Utils;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace SilkBound.Network.Packets {
    /// <summary>
    /// Client types. Serves as the filtering enum aswell.
    /// </summary>
    public enum AuthorityNode {
        Any = 0,
        Server = 1,
        Client = 2,
    }

    /// <summary>
    /// Represents a packets direction in the form of <c>Sender1 -> (to/2) Reciever</c> where both ends are the <see cref="AuthorityNode"/> of the corresponding client
    /// </summary>
    public enum Authority {
        C2C = 0x00, // Client to Client
        C2S = 0x01, // Client to Server
        S2C = 0x10, // Server to Client
        S2S = 0x11, // Server to Server (support)
    }
    public abstract class Packet {
        protected Packet() { }
        
        /// <summary>
        /// Resolve the authority type of a client.
        /// </summary>
        /// <returns>The clients authority as enum <see cref="AuthorityNode"/>.</returns>
        public static AuthorityNode GetSenderAuthority(Weaver sender)
        {
            return (Server.CurrentServer?.Host == sender || NetworkUtils.IsServer) ? AuthorityNode.Server : AuthorityNode.Client;
        }
        /// <summary>
        /// The send and recieve type for a client and the local client. Throws <see cref="InvalidOperationException"/> if <see cref="Sender"/> or <see cref="NetworkUtils.LocalClient"/> are null.
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
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

        /// <summary>
        /// Gets the authority of a client.
        /// </summary>
        /// <param name="sender">The client to check the authority of.</param>
        /// <returns>The authority of <paramref name="sender"/></returns>
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

        /// <summary>
        /// Checks if the packet sender fits a specified <see cref="AuthorityNode"/>.
        /// </summary>
        /// <param name="required">The required node for permission.</param>
        /// <returns>Whether <see cref="Sender"/> (or <see cref="NetworkUtils.LocalClient"/>) fits the required authority.</returns>
        public bool HasAuthority(AuthorityNode required)
        {
            AuthorityNode senderAuthority = GetSenderAuthority(Sender ?? NetworkUtils.LocalClient);
            return required == AuthorityNode.Any || senderAuthority == required;
        }

        /// <summary>
        /// Required authority to read this packet.
        /// </summary>
        public virtual AuthorityNode ReadAuthority { get; } = AuthorityNode.Any;

        /// <summary>
        /// Required authority to send this packet.
        /// </summary>
        public virtual AuthorityNode SendAuthority { get; } = AuthorityNode.Any;

        /// <summary>
        /// The combined authority of the sender and reciever as an enum. Throws <see cref="InvalidOperationException"/> if <see cref="Sender"/> or <see cref="NetworkUtils.LocalClient"/> are null.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public Authority PacketAuthority => GetAuthority(Sender);

        /// <summary>
        /// The client who sent the packet. 
        /// Sender may very rarely be <see langword="null"/> if the packet has not been sent yet.
        /// </summary>
        public Weaver Sender { get; protected set; } = null!;

        /// <summary>
        /// Writes any packet data into <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter"/> for the current serialization context.</param>
        public abstract void Serialize(BinaryWriter writer);
        internal void SerializeInternal(BinaryWriter writer)
        {
            if (!HasAuthority(SendAuthority))
            {
                throw new UnauthorizedAccessException($"Packet of type {GetType().Name} cannot be sent by {PacketAuthority} authority.");
            }

            Sender ??= NetworkUtils.LocalClient;

            Serialize(writer);
        }

        /// <summary>
        /// Reads the deserialized packet data into <paramref name="reader"/> and returns a new <see cref="Packet"/>.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader"/> for the current deserialization context.</param>
        /// <returns>A <see cref="Packet"/> representing the data from <paramref name="reader"/>.</returns>
        public abstract Packet Deserialize(BinaryReader reader);
        internal Packet Deserialize(Guid clientId, BinaryReader reader)
        {
            Sender ??= Server.CurrentServer?.GetWeaver(clientId)!; // thank god i fixed this when the only authed packet was network ownership lol ANY client could send a packet of any auth with a modded client to bypass the serialize check
            if (!HasAuthority(ReadAuthority))
            {
                throw new UnauthorizedAccessException($"Packet of type {GetType().Name} cannot be sent by {PacketAuthority} authority.");
            }


            Packet packet = Deserialize(reader);
            packet.Sender = Sender;

            return packet;
        }

        /// <summary>
        /// Optional default handler when the packet is recieved as the server.
        /// </summary>
        /// <param name="connection">The senders <see cref="NetworkConnection"/></param>
        public virtual void ServerHandler(NetworkConnection connection) { }
        /// <summary>
        /// Optional default handler when the packet is recieved as the client.
        /// </summary>
        /// <param name="connection">The current <see cref="NetworkConnection"/> to the server. Equivalent to <see cref="NetworkUtils.LocalConnection"/></param>
        public virtual void ClientHandler(NetworkConnection connection) { }

        /// <summary>
        /// The relay tunnel used when determining how to forward a packet.
        /// </summary>
        /// <param name="connection">The connection being written to.</param>
        protected virtual void Tunnel(NetworkConnection connection)
        {
            if (NetworkUtils.IsServer)
                NetworkUtils.LocalServer.SendExcept(this, connection);
        }

        /// <summary>
        /// Forwards a packet to another network connection.
        /// Must be initiated by the server.
        /// </summary>
        /// <param name="connection">The connection being written to.</param>
        public void Relay(NetworkConnection connection)
        {
            if (!NetworkUtils.IsServer)
            {
                Logger.Warn("Client attempted to relay packet", GetType(), "- preventing");
                return;
            }
            Tunnel(connection);
        }

        //public void Send(NetworkConnection connection)
        //{
        //    connection.Send(this);
        //}
        //public void SendExcept(NetworkConnection connection)
        //{
        //    if (NetworkUtils.IsServer)
        //        NetworkUtils.LocalServer.SendExcept(this, connection);
        //}

        #region Binary IO Shortcuts

        public static bool GetDeserializer([NotNullWhen(true)] out BinaryReader? reader)
        {
            reader = StackFlag<SerializerContext>.Value.Reader;
            if (StackFlag<SerializerContext>.RaisedWithValue && reader != null)
                return true;

            return false;
        }

        public static bool GetSerializer([NotNullWhen(true)] out BinaryWriter? writer)
        {
            writer = StackFlag<SerializerContext>.Value.Writer;
            if (StackFlag<SerializerContext>.RaisedWithValue && writer != null)
                return true;

            return false;
        }

        // ENUM
        public static void Write(Enum obj)
        {
            if (!GetSerializer(out BinaryWriter? writer))
                return;

            writer.Write(Convert.ToInt32(obj));
        }

        public static T ReadEnum<T>() where T : struct, Enum
        {
            if (!GetDeserializer(out BinaryReader? reader))
                return default;

            int raw = reader.ReadInt32();
            return (T) Enum.ToObject(typeof(T), raw);
        }

        // BOOL
        public static void Write(bool value)
        {
            if (!GetSerializer(out BinaryWriter? writer))
                return;

            writer.Write(value);
        }

        public static bool ReadBool()
        {
            if (!GetDeserializer(out BinaryReader? reader))
                return default;

            return reader.ReadBoolean();
        }

        // BYTE
        public static void Write(byte value)
        {
            if (!GetSerializer(out BinaryWriter? writer))
                return;

            writer.Write(value);
        }

        public static byte ReadByte()
        {
            if (!GetDeserializer(out BinaryReader? reader))
                return default;

            return reader.ReadByte();
        }

        // SHORT
        public static void Write(short value)
        {
            if (!GetSerializer(out BinaryWriter? writer))
                return;

            writer.Write(value);
        }

        public static short ReadShort()
        {
            if (!GetDeserializer(out BinaryReader? reader))
                return default;

            return reader.ReadInt16();
        }

        // INT
        public static void Write(int value)
        {
            if (!GetSerializer(out BinaryWriter? writer))
                return;

            writer.Write(value);
        }

        public static int ReadInt()
        {
            if (!GetDeserializer(out BinaryReader? reader))
                return default;

            return reader.ReadInt32();
        }

        // LONG
        public static void Write(long value)
        {
            if (!GetSerializer(out BinaryWriter? writer))
                return;

            writer.Write(value);
        }

        public static long ReadLong()
        {
            if (!GetDeserializer(out BinaryReader? reader))
                return default;

            return reader.ReadInt64();
        }

        // FLOAT
        public static void Write(float value)
        {
            if (!GetSerializer(out BinaryWriter? writer))
                return;

            writer.Write(value);
        }

        public static float ReadFloat()
        {
            if (!GetDeserializer(out BinaryReader? reader))
                return default;

            return reader.ReadSingle();
        }

        // DOUBLE
        public static void Write(double value)
        {
            if (!GetSerializer(out BinaryWriter? writer))
                return;

            writer.Write(value);
        }

        public static double ReadDouble()
        {
            if (!GetDeserializer(out BinaryReader? reader))
                return default;

            return reader.ReadDouble();
        }

        // STRING
        public static void Write(string? value)
        {
            if (!GetSerializer(out BinaryWriter? writer))
                return;

            writer.Write(value ?? string.Empty);
        }

        public static string ReadString()
        {
            if (!GetDeserializer(out BinaryReader? reader))
                return string.Empty;

            return reader.ReadString();
        }

        // GUID
        public static void Write(Guid? value)
        {
            if (!GetSerializer(out BinaryWriter? writer))
                return;

            writer.Write((value ?? Guid.Empty).ToByteArray());
        }

        public static Guid ReadGuid()
        {
            if (!GetDeserializer(out BinaryReader? reader))
                return Guid.Empty;

            return new Guid(reader.ReadBytes(16));
        }

        // GENERIC
        public static void Write(object? value, params JsonConverter[] converters)
        {
            if (!GetSerializer(out BinaryWriter? writer))
                return;

            var data = ChunkedTransfer.Serialize(value, converters);
            writer.Write(data.Length);
            writer.Write(data);
        }
        public static T? Read<T>(params JsonConverter[] converters)
        {
            if (!GetDeserializer(out BinaryReader? reader))
                return default;

            var len = reader.ReadInt32();
            return ChunkedTransfer.Deserialize<T>(reader.ReadBytes(len));
        }

        #endregion
    }
}
