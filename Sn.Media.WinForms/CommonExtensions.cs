using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sn.Media.WinForms
{
    public static class CommonExtensions
    {
        public static bool IsSupportedPixelFormat(this FrameFormat frameFormat)
        {
            return frameFormat is FrameFormat.Bgra8888 or FrameFormat.Bgr888;
        }

        public static PixelFormat ToPixelFormat(this FrameFormat frameFormat)
        {
            return frameFormat switch
            {
                FrameFormat.Bgra8888 => PixelFormat.Format32bppArgb,
                FrameFormat.Bgr888 => PixelFormat.Format24bppRgb,
                _ => throw new NotSupportedException($"Unsupported frame format: {frameFormat}"),
            };
        }
    }
}
