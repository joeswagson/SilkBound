using HutongGames.PlayMaker.Actions;
using SilkBound.Network.Packets.Impl;
using SilkBound.Network.Packets.Impl.Communication;
using SilkBound.Types;
using SilkBound.Types.Transfers;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Managers
{
    public class TransferManager
    {
        public static void Send(Transfer transfer, params object[] fetchArgs)
        {
            if (!NetworkUtils.IsConnected)
                return;

            Type transferType = transfer.GetType();
            List<byte[]> chunks = ChunkedTransfer.Pack(transfer.Fetch(fetchArgs), transfer.Converters);  
            Logger.Msg("Transfering", transferType.Name, "with id", transfer.TransferId, "in", chunks.Count, "chunks");
            for (int i = 0; i < chunks.Count; i++)
            {
                byte[] chunk = chunks[i];
                //Logger.Msg("sending chunk", i + 1, "of", chunks.Count);
                NetworkUtils.SendPacket(new TransferDataPacket(chunk, i, chunks.Count, transfer.TransferId, transferType));
            }
        }
    }
}
