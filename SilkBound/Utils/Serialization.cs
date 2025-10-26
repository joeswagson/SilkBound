using System.Collections.Generic;
using System.IO;

namespace SilkBound.Utils
{
    public class Serialization
    {
        public static void SerializeDictionary(BinaryWriter writer, Dictionary<string, string> dict)
        {
            if (dict == null || writer == null) return;

            writer.Write(dict.Count);
            foreach (var kv in dict)
            {
                writer.Write(kv.Key);
                writer.Write(kv.Value);
            }
        }

        public static Dictionary<string, string> DeserializeDictionary(BinaryReader reader)
        {
            var dict = new Dictionary<string, string>();
            if (reader == null) return dict;

            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                string key = reader.ReadString();
                string value = reader.ReadString();
                dict[key] = value;
            }

            return dict;
        }
    }
}
