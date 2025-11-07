using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Generated {
    internal class Props {
        internal const string ONLINE_PATH = @"$(SilksongPath_Steam)";
        internal const string OFFLINE_PATH = @"$(SilksongPath_Offline)";

        internal const string LOCAL_PROPS = @"$(LocalPropsDir)";

        internal const bool SMALL_WINDOW = $([MSBuild]::ValueOrDefault($(SmallWindow), false));
    }
}
