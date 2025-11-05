using SilkBound.Network.Packets;
using SilkBound.Network.Packets.Handlers;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace SilkBound.Types.NetLayers
{
    public class NamedPipeServer : NetworkServer
    {
        public NamedPipeServer(string host) : base(new ServerPacketHandler(), host)
        {
            //Connect(host, null);
        }

        public NamedPipeServerStream? Stream;
        public override bool IsConnected => Stream != null && Stream.IsConnected;

        public override async Task ConnectImpl(string host, int? port)
        {
            Stream = new NamedPipeServerStream(
                host,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous
            );

            Logger.Msg("NamedPipeServerStream object created.");
            Logger.Msg("Waiting for client connection...");
            await Stream.WaitForConnectionAsync();
            Logger.Msg("Client connected!");

            _ = ReceiveLoop();
        }

        private async Task ReceiveLoop()
        {
            byte[] buffer = new byte[SilkConstants.PACKET_BUFFER];

            while (Stream!.IsConnected)
            {
                try
                {
                    int read = await Stream.ReadAsync(buffer, 0, buffer.Length);
                    if (read > 0)
                    {
                        byte[] data = new byte[read - 4];
                        Array.Copy(buffer, 4, data, 0, read - 4);
                        HandlePacket(data);
                    }
                }
                catch (IOException e)
                {
                    Logger.Warn($"NamedPipeServer receive loop ended: {e.Message}");
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Error($"NamedPipeServer receive loop fatal: {ex}");
                    break;
                }
            }
        }



        public override void Dispose()
        {
            Stream?.Dispose();
        }

        public override void Initialize()
        {

        }
        public override async Task Send(byte[] packetData)
        {
            if (Stream == null || !Stream.IsConnected)
            {
                Logger.Warn("Stream was null or not connected.");
                return;
            }

            //Logger.Msg("presend"); 
            try
            {
                await Stream.WriteAsync(packetData, 0, packetData.Length);
                await Stream.FlushAsync();
            } catch(Exception e)
            {
                Logger.Warn($"NamedPipeServer send error: {e.Message} {e.GetType().Name}");
                return;
            }
            //Logger.Msg("sent");
            //Logger.Msg("flushed");
        }


        //we dont need these because pipe communication is bilateral
        public override async Task SendIncluding(Packet packet, IEnumerable<NetworkConnection> include)
        {
            await Send(packet);
        }
        
        // if the pipe server wants to exclude a client, itll be the only one it can
        public override Task SendExcluding(Packet packet, IEnumerable<NetworkConnection> exclude)
        {
            return Task.CompletedTask;
        }
    }
}
