using Newtonsoft.Json;
using SilkBound.Types;
using SilkBound.Types.Data;
using SilkBound.Types.Language;
using SilkBound.Utils;
using System;
using System.Diagnostics.CodeAnalysis;
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
        protected virtual void Relay(NetworkConnection connection) => NetworkUtils.LocalServer?.SendExcept(this, connection);
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
