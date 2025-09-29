using Newtonsoft.Json;
using SilkBound.Managers;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Types.Transfers
{
    public abstract class Transfer
    {
        public class TransferData
        {
            public byte[][] Chunks = new byte[SilkConstants.CHUNK_TRANSFER][];
            public int TotalChunks;
        }

        public Guid TransferId;
        public TransferData? ChunkData;

        public Transfer()
        {
            TransferId = Guid.NewGuid();
            TransactionManager.Promise(TransferId.ToString("N"), this);
        }
        public static Transfer Create(Type original)
        {
            return Activator.CreateInstance(original) as Transfer ?? throw new Exception("Failed to create Transfer instance");
        }

        public abstract object Fetch(params object[] args);
        public abstract void Completed(List<byte[]> unpacked);
        public void TransferCompleted(List<byte[]> unpacked)
        {
            Completed(unpacked);
            TransactionManager.Revoke(TransferId.ToString("N"));
        }
    }
}
