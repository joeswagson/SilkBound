using SilkBound.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Network.Packets.Impl.Sync.Attacks
{
    public class NailSlashPacket(Guid weaver, NailSlash slash) : Packet
    {
        public Guid weaver = weaver;
        public NailSlash slash = slash;
        public override Packet Deserialize(BinaryReader reader)
        {
            return null!;

            //Guid weaverId = Guid.Parse(reader.ReadString());
            //string path = reader.ReadString();

            //Weaver? weaver = Server.CurrentServer!.GetWeaver(weaverId);
            //if(weaver == null || weaver.Mirror == null)
            //{
            //    throw new Exception("Weaver not found for packet.");
            //}

            //return new NailSlashPacket(weaverId, weaver.Mirror.GetNailAttack<NailSlash>(path)!);
        }

        public override void Serialize(BinaryWriter writer)
        {
            //writer.Write(weaver.ToString());
            //writer.Write(slash.transform.parent.name + "/" + slash.gameObject.name);
        }
    }
}
