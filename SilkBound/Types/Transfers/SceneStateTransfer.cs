using Newtonsoft.Json;
using SilkBound.Managers;
using SilkBound.Types.JsonConverters;
using SilkBound.Utils;
using System.Collections.Generic;
using System.Linq;

namespace SilkBound.Types.Transfers
{
    public class SceneStateTransfer(string name, StateChange[] changes) : Transfer
    {
        static JsonConverter[] converters => [new GameObjectConverter(true)];
        public override JsonConverter[] Converters => converters;
        public override void Completed(List<byte[]> unpacked, NetworkConnection connection)
        {
            Logger.Msg($"Converters ({converters.Length}):");
            converters.ToList().ForEach((conv)=>Logger.Msg("-", conv.GetType().Name));
            (string, StateChange[])? state = ChunkedTransfer.Unpack<(string, StateChange[])>(unpacked, converters);
            if (state.HasValue)
                SceneStateManager.ApplyChanges(SceneStateManager.Fetch(state.Value.Item1).Result, state.Value.Item2);
            else
                Logger.Error("SceneStateTransfer failed. Reason: Unpacked data chunks resulted in a null StateChange array.");
        }

        public override object Fetch(params object[] args) => (name, changes);
    }
}
