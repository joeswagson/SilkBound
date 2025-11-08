using SilkBound.Network.Packets.Impl.Communication;
using SilkBound.Types;
using SilkBound.Types.Transfers;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SilkBound.Managers
{
    public class TransferManager
    {
        public static async Task Send(Transfer transfer, params object[] fetchArgs) => await Send(transfer, null, fetchArgs);
        public static async Task Send(Transfer transfer, List<NetworkConnection>? connections, params object[] fetchArgs)
        {
            if (!NetworkUtils.Connected)
                return;

            Type transferType = transfer.GetType();
            List<byte[]> chunks = ChunkedTransfer.Pack(await transfer.Prepare(fetchArgs), transfer.Converters);
            Logger.Msg("Transfering", transferType.Name, "with id", transfer.TransferId, "in", chunks.Count, "chunks");
            for (int i = 0; i < chunks.Count; i++)
            {
                byte[] chunk = chunks[i];
                var packet = new TransferDataPacket(chunk, i, chunks.Count, transfer.TransferId, transferType);
                //Logger.Msg("sending chunk", i + 1, "of", chunks.Count);
                if (NetworkUtils.IsServer && connections != null)
                    await NetworkUtils.LocalServer.SendIncluding(packet, connections);
                else
                    await NetworkUtils.SendPacketAsync(packet);
            }
        }
    }
}
