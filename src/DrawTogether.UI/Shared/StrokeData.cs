using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrawTogether.UI.Shared
{
    public struct StrokeData
    {
        public int cursorSize { get; set; }
        public (double x, double y)[] points { get; set; }
        public string color { get; set; }
    }
}
