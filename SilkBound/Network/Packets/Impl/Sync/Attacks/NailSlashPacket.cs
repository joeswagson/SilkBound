using SilkBound.Types;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Network.Packets.Impl.Sync.Attacks
{
    public class NailSlashPacket(string crest, string attack) : Packet
    {
        public NailSlash Slash => Sender.Mirror!.GetNailAttack<NailSlash>($"{crest}/{attack}")!;
        public override Packet Deserialize(BinaryReader reader)
        {
            string crest = reader.ReadString();
            string attack = reader.ReadString();
            return new NailSlashPacket(crest, attack);
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(crest);
            writer.Write(attack);
            //writer.Write(slash.transform.parent.name, slash.gameObject.name);
        }
    }
}
