using SilkBound.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Network.Packets.Impl.Sync.Attacks
{
    public class DownspikePacket(Guid weaver, Downspike slash) : Packet
    {
        public Guid weaver = weaver;
        public Downspike slash = slash;
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

            //return new DownspikePacket(weaverId, weaver.Mirror.GetNailAttack<Downspike>(path)!);
        }

        public override void Serialize(BinaryWriter writer)
        {
            //writer.Write(weaver.ToString());
            //writer.Write(slash.transform.parent.name + "/" + slash.gameObject.name);
        }
    }
}
