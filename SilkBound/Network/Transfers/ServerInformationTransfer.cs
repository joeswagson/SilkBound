using Newtonsoft.Json;
using SilkBound.Managers;
using SilkBound.Network;
using SilkBound.Network.NetworkLayers;
using SilkBound.Types.JsonConverters;
using SilkBound.Utils;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SilkBound.Types.Transfers
{
    public struct ServerState
    {
        public static ServerState GetCurrent()
        {
            return new ServerState()
            {
                Host = SerializedWeaver.FromWeaver(Server.CurrentServer.Host),
                Settings = Server.CurrentServer.Settings,
            };
        }

        public SerializedWeaver Host;
        public ServerSettings Settings;
    }
    public class ServerInformationTransfer(ServerState state) : Transfer
    {
        static JsonConverter[] converters => [new GameObjectConverter(true)];
        public override JsonConverter[] Converters => converters;
        public override void Completed(List<byte[]> unpacked, NetworkConnection connection)
        {
            var state = ChunkedTransfer.Unpack<ServerState>(unpacked, converters);

            Server.CurrentServer.Settings = state.Settings;

            Server.CurrentServer.Host ??= state.Host.ToWeaver();
        }

        protected override Task<object> Fetch(params object[] args) => Task.FromResult<object>(state);
    }
}
