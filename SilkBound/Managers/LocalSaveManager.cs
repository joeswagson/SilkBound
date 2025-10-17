using MelonLoader.Utils;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Managers
{
    public class MultiplayerSaveGameData
    {
        public SaveGameData Data { get; }
        public Dictionary<string, SceneState> SceneStates { get; }
        public MultiplayerSaveGameData(SaveGameData data)
        {
            Data = data; // god i love setting a getter only property! (it makes sense but i bet this would kill a newgen)
            SceneStates = SceneStateManager.States; // look at that i did it again!
        }
    }
    public class LocalSaveManager
    {
        public static string SavePath = ModFolder.Saves.FullName;
        public static string GetSavePath(int id)
        {
            return $"{SavePath}/silkbound_{id}.save";
        }

        public static void WriteToFile(string path, SaveGameData data)
        {
            var mpdata = new MultiplayerSaveGameData(data);
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using(BinaryWriter writer = new BinaryWriter(fs))
            {
                writer.Write(MagicByteManager.SAVE_SIGNATURE);

                List<byte[]> chunks = ChunkedTransfer.Pack(mpdata);
                writer.Write(chunks.Count);
                foreach (byte[] chunk in chunks)
                {
                    writer.Write(chunk.Length);
                    writer.Write(chunk);
                }
            }
        }

        public static bool SaveExists(int id)
        {
            if (!ModMain.Config.UseMultiplayerSaving)
                return false;

            return File.Exists(GetSavePath(id));
        }

        public static MultiplayerSaveGameData? ReadFromFile(int id)
        {
            return ReadFromFile(GetSavePath(id));
        }
        public static MultiplayerSaveGameData? ReadFromFile(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                reader.ReadBytes(MagicByteManager.SAVE_SIGNATURE.Length);

                List<byte[]> chunks = new List<byte[]>();

                int chunkCount = reader.ReadInt32();
                for (int i = 0; i < chunkCount; i++)
                {
                    int length = reader.ReadInt32();
                    byte[] chunk = reader.ReadBytes(length);
                    chunks.Add(chunk);
                }

                return ChunkedTransfer.Unpack<MultiplayerSaveGameData>(chunks);
            }
        }
        public static void CreateFromData(int id, MultiplayerSaveGameData mpdata)
        {
            CreateFromData(GetSavePath(id), mpdata);
        }
        public static void CreateFromData(string path, MultiplayerSaveGameData mpdata)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                writer.Write(MagicByteManager.SAVE_SIGNATURE);

                List<byte[]> chunks = ChunkedTransfer.Pack(mpdata);
                writer.Write(chunks.Count);
                foreach (byte[] chunk in chunks)
                {
                    writer.Write(chunk.Length);
                    writer.Write(chunk);
                }
            }
        }
    }
}
