using System.Diagnostics;

namespace Sn.Media.Tests
{
    public class BlinkFrameStream : IFrameStream
    {
        private readonly int _frameWidth;
        private readonly int _frameHeight;
        private readonly ColorBgra _color1;
        private readonly ColorBgra _color2;
        private readonly double _blinkPeriod; // in seconds
        private long _currentPosition; // Current frame position
        public BlinkFrameStream(int frameWidth, int frameHeight, ColorBgra color1, ColorBgra color2, double blinkPeriod)
        {
            _frameWidth = frameWidth;
            _frameHeight = frameHeight;
            _color1 = color1;
            _color2 = color2;
            _blinkPeriod = blinkPeriod;
            _currentPosition = 0;
        }
        public FrameFormat Format => FrameFormat.Bgra8888;
        public Fraction FrameRate => new Fraction(30, 1); // Assuming 30 FPS
        public int FrameWidth => _frameWidth;
        public int FrameHeight => _frameHeight;
        public int FrameStride => ((4 * _frameWidth + 3) / 4) * 4; // 4 bytes per pixel, aligned to 4 bytes
        public int FrameDataSize => FrameStride * FrameHeight;

        public bool HasPosition => true;
        public bool CanSeek => true;

        public long Position => _currentPosition;

        public void Seek(long position)
        {
            _currentPosition = position;
        }
        public bool ReadFrame(byte[] buffer, int offset, int count)
        {
            // Calculate the time in the current frame cycle
            double totalSeconds = ((double)_currentPosition / FrameRate.Numerator) * FrameRate.Denominator;
            double phase = (totalSeconds % _blinkPeriod) / _blinkPeriod; // Normalize to [0, 1)
            ColorBgra currentColor;
            if (phase < 0.5)
            {
                // Transition from color1 to color2
                currentColor = LerpColor(_color1, _color2, phase * 2);
            }
            else
            {
                // Transition from color2 to color1
                currentColor = LerpColor(_color2, _color1, (phase - 0.5) * 2);
            }
            int bytesPerPixel = 4; // BGRA
                                   // Fill the buffer with the current color
            for (int i = 0; i < _frameHeight; i++)
            {
                for (int j = 0; j < _frameWidth; j++)
                {
                    int pixelIndex = (i * FrameStride) + (j * bytesPerPixel);
                    buffer[offset + pixelIndex] = currentColor.B;   // Blue
                    buffer[offset + pixelIndex + 1] = currentColor.G; // Green
                    buffer[offset + pixelIndex + 2] = currentColor.R; // Red
                    buffer[offset + pixelIndex + 3] = currentColor.A; // Alpha
                }
            }
            _currentPosition++;

            return true;
        }
        private ColorBgra LerpColor(ColorBgra from, ColorBgra to, double t)
        {
            byte b = (byte)(from.B + (to.B - from.B) * t);
            byte g = (byte)(from.G + (to.G - from.G) * t);
            byte r = (byte)(from.R + (to.R - from.R) * t);
            byte a = (byte)(from.A + (to.A - from.A) * t);
            return new ColorBgra(b, g, r, a);
        }
    }
}
