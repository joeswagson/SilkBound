using SilkBound.Network.Packets;
using SilkBound.Types;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SilkBound.Utils {
    public struct NetworkStats(NetworkConnection connection) {
        public NetworkConnection Connection = connection;

        //public int PacketsSent = 
        //public int PacketsDropped = 0;

        //internal void HandlePacketTask(Task packetSend, byte[] data)
        //{
        //    if (packetSend.IsCompletedSuccessfully)
        //        Packents
        //    else
        //                PacketDropped(data);
        //}
        //public void PacketSent(byte[] data)
        //{

        //}
        //public void PacketDropped(byte[] data)
        //{

        //}
    }
}
