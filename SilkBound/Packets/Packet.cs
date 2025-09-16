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
    public abstract class Packet
    {
        public abstract string PacketName { get; }
        public abstract byte[] Serialize();
        public abstract Packet? Create(params object[] values);
        public abstract Packet Deserialize(byte[] data);

        public void Send(NetworkConnection connection)
        {
            connection.Send(this);
        }
    }
}
