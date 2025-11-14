using HutongGames.PlayMaker;
using JetBrains.Annotations;
using Newtonsoft.Json;
using SilkBound.Types;
using SilkBound.Types.Data;
using SilkBound.Types.Language;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using UnityEngine;

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
        public ushort ID;
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
        public bool HasAuthority(AuthorityNode required, Weaver? target = null)
        {
            AuthorityNode senderAuthority = GetSenderAuthority(target ?? Sender ?? NetworkUtils.LocalClient);
            return required == AuthorityNode.Any || senderAuthority == required;
        }
        public bool ClientHasAuthority(AuthorityNode required, Weaver target)
        {
            AuthorityNode senderAuthority = GetSenderAuthority(target ?? Sender ?? NetworkUtils.LocalClient);
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
        /// Whether or not to use the gzip stream when packing and unpacking the packet.
        /// </summary>
        public virtual bool IsGzipped => false;

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

            _writer = writer;

            Serialize(writer);
        }

        public bool Cancelled { get; private set; } = false;

        /// <summary>
        /// Prevents a packet from sending when called in <see cref="Serialize(BinaryWriter)"/>
        /// </summary>
        protected void Abrupt()
        {
            Cancelled = true;
        }

        /// <summary>
        /// Reads the deserialized packet data into <paramref name="reader"/> and returns a new <see cref="Packet"/>.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader"/> for the current deserialization context.</param>
        /// <returns>A <see cref="Packet"/> representing the data from <paramref name="reader"/>.</returns>
        public abstract Packet Deserialize(BinaryReader reader);
        internal Packet Deserialize(Guid clientId, BinaryReader reader)
        {
            Sender ??= Server.CurrentServer?.GetWeaver(clientId)!;
            if (!ClientHasAuthority(ReadAuthority, NetworkUtils.LocalClient) || !ClientHasAuthority(SendAuthority, Sender))
            {
                throw new UnauthorizedAccessException($"Packet of type {GetType().Name} does not allow authority {PacketAuthority}.");
            }

            _reader = reader;

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

        private BinaryReader _reader;
        private BinaryWriter _writer;

        // GENERIC
        public void Write<T>(T value)
        {
            switch (value)
            {
                case int val: _writer.Write(val); break;
                case float val: _writer.Write(val); break;
                case bool val: _writer.Write(val); break;
                case byte val: _writer.Write(val); break;
                case Guid val: WriteGuid(val); break;
                case Enum val: WriteEnum(val); break;
                case Vector3 val:
                    _writer.Write(val.x);
                    _writer.Write(val.y);
                    _writer.Write(val.z);
                    break;
                case Vector2 val:
                    _writer.Write(val.x);
                    _writer.Write(val.y);
                    break;
                case string val: _writer.Write(val); break;
                case ushort val: _writer.Write(val); break;
                case short val: _writer.Write(val); break;
                case char val: _writer.Write(val); break;
                case uint val: _writer.Write(val); break;
                case long val: _writer.Write(val); break;
                case double val: _writer.Write(val); break;
                case sbyte val: _writer.Write(val); break;
                case ulong val: _writer.Write(val); break;
                case decimal val: _writer.Write(val); break;
                default:
                    throw new InvalidOperationException(
                    $"Type {typeof(T).Name} does not have a registered serializer");
            }
        }
        public void Write<T>(T? value) where T : struct
        {
            Write(value.HasValue);
            if (value.HasValue)
                Write(value.Value);
        }

        private static readonly Dictionary<Type, Func<BinaryReader, Type, object>> _readers = new() {
                { typeof(int), (r, t) => r.ReadInt32() },
                { typeof(float), (r, t) => r.ReadSingle() },
                { typeof(bool), (r, t) => r.ReadBoolean() },
                { typeof(byte), (r, t) => r.ReadByte() },
                { typeof(Guid), (r, t) => ReadGuid(r) },
                { typeof(Enum), (r, t) => ReadEnum(r, t) },
                { typeof(Vector3), (r, t) =>
                    new Vector3(r.ReadSingle() ,r.ReadSingle() ,r.ReadSingle()) },
                { typeof(Vector2), (r, t) =>
                    new Vector2(r.ReadSingle() ,r.ReadSingle()) },
                { typeof(string), (r, t) => r.ReadString() },
                { typeof(ushort), (r, t) => r.ReadUInt16() },
                { typeof(short), (r, t) => r.ReadInt16() },
                { typeof(char), (r, t) => r.ReadChar() },
                { typeof(uint), (r, t) => r.ReadUInt32() },
                { typeof(long), (r, t) => r.ReadInt64() },
                { typeof(double), (r, t) => r.ReadDouble() },
                { typeof(sbyte), (r, t) => r.ReadSByte() },
                { typeof(ulong), (r, t) => r.ReadUInt64() },
                { typeof(decimal), (r, t) => r.ReadDecimal() }
        };

        public object Read(Type t)
        {
            if (_readers.TryGetValue(t, out var readerFunc))
                return readerFunc(_reader, t);

            throw new InvalidOperationException(
                    $"Type {t.Name} does not have a registered serializer");
        }
        public T Read<T>()
        {
            return (T) Read(typeof(T));
        }
        public T? ReadNull<T>() where T : struct
        {
            var underlying = Nullable.GetUnderlyingType(typeof(T));
            if (underlying == null)
                return null;

            if (Read<bool>())
                return (T) Read(underlying);

            return null;
        }

        // ENUM
        private void WriteEnum(Enum obj)
        {
            _writer.Write(Convert.ToInt32(obj));
        }

        private static T ReadEnum<T>(BinaryReader r) where T : struct, Enum
        {
            int raw = r.ReadInt32();
            return (T) Enum.ToObject(typeof(T), raw);
        }
        private static object ReadEnum(BinaryReader r, Type t)
        {
            int raw = r.ReadInt32();
            return Enum.ToObject(t, raw);
        }

        // GUID
        private void WriteGuid(Guid? value)
        {
            _writer.Write((value ?? Guid.Empty).ToByteArray());
        }

        private static Guid ReadGuid(BinaryReader r)
        {
            return new Guid(r.ReadBytes(16));
        }

        // GENERIC
        public void Write(object? value, params JsonConverter[] converters)
        {
            var data = ChunkedTransfer.Serialize(value, converters);
            _writer.Write(data.Length);
            _writer.Write(data);
        }
        public T? Read<T>(params JsonConverter[] converters)
        {
            var len = _reader.ReadInt32();
            return ChunkedTransfer.Deserialize<T>(_reader.ReadBytes(len));
        }

        #endregion
    }
}
