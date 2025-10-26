using System.IO;

namespace SilkBound.Network.Packets.Impl.Sync.Attacks
{
    public class DownspikePacket(string crest, string attack, bool cancel) : Packet
    {
        public Downspike Slash => Sender.Mirror!.GetNailAttack<Downspike>($"{crest}/{attack}")!;
        public bool Cancel => cancel;
        public override Packet Deserialize(BinaryReader reader)
        {
            string crest = reader.ReadString();
            string attack = reader.ReadString();
            bool cancel = reader.ReadBoolean();
            return new DownspikePacket(crest, attack, cancel);
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(crest);
            writer.Write(attack);
            writer.Write(cancel);
            //writer.Write(slash.transform.parent.name, slash.gameObject.name);
        }
    }
}
