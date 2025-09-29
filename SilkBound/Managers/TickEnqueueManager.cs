using SilkBound.Types.Tick;
using System;
using System.Text;

namespace SilkBound.Managers
{
    public class TickEnqueueManager
    {
        public static void Register<T>(Queue<T> queue)
        {
            TickManager.OnTick += queue.Completed;
        }
    }
}
