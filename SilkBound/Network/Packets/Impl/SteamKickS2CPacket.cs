using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Network.Packets.Impl
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

        public override Packet Deserialize(BinaryReader reader)
        {
            return new SteamKickS2CPacket(reader.ReadUInt64(), reader.ReadString());
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(TargetSteamId ?? 0);
            writer.Write(Reason.Substring(0, Math.Min(200, Reason.Length)));
        }
    }
}
