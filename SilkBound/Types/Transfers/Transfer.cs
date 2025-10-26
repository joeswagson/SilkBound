using Newtonsoft.Json;
using SilkBound.Managers;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

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
        public virtual JsonConverter[] Converters => [];
        public Transfer()
        {
            TransferId = Guid.NewGuid();
            TransactionManager.Promise(TransferId.ToString("N"), this);
        }
        public static Transfer Create(Type original)
        {
            return FormatterServices.GetUninitializedObject(original) as Transfer ?? throw new Exception("Failed to create Transfer instance"); // once more can use an uninitialized object as the source Fetch should never be called for this object
        }

        public abstract object Fetch(params object[] args);
        public abstract void Completed(List<byte[]> unpacked, NetworkConnection connection);
        public void TransferCompleted(List<byte[]> unpacked, NetworkConnection connection)
        {
            Completed(unpacked, connection);
            TransactionManager.Revoke(TransferId.ToString("N"));
        }
    }
}
