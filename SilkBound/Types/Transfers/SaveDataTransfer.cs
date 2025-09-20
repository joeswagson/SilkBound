using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Types.Transfers
{
    public class SaveDataTransfer
    {
        public class TransferData
        {
            public byte[][] Chunks = new byte[SilkConstants.CHUNK_TRANSFER][];
            public int TotalChunks;
        }

        public int HostHash;
        public string SceneName;
        public string SceneMarker;
        public SaveGameData Data;
        public TransferData? ChunkData;

        public SaveDataTransfer(Guid host, SaveGameData data, string sceneName, string sceneMarker)
        {
            HostHash = GetHostHash(host);
            Data = data;
            SceneName = sceneName;
            SceneMarker = sceneMarker;
        }

        public static int GetHostHash(Guid guid)
        {
            byte[] bytes = guid.ToByteArray();

            return BitConverter.ToInt32(bytes, 0)
                 ^ BitConverter.ToInt32(bytes, 4)
                 ^ BitConverter.ToInt32(bytes, 8)
                 ^ BitConverter.ToInt32(bytes, 12);
        }
    }
}
