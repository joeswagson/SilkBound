using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace SilkBound.Utils
{
    public class ChunkedTransfer
    {
        public const int CHUNK_SIZE = SilkConstants.CHUNK_TRANSFER;

        public static readonly JsonSerializerSettings SerializerSettings = new()
        {
            NullValueHandling = NullValueHandling.Include,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
#if DEBUG // swallow json serialization errors
            Error = (sender, args) => {
                Logger.Debug("Error at JSON prop:", args.ErrorContext.Path);
                args.ErrorContext.Handled = true;
            }
#endif
        };
        public static JsonSerializer CreateSerializer(params JsonConverter[] converters)
        {
            SerializerSettings.Converters = converters;
            return JsonSerializer.Create(SerializerSettings);
        }
        public static string ToString(object? data, Formatting formatting=Formatting.Indented, params JsonConverter[] converters)
        {
            SerializerSettings.Converters = converters;
            return JsonConvert.SerializeObject(data, formatting, SerializerSettings);
        }
        public static byte[] Serialize(object? data, params JsonConverter[] converters)
        {
            SerializerSettings.Converters = converters;
            string json = JsonConvert.SerializeObject(data, SerializerSettings);
            //Logger.Msg("Serialized JSON:", json);
            return CompressString(json);
        }

        public static T Deserialize<T>(byte[] rawData, params JsonConverter[] converters)
        {
            string json = DecompressString(rawData);
            //Logger.Msg("Deserialized JSON:", json);
            SerializerSettings.Converters = converters;
            return JsonConvert.DeserializeObject<T>(json, SerializerSettings)!;
        }

        public static List<byte[]> Pack(object data, params JsonConverter[] converters)
        {
            byte[] rawData = Serialize(data, converters);

            var chunks = new List<byte[]>();
            int totalChunks = (rawData.Length + CHUNK_SIZE - 1) / CHUNK_SIZE;

            for (int i = 0; i < totalChunks; i++)
            {
                int offset = i * CHUNK_SIZE;
                int length = Math.Min(CHUNK_SIZE, rawData.Length - offset);

                using var ms = new MemoryStream();
                using (var writer = new BinaryWriter(ms, Encoding.UTF8, true))
                {
                    writer.Write(i);
                    writer.Write(totalChunks);
                    writer.Write(length);
                    writer.Write(rawData, offset, length);
                }

                chunks.Add(ms.ToArray());
            }

            return chunks;
        }

        public static List<byte[]> Pack<T>(T data, params JsonConverter[] converters)
        {
            byte[] rawData = Serialize(data, converters);

            var chunks = new List<byte[]>();
            int totalChunks = (rawData.Length + CHUNK_SIZE - 1) / CHUNK_SIZE;

            for (int i = 0; i < totalChunks; i++)
            {
                int offset = i * CHUNK_SIZE;
                int length = Math.Min(CHUNK_SIZE, rawData.Length - offset);

                using var ms = new MemoryStream();
                using (var writer = new BinaryWriter(ms, Encoding.UTF8, true))
                {
                    writer.Write(i);
                    writer.Write(totalChunks);
                    writer.Write(length);
                    writer.Write(rawData, offset, length);
                }

                chunks.Add(ms.ToArray());
            }

            return chunks;
        }

        public static object? Unpack(List<byte[]> chunks, params JsonConverter[] converters)
        {
            if (chunks.Count == 0)
                throw new ArgumentException("No chunks to unpack.");

            chunks.Sort((a, b) =>
            {
                int indexA = BitConverter.ToInt32(a, 0);
                int indexB = BitConverter.ToInt32(b, 0);
                return indexA.CompareTo(indexB);
            });

            using var ms = new MemoryStream();
            foreach (var chunk in chunks)
            {
                using var reader = new BinaryReader(new MemoryStream(chunk), Encoding.UTF8, true);
                int chunkIndex = reader.ReadInt32();
                int totalChunks = reader.ReadInt32();
                int length = reader.ReadInt32();
                byte[] data = reader.ReadBytes(length);
                ms.Write(data, 0, data.Length);
            }

            return Deserialize<object>(ms.ToArray(), converters);
        }
        public static T? Unpack<T>(List<byte[]> chunks, params JsonConverter[] converters)
        {
            if (chunks.Count == 0)
                throw new ArgumentException("No chunks to unpack.");

            chunks.Sort((a, b) =>
            {
                int indexA = BitConverter.ToInt32(a, 0);
                int indexB = BitConverter.ToInt32(b, 0);
                return indexA.CompareTo(indexB);
            });

            using var ms = new MemoryStream();
            foreach (var chunk in chunks)
            {
                using var reader = new BinaryReader(new MemoryStream(chunk), Encoding.UTF8, true);
                int chunkIndex = reader.ReadInt32();
                int totalChunks = reader.ReadInt32();
                int length = reader.ReadInt32();
                byte[] data = reader.ReadBytes(length);
                ms.Write(data, 0, data.Length);
            }

            return Deserialize<T>(ms.ToArray(), converters);
        }

        public static byte[] CompressString(string str)
        {
            byte[] input = Encoding.UTF8.GetBytes(str);
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
                gzip.Write(input, 0, input.Length);
            return output.ToArray();
        }

        public static string DecompressString(byte[] data)
        {
            using var input = new MemoryStream(data);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);
            return Encoding.UTF8.GetString(output.ToArray());
        }

        public static string[] NormalizeArray(object?[] input) => [.. input.Select(o=>NormalizeObject(o))];
        public static string FormatArray(object?[] elements, string separator=",", bool brackets = true)
        {
            var contents = string.Join(separator, elements.Select(o=>NormalizeObject(o)));
            return brackets ? $"{{{contents}}}" : contents;
        }
        public static string NormalizeObject(object? input, bool arrayBrackets=true) =>
            input switch {
                null => "null",
                object?[] array => FormatArray(array, brackets: arrayBrackets),
                _ => input.ToString() ?? "[object->string failure]"
            };
    }
}