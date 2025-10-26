using SilkBound.Extensions;
using System.IO;

namespace SilkBound.Network.Packets.Impl.Sync.World
{
    public class StartBattlePacket(string battlePath) : Packet
    {
        public BattleScene? Battle => UnityObjectExtensions.FindComponent<BattleScene>(battlePath);
        public override Packet Deserialize(BinaryReader reader)
        {
            string battlePath = reader.ReadString();

            return new StartBattlePacket(battlePath);
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(battlePath);
        }
    }
}
