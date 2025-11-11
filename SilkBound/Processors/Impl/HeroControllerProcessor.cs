using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Processors.Impl {
    public class HeroControllerProcessorState : ProcessorState<HeroController> {
        
    }
    public class HeroControllerProcessor : Processor<HeroController> {
        public override HeroController Process(ProcessorState<HeroController> state)
        {
            throw new NotImplementedException();
        }
    }
}
