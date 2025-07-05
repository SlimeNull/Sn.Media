using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sn.Media
{
    public record struct ColorBgra(byte B, byte G, byte R, byte A)
    {
        public static readonly ColorBgra Red = new ColorBgra(0, 0, 255, 255);
        public static readonly ColorBgra Green = new ColorBgra(0, 255, 0, 255);
        public static readonly ColorBgra Blue = new ColorBgra(255, 255, 0, 255);
    }
}
