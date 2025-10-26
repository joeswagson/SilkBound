using SilkBound.Utils;
using System.Collections.Generic;

namespace SilkBound.Types.Transfers
{
    public class TestTransfer(Dictionary<string, string> data) : Transfer
    {
        public override void Completed(List<byte[]> unpacked, NetworkConnection connection)
        {
            Dictionary<string, string>? result = ChunkedTransfer.Unpack<Dictionary<string, string>>(unpacked);
            if (result == null)
            {
                Logger.Msg("Failed to deserialize TestTransfer data.");
                return;
            }

            foreach (var kvp in result)
            {
                int maxLen = 20;

                string val = kvp.Value ?? "nil";
                if(val.Length > maxLen)
                {
                    val = $"{val.Substring(0, maxLen)}... ({val.Length})";
                }
                Logger.Msg($"Key: {kvp.Key}, Value: {val}");
            }
        }

        public override object Fetch(params object[] args) => data;
    }
}
