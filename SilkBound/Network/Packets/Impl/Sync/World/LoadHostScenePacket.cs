using System;
using System.IO;
using System.Text;
using UnityEngine.SceneManagement;

namespace SilkBound.Network.Packets.Impl.World;

public class LoadHostScenePacket : Packet
{
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
    public override void Serialize(BinaryWriter writer)
    {
        writer.Write(SceneName);
        writer.Write(GateName);
    }

    public override Packet Deserialize(BinaryReader reader)
    {
        return new LoadHostScenePacket(reader.ReadString(), reader.ReadString());
    }
}