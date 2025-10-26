using SilkBound.Managers;
using System.Collections.Generic;

namespace SilkBound.Types.Tick
{
    public abstract class Queue<T>
    {
        public List<T> items { get; private set; } = [];
        public void Enqueue(T item)
        {
            items.Add(item);
        }
        public void Pop(T item)
        {
            items.Remove(item);
        }
        public void Reset()
        {
            items.Clear();
        }
        public void Completed(float dt)
        {
            TickManager.OnTick -= Completed;
            CompletedImpl(dt);
            items.Clear();
        }

        public abstract void CompletedImpl(float dt);
    }
}
