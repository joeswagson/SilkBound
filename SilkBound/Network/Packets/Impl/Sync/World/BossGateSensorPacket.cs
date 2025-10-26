using SilkBound.Behaviours;
using SilkBound.Extensions;
using System.IO;

namespace SilkBound.Network.Packets.Impl.Sync.World
{
    public class BossGateSensorPacket(string gatePath, bool sensorActivated) : Packet
    {
        public NetworkPropagatedGateSensor? GateSensor = UnityObjectExtensions.FindObjectFromFullName(gatePath)?
                                                                              .GetComponent<NetworkPropagatedGateSensor>();
        public bool SensorActivated => sensorActivated;
        public override Packet Deserialize(BinaryReader reader)
        {
            string gatePath = reader.ReadString();
            bool sensorActivated = reader.ReadBoolean();

            return new BossGateSensorPacket(gatePath, sensorActivated);
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(gatePath);
            writer.Write(sensorActivated);
        }
    }
}
