using System;
using System.IO;

namespace SilkBound.Network.Packets.Impl.Communication
{
    public class TransferDataPacket : Packet
    {
        public byte[] Data;
        public int ChunkIndex;
        public int TotalChunks;
        public Guid TransferId;
        public Type TransferType;
        public TransferDataPacket() { Data = []; TransferType = GetType(); }
        public TransferDataPacket(byte[] data, int chunkIndex, int totalChunks, Guid transferId, Type transferType)
        {
            Data = data;
            ChunkIndex = chunkIndex;
            TotalChunks = totalChunks;
            TransferId = transferId;
            TransferType = transferType;
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(ChunkIndex);
            writer.Write(TotalChunks);
            writer.Write(TransferId.ToString());
            writer.Write(TransferType.FullName);
            writer.Write(Data.Length);
            writer.Write(Data);
        }

        public override Packet Deserialize(BinaryReader reader)
        {
            int chunkIndex = reader.ReadInt32();
            int totalChunks = reader.ReadInt32();
            string transferId = reader.ReadString();
            string transferTypeName = reader.ReadString();
            Type transferType = Type.GetType(transferTypeName) ?? throw new Exception("Packet passed an invalid TransferType.");
            int length = reader.ReadInt32();
            byte[] data = reader.ReadBytes(length);

            return new TransferDataPacket(data, chunkIndex, totalChunks, Guid.Parse(transferId), transferType);
        }
    }
}
