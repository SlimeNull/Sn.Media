using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Sn.Media.WPF
{
    public static class CommonExtensions
    {
        public static bool IsSupportedPixelFormat(this FrameFormat frameFormat)
        {
            return frameFormat is FrameFormat.Bgra8888 or FrameFormat.Bgr888 or FrameFormat.Rgb888;
        }

        public static PixelFormat ToPixelFormat(this FrameFormat frameFormat)
        {
            return frameFormat switch
            {
                FrameFormat.Bgra8888 => PixelFormats.Bgra32,
                FrameFormat.Bgr888 => PixelFormats.Bgr24,
                FrameFormat.Rgb888 => PixelFormats.Rgb24,
                _ => throw new NotSupportedException($"Unsupported frame format: {frameFormat}"),
            };
        }
    }
}
