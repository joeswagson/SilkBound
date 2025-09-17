using SilkBound.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Packets
{
    public enum Authority
    {
        Client=0,
        Server=1
    }
    //every packet needs an empty constructor for deserialization to work (i think you can have static abstracts in c# 11 but for compatibility ill keep it this way for now)
    public abstract class Packet
    {
        public abstract string PacketName { get; }
        public abstract byte[] Serialize();
        public abstract Packet Deserialize(byte[] data);

        public void Send(NetworkConnection connection)
        {
            connection.Send(this);
        }
    }
}
