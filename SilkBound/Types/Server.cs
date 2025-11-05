using SilkBound.Network;
using SilkBound.Types.NetLayers;
using SilkBound.Utils;
using Steamworks;
using System;
using System.Collections.Generic;
using SilkBound.Addons.AddonLoading;
using SilkBound.Network.Packets.Impl.Steam;
using SilkBound.Managers;
using SilkBound.Network.Packets.Handlers;
using UnityEngine.SceneManagement;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SilkBound.Types
{
    public class Server
    {
        /// <summary>
        /// Create a server instance for a host connection.
        /// </summary>
        /// <param name="connection">The servers <see cref="NetworkServer"/></param>
        public Server(NetworkServer connection)
        {
            Connection = connection;
        }

        /// <summary>
        /// Creates a generic server instance for client use.
        /// </summary>
        /// <param name="connection"></param>
        public Server(NetworkConnection connection)
        {
            Connection = connection;
        }

        /// <summary>
        /// The currently connected server.
        /// </summary>
        public static Server CurrentServer = null!;

        /// <summary>
        /// Returns <c>true</c> if <see cref="CurrentServer"/> isn't null.
        /// </summary>
        public static bool IsOnline => CurrentServer != null;
        /// <summary>
        /// Returns <c>true</c> if you are the only connection.
        /// </summary>
        public static bool Lonely => CurrentServer.Connections.Count <= 1;

        /// <summary>
        /// List containing every connection, including the local one.
        /// </summary>
        public List<Weaver> Connections = [];

        /// <summary>
        /// Find a connected <see cref="Weaver"/> from a specified client ID.
        /// </summary>
        /// <param name="clientId">The target id of the client.</param>
        /// <returns>A non-null <see cref="Weaver"/> if the client ID is found, otherwise <see langword="null"/></returns>
        public Weaver? GetWeaver(Guid clientId)
        {
            return Connections.Find(c => c.ClientID == clientId);
        }
        /// <summary>
        /// Find a connected <see cref="Weaver"/> from a specified NetworkConnection instance.
        /// </summary>
        /// <param name="connection">The same connection object stored on the client.</param>
        /// <returns>A non-null <see cref="Weaver"/> if the <paramref name="connection"/> object reference matches <see cref="Weaver.Connection"/>, otherwise <see langword="null"/></returns>
        public Weaver? GetWeaver(NetworkConnection connection)
        {
            return Connections.Find(c => c.Connection == connection);
        }

        /// <summary>
        /// Server method to disconnect a client from the current server and synchronize the event with all clients.
        /// </summary>
        /// <param name="weaver">The target client.</param>
        public void Kick(Weaver weaver)
        {
            if (!NetworkUtils.IsServer)
                return;

            if (Connection is SteamServer server)
            {
                CSteamID weaverId = ((SteamConnection)weaver.Connection).RemoteId;

                foreach (Weaver connection in Connections)
                {
                    if (weaver != NetworkUtils.LocalClient)
                    {
                        connection.Connection.Send(new SteamKickS2CPacket(weaverId.m_SteamID));
                        break;
                    }
                }

                Connections.Remove(weaver);
            }
            else
            {
                
            }
        }

        /// <summary>
        /// The current servers settings, otherwise the default specified by <see cref="ServerSettings"/>.
        /// </summary>
        public ServerSettings Settings { get; set; } = Silkbound.Config.HostSettings;

        /// <summary>
        /// Client hosting the current server.
        /// </summary>
        public Weaver Host { get; internal set; } = null!;

        /// <summary>
        /// Wrapper for the backing connections <see cref="NetworkConnection.Host"/> property.
        /// If the underlying network layer does not require this field and doesn't implement it, it will be <see langword="null"/>.
        /// </summary>
        public string Address { get; internal set; } = null!;

        /// <summary>
        /// Wrapper for the backing connectsion <see cref="NetworkConnection.Port"/> property.
        /// </summary>
        public int? Port { get; internal set; }

        /// <summary>
        /// The network layer associated with the server.
        /// </summary>
        public NetworkConnection Connection { get; internal set; } = null!;

        public void Shutdown()
        {
            Logger.Msg("Shutting down server...");
        }

        #region Async Connectors

        public static async Task<Server> ConnectPipeAsync(string host, string name)
        {
            return await ConnectAsync(new NamedPipeServer(host), name);
        }
        public static async Task<Server> ConnectP2PAsync(string name)
        {
            return await ConnectAsync(new SteamServer(), name);
        }
        public static async Task<Server> ConnectTCPAsync(string host, string name, int? port = null)
        {
            return await ConnectAsync(new TCPServer(host, new ServerPacketHandler(), port), name);
        }

        public static async Task<Server> ConnectAsync(NetworkServer connection, string name)
        {
            Weaver host = await NetworkUtils.ConnectAsync(connection, name);
            NetworkUtils.LocalServer = connection;
            CurrentServer.Host = host;
            AddonManager.LoadAddons();

            return CurrentServer;
        }
        #endregion

        #region Synchronous Connectors

        /// <summary>
        /// Connect to a server via Named Pipes (Local machine only).
        /// </summary>
        /// <param name="host">The pipes name.</param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Server ConnectPipe(string host, string name)
        {
            return Connect(new NamedPipeServer(host), name);
        }
        public static Server ConnectP2P(string name)
        {
            return Connect(new SteamServer(), name);
        }
        public static Server ConnectTCP(string host, string name, int? port = null)
        {
            return Connect(new TCPServer(host, new ServerPacketHandler(), port), name);
        }

        public static Server Connect(NetworkServer connection, string name)
        {
            Weaver host = NetworkUtils.Connect(connection, name);
            NetworkUtils.LocalServer = connection;
            CurrentServer.Host = host;
            AddonManager.LoadAddons();

            return CurrentServer;
        }
        #endregion

        #region game functions
        public int GetPlayersInScene()
        {
            return GetPlayersInScene(SceneManager.GetActiveScene().name);
        }
        public int GetPlayersInScene(string sceneName)
        {
            int count = 0;
            foreach (Weaver weaver in Connections)
            {
                if (weaver.Mirror?.Scene == sceneName)
                    count++;
            }
            return count;
        }

        #endregion
    }
}
