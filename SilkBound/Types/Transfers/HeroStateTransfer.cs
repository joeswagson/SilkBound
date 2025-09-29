using SilkBound.Behaviours;
using SilkBound.Network;
using SilkBound.Types.Mirrors;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Types.Transfers
{
    public class HeroStateTransfer(Guid weaver, Dictionary<string, object> changes) : Transfer
    {
        public HeroStateTransfer() : this(default, new Dictionary<string, object>()) { } // required for deserialization
        public Dictionary<string, object> changes = new();
        public override void Completed(List<byte[]> unpacked)
        {
            var result = ChunkedTransfer.Unpack<KeyValuePair<Guid, Dictionary<string, object>>?>(unpacked);
            if (!result.HasValue)
            {
                throw new Exception("Failed to deserialize HeroStateTransfer data.");
            }

            Weaver? weaverInstance = Server.CurrentServer!.GetWeaver(result.Value.Key);
            if (weaverInstance == null)
            {
                throw new Exception("Weaver not found for HeroStateTransfer.");
            }

            if(weaverInstance.Mirror == null)
            {
                throw new Exception("Weaver mirror not found for HeroStateTransfer.");
            }

            HeroControllerMirror mirrorController = weaverInstance.Mirror.MirrorController;

            foreach (var kvp in result.Value.Value)
            {
                if (mirrorController.IsMethod(kvp.Key))
                {
                    mirrorController.CallStateMember(kvp.Key, kvp.Value);
                }
                else
                {
                    mirrorController.SetStateProperty(kvp.Key, kvp.Value);
                }
            }
        }
        public override object Fetch(params object[] args) => new KeyValuePair<Guid, Dictionary<string, object>>(weaver, changes);
    }
}
