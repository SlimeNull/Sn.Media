using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sn.Media
{
    public static class CommonExtensions
    {
        public static int GetByteSize(this SampleFormat sampleFormat)
        {
            return sampleFormat switch
            {
                SampleFormat.UInt8 => 1,
                SampleFormat.Int16 => 2,
                SampleFormat.Int32 => 4,
                SampleFormat.Float32 => 4,
                _ => throw new NotSupportedException($"Unsupported sample format: {sampleFormat}"),
            };
        }
    }
}
