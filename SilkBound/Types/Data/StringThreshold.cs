using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Types.Data {
    public class StringThreshold() : DistanceThreshold<ComparableString>(ComparableString.Distance, 1) { }
}
