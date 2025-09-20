using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Collections.LowLevel.Unsafe;
using static UnityEngine.UI.SaveSlotButton;

namespace SilkBound.Packets.Impl
{
    public class TransferSaveDataPacket : Packet
    {
        public class TransferData
        {
            public byte[]?[] Chunks = new byte[SilkConstants.CHUNK_TRANSFER][];
            public int TotalChunks;
        }
        public override string PacketName => "TransferSaveDataPacket";

        public byte[] Data;
        public int ChunkIndex;
        public int TotalChunks;
        public Guid TransferId;
        public TransferSaveDataPacket() { Data = new byte[0]; }
        public TransferSaveDataPacket(byte[] data, int chunkIndex, int totalChunks, Guid transferId)
        {
            Data = data;
            ChunkIndex = chunkIndex;
            TotalChunks = totalChunks;
            TransferId = transferId;
        }

        public override byte[] Serialize()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(ChunkIndex);
                writer.Write(TotalChunks);
                writer.Write(TransferId.ToString());
                writer.Write(Data.Length);
                writer.Write(Data);
                return stream.ToArray();
            }
        }

        public override Packet Deserialize(byte[] bytes)
        {
            using (MemoryStream stream = new MemoryStream(bytes))
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8))
            {
                int chunkIndex = reader.ReadInt32();
                int totalChunks = reader.ReadInt32();
                string transferId = reader.ReadString();
                int length = reader.ReadInt32();
                byte[] data = reader.ReadBytes(length);

                return new TransferSaveDataPacket(data, chunkIndex, totalChunks, Guid.Parse(transferId));
            }
        }
    }
}
