using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Types.Tick
{
    public abstract class Queue<T>
    {
        public List<T> items { get; private set; } = new List<T>();
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
            CompletedImpl(dt);
            items.Clear();
        }

        public abstract void CompletedImpl(float dt);
    }
}
