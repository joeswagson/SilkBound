using MelonLoader.Utils;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Managers
{
    public class LocalSaveManager
    {
        public static string SavePath = ModFolder.Saves.FullName;
        public static string GetSavePath(int id)
        {
            return $"{SavePath}/silkbound_{id}.save";
        }

        public static void WriteToFile(string path, SaveGameData data)
        {
            using(FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using(BinaryWriter writer = new BinaryWriter(fs))
            {
                writer.Write(MagicByteManager.SAVE_SIGNATURE);

                List<byte[]> chunks = ChunkedTransfer.Pack(data);
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
            return File.Exists(GetSavePath(id));
        }

        public static SaveGameData? ReadFromFile(string path)
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

                return ChunkedTransfer.Unpack<SaveGameData>(chunks);
            }
        }
    }
}
