using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using SilkBound.Packets.Impl;

namespace SilkBound.Utils
{
    public class ChunkedTransfer
    {
        public const int CHUNK_SIZE = SilkConstants.CHUNK_TRANSFER;

        public static List<byte[]> Pack<T>(T data)
        {
            string json = JsonConvert.SerializeObject(data);
            byte[] rawData = CompressString(json);

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

        public static T? Unpack<T>(List<byte[]> chunks)
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

            string json = DecompressString(ms.ToArray());
            return JsonConvert.DeserializeObject<T>(json);
        }

        private static byte[] CompressString(string str)
        {
            byte[] input = Encoding.UTF8.GetBytes(str);
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
                gzip.Write(input, 0, input.Length);
            return output.ToArray();
        }

        private static string DecompressString(byte[] data)
        {
            using var input = new MemoryStream(data);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);
            return Encoding.UTF8.GetString(output.ToArray());
        }
    }
}