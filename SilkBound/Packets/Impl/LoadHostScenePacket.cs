using System;
using System.IO;
using System.Text;
using MelonLoader.TinyJSON;
using SilkBound.Network.Packets;
using UnityEngine.SceneManagement;

namespace SilkBound.Packets.Impl;

public class LoadHostScenePacket : Packet
{
    public override string PacketName => "LoadHostScenePacket";

    private string _sceneName;
    public LoadHostScenePacket(string sceneName)
    {
        _sceneName = sceneName;
    }

    public LoadHostScenePacket()
    {
        _sceneName = String.Empty;
    }
    public override byte[] Serialize()
    {
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8))
        {
            writer.Write(_sceneName);
            return ms.ToArray();
        }
    }

    public override Packet Deserialize(byte[] data)
    {
        using(MemoryStream ms = new MemoryStream(data))
        using(BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
        {
            return new LoadHostScenePacket(reader.ReadString());
        }
    }
}