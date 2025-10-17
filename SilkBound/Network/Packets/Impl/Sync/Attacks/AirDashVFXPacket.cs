using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Network.Packets.Impl.Sync.Attacks
{
    public class AirDashVFXPacket(bool groundDash, bool airDash, bool wallSliding, bool dashDown, float num) : Packet
    {
        public bool GroundDash => groundDash;
        public bool AirDash => airDash;
        public bool WallSliding => wallSliding;
        public bool DashDown => dashDown;
        public float Scale => num;
        public override Packet Deserialize(BinaryReader reader)
        {
            bool groundDash = reader.ReadBoolean();
            bool airDash = reader.ReadBoolean();
            bool wallSliding = reader.ReadBoolean();
            bool dashDown = reader.ReadBoolean();
            float num = reader.ReadSingle();
            return new AirDashVFXPacket(groundDash, airDash, wallSliding, dashDown, num);
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(groundDash);
            writer.Write(airDash);
            writer.Write(wallSliding);
            writer.Write(dashDown);
            writer.Write(num);
        }
    }
}
