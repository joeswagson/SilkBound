using SilkBound.Types.Tick;

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
