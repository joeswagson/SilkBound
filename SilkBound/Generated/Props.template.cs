using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Generated {
    internal class Props {
        internal const bool SMALL_WINDOW = $([MSBuild]::ValueOrDefault($(SmallWindow), false));
        internal const bool DRAW_COLLIDERS = $([MSBuild]::ValueOrDefault($(DrawColliders), false));
        internal const bool DEBUG = $([MSBuild]::ValueOrDefault($(Debug), false));
        internal const bool IMMORTAL = $([MSBuild]::ValueOrDefault($(Immortal), false));
        internal const bool POWERUPS = $([MSBuild]::ValueOrDefault($(Powerups), false));
        internal const int MONITOR = $([MSBuild]::ValueOrDefault($(Monitor), 1));
    }
}
