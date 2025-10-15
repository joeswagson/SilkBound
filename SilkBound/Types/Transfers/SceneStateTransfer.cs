using SilkBound.Managers;
using SilkBound.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Types.Transfers
{
    public class SceneStateTransfer(SceneState state) : Transfer
    {
        public override void Completed(List<byte[]> unpacked)
        {
            SceneStateManager.Register(ChunkedTransfer.Unpack<SceneState>(unpacked));
        }

        public override object Fetch(params object[] args) => ChunkedTransfer.Pack(state);
    }
}
