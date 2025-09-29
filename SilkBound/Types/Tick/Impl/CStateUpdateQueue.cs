using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Types.Tick.Impl
{
    public class CStateUpdateQueue : Queue<KeyValuePair<string, object>>
    {
        public override void CompletedImpl(float dt)
        {

        }
    }
}
