using SilkBound.Utils;
using System.Collections.Generic;
using System.IO;

namespace SilkBound.Managers
{
    public class MultiplayerSaveGameData(SaveGameData data) {
        public SaveGameData Data { get; } = data; // god i love setting a getter only property! (it makes sense but i bet this would kill a newgen)
        public Dictionary<string, SceneState> SceneStates { get; } = SceneStateManager.States; // look at that i did it again!
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
            using FileStream fs = new(path, FileMode.Create, FileAccess.Write);
            using BinaryWriter writer = new(fs);
            writer.Write(MagicByteManager.SAVE_SIGNATURE);

            List<byte[]> chunks = ChunkedTransfer.Pack(mpdata);
            writer.Write(chunks.Count);
            foreach (byte[] chunk in chunks) {
                writer.Write(chunk.Length);
                writer.Write(chunk);
            }
        }

        public static bool SaveExists(int id)
        {
            if (!Silkbound.Config.UseMultiplayerSaving)
                return false;

            return File.Exists(GetSavePath(id));
        }

        public static MultiplayerSaveGameData? ReadFromFile(int id)
        {
            return ReadFromFile(GetSavePath(id));
        }
        public static MultiplayerSaveGameData? ReadFromFile(string path)
        {
            using FileStream fs = new(path, FileMode.Open, FileAccess.Read);
            using BinaryReader reader = new(fs);
            reader.ReadBytes(MagicByteManager.SAVE_SIGNATURE.Length);

            List<byte[]> chunks = [];

            int chunkCount = reader.ReadInt32();
            for (int i = 0; i < chunkCount; i++) {
                int length = reader.ReadInt32();
                byte[] chunk = reader.ReadBytes(length);
                chunks.Add(chunk);
            }

            return ChunkedTransfer.Unpack<MultiplayerSaveGameData>(chunks);
        }
        public static void CreateFromData(int id, MultiplayerSaveGameData mpdata)
        {
            CreateFromData(GetSavePath(id), mpdata);
        }
        public static void CreateFromData(string path, MultiplayerSaveGameData mpdata)
        {
            using FileStream fs = new(path, FileMode.Create, FileAccess.Write);
            using BinaryWriter writer = new(fs);
            writer.Write(MagicByteManager.SAVE_SIGNATURE);

            List<byte[]> chunks = ChunkedTransfer.Pack(mpdata);
            writer.Write(chunks.Count);
            foreach (byte[] chunk in chunks) {
                writer.Write(chunk.Length);
                writer.Write(chunk);
            }
        }
    }
}
