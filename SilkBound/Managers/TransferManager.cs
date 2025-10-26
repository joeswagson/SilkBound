using SilkBound.Network.Packets.Impl.Communication;
using SilkBound.Types;
using SilkBound.Types.Transfers;
using SilkBound.Utils;
using System;
using System.Collections.Generic;

namespace SilkBound.Managers
{
    public class TransferManager
    {
        public static void Send(Transfer transfer, params object[] fetchArgs) => Send(transfer, null, fetchArgs);
        public static void Send(Transfer transfer, List<NetworkConnection>? connections, params object[] fetchArgs)
        {
            if (!NetworkUtils.Connected)
                return;

            Type transferType = transfer.GetType();
            List<byte[]> chunks = ChunkedTransfer.Pack(transfer.Fetch(fetchArgs), transfer.Converters);
            Logger.Msg("Transfering", transferType.Name, "with id", transfer.TransferId, "in", chunks.Count, "chunks");
            for (int i = 0; i < chunks.Count; i++)
            {
                byte[] chunk = chunks[i];
                var packet = new TransferDataPacket(chunk, i, chunks.Count, transfer.TransferId, transferType);
                //Logger.Msg("sending chunk", i + 1, "of", chunks.Count);
                if (NetworkUtils.IsServer && connections != null)
                    NetworkUtils.LocalServer.SendIncluding(packet, connections);
                else
                    NetworkUtils.SendPacket(packet);
            }
        }
    }
}
