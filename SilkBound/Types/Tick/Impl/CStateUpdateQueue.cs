using System.Collections.Generic;

namespace SilkBound.Types.Tick.Impl
{
    public class CStateUpdateQueue : Queue<KeyValuePair<string, object>>
    {
        public override void CompletedImpl(float dt)
        {

        }
    }
}
