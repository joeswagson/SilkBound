using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SilkBound.Processors {
    public abstract class Processor<T> {
        internal void Init()
        {
            Initialize();
            Logger.Debug("Initialized processor", GetType().Name);
        }

        protected virtual void Initialize() { }
        protected virtual void Dispose() { }
        protected virtual void Update() { }

        public abstract T Process(ProcessorState<T> state);
        protected virtual void DisposeObject(T claim)
        {
            if(claim is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
