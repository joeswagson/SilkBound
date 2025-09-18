using SilkBound.Packets;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Network.Packets
{
    public class SteamKickS2CPacket : Packet
    {
        public override string PacketName => "SteamKickS2CPacket";
        
        public ulong? TargetSteamId;
        public string Reason = "No reason specified.";

        public SteamKickS2CPacket() { }

        public SteamKickS2CPacket(ulong targetSteamId, string? reason = null)
        {
            TargetSteamId = targetSteamId;
            Reason = reason ?? Reason;
        }

        public override Packet Deserialize(byte[] data)
        {
            using(MemoryStream ms = new MemoryStream(data))
            using(BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
            {
                return new SteamKickS2CPacket(reader.ReadUInt64(), reader.ReadString());
            }
        }

        public override byte[] Serialize()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                writer.Write(TargetSteamId ?? 0);
                writer.Write(Reason.Substring(0, Math.Min(200, Reason.Length)));
                return ms.ToArray();
            }
        }
    }
}
