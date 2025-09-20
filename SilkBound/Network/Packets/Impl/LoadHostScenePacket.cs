using System;
using System.IO;
using System.Text;
using UnityEngine.SceneManagement;

namespace SilkBound.Network.Packets.Impl;

public class LoadHostScenePacket : Packet
{
    public override string PacketName => "LoadHostScenePacket";

    public string SceneName;
    public string GateName;
    public LoadHostScenePacket(string sceneName, string gateName)
    {
        SceneName = sceneName;
        GateName = gateName;
    }

    public LoadHostScenePacket()
    {
        SceneName = string.Empty;
        GateName = string.Empty;
    }
    public override byte[] Serialize()
    {
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8))
        {
            writer.Write(SceneName);
            writer.Write(GateName);
            return ms.ToArray();
        }
    }

    public override Packet Deserialize(byte[] data)
    {
        using(MemoryStream ms = new MemoryStream(data))
        using(BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
        {
            return new LoadHostScenePacket(reader.ReadString(), reader.ReadString());
        }
    }
}